namespace StickyDock.Core.Timing;

/// <summary>
/// One-shot timer with a single slot: scheduling replaces whatever was armed before.
/// </summary>
/// <remarks>
/// <para>
/// The single slot is the design decision that makes overlapping timers impossible, which is the classic source of
/// inconsistent states in an interaction machine of this kind (ADR-003, D4).
/// </para>
/// <para>
/// Implemented by a <c>DispatcherTimer</c> in the application and by a manual clock in the tests, so no test waits for
/// real time.
/// </para>
/// </remarks>
public interface ISingleShotScheduler
{
    /// <summary>
    /// Gets a value indicating whether a timer is armed.
    /// </summary>
    bool IsPending { get; }

    /// <summary>
    /// Arms the slot, replacing any pending timer.
    /// </summary>
    /// <param name="delay">Time between now and the callback.</param>
    /// <param name="callback">Action invoked once, on the thread that owns the scheduler.</param>
    void Schedule(TimeSpan delay, Action callback);

    /// <summary>
    /// Disarms the slot. Idempotent.
    /// </summary>
    void Cancel();
}
