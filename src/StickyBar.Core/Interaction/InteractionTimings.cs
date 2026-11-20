namespace StickyBar.Core.Interaction;

/// <summary>
/// Durations of the interaction of the bar.
/// </summary>
/// <remarks>
/// The activation and the slide are fixed by RF02 and by ADR-003; the grace period is the only one the user can change,
/// and it is normalized when the timings are built.
/// </remarks>
/// <param name="Activation">Time the pointer has to rest on the tab before the bar deploys.</param>
/// <param name="Grace">Time the bar stays deployed after the pointer leaves its interactive area.</param>
/// <param name="Slide">Duration of the slide and of the micro-expansion.</param>
public readonly record struct InteractionTimings(TimeSpan Activation, TimeSpan Grace, TimeSpan Slide)
{
    /// <summary>
    /// Activation of the hover, fixed at 200 ms by RF02 (§4.3-02) and Q-03: it is not configurable.
    /// </summary>
    public const int ActivationMilliseconds = 200;

    /// <summary>
    /// Duration of the slide and of the micro-expansion, in milliseconds. Engineering decision of ADR-003 (§12).
    /// </summary>
    public const int SlideMilliseconds = 160;

    /// <summary>
    /// Builds the timings from the stored grace period.
    /// </summary>
    /// <param name="graceMilliseconds">Grace period read from the configuration.</param>
    /// <returns>The timings, with the grace period normalized to 300..500 ms.</returns>
    public static InteractionTimings FromGrace(int graceMilliseconds)
        => new(
            TimeSpan.FromMilliseconds(ActivationMilliseconds),
            TimeSpan.FromMilliseconds(GracePeriod.Normalize(graceMilliseconds)),
            TimeSpan.FromMilliseconds(SlideMilliseconds));
}
