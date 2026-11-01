using System.Windows.Threading;
using StickyDock.Core.Timing;

namespace StickyDock.App.Timing;

/// <summary>
/// Adapter of <see cref="ISingleShotScheduler"/> over one <see cref="DispatcherTimer"/> of the UI thread (ADR-003, D4).
/// </summary>
/// <remarks>
/// <para>
/// The timer is created with the dispatcher of the thread that builds the instance and is never running while no timer is
/// armed, so the bar costs nothing while idle (RNF-02).
/// </para>
/// <para>
/// Single-threaded: it must be created and used on the UI thread, which is where the interaction controller lives.
/// </para>
/// </remarks>
internal sealed class DispatcherTimerScheduler : ISingleShotScheduler
{
    private readonly DispatcherTimer _timer;

    private Action? _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimerScheduler"/> class.
    /// </summary>
    public DispatcherTimerScheduler()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher);
        _timer.Tick += OnTick;
    }

    /// <inheritdoc />
    public bool IsPending => _timer.IsEnabled;

    /// <inheritdoc />
    public void Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // One slot: arming it again replaces whatever was pending, which is the invariant of the state machine.
        Stop();
        _callback = callback;
        _timer.Interval = delay;
        _timer.Start();
    }

    /// <inheritdoc />
    public void Cancel() => Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        var callback = _callback;

        // Disarmed before invoking so that a transition inside the callback cannot re-enter the tick.
        Stop();

        callback?.Invoke();
    }

    private void Stop()
    {
        _timer.Stop();
        _callback = null;
    }
}
