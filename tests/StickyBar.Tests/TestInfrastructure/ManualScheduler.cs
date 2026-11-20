using StickyBar.Core.Timing;

namespace StickyBar.Tests.TestInfrastructure;

/// <summary>
/// Manual clock behind <see cref="ISingleShotScheduler"/>: time only moves when <see cref="Advance"/> is called, so the
/// behaviour of the timers is asserted without waiting for real time (ADR-003, §9).
/// </summary>
/// <remarks>It keeps the single slot of the contract: scheduling replaces whatever was pending.</remarks>
internal sealed class ManualScheduler : ISingleShotScheduler
{
    private const int MaxCallbacksPerAdvance = 16;

    private Action? _callback;
    private TimeSpan _due;
    private TimeSpan _clock;

    /// <summary>Gets a value indicating whether a timer is armed.</summary>
    public bool IsPending => _callback is not null;

    /// <summary>Gets the delay of the timer that is armed, or <see langword="null"/> when the slot is empty.</summary>
    public TimeSpan? PendingDelay => _callback is null ? null : _due - _clock;

    /// <summary>Gets the number of times a timer was armed.</summary>
    public int ScheduleCount { get; private set; }

    /// <summary>Gets the number of times the slot was disarmed.</summary>
    public int CancelCount { get; private set; }

    /// <inheritdoc />
    public void Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        ScheduleCount++;
        _callback = callback;
        _due = _clock + delay;
    }

    /// <inheritdoc />
    public void Cancel()
    {
        if (_callback is null)
        {
            return;
        }

        CancelCount++;
        _callback = null;
    }

    /// <summary>
    /// Moves the clock forward and runs the pending timer when it is due, including the timers it arms in turn.
    /// </summary>
    /// <param name="elapsed">Time elapsed since the previous call.</param>
    public void Advance(TimeSpan elapsed)
    {
        _clock += elapsed;

        var fired = 0;

        // A callback may arm the slot again (settle after grace, activation after collapse), so the loop drains it.
        while (_callback is { } callback && _clock >= _due)
        {
            _callback = null;
            callback();

            if (++fired > MaxCallbacksPerAdvance)
            {
                throw new InvalidOperationException("The scheduler armed more timers than expected in one advance.");
            }
        }
    }

    /// <summary>Moves the clock forward by a number of milliseconds.</summary>
    /// <param name="milliseconds">Time elapsed since the previous call.</param>
    public void Advance(int milliseconds) => Advance(TimeSpan.FromMilliseconds(milliseconds));
}
