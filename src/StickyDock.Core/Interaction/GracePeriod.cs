namespace StickyDock.Core.Interaction;

/// <summary>
/// Bounds of the configurable grace period of the collapse timer (Q-03, §4.3-03).
/// </summary>
/// <remarks>
/// The hover of 200 ms is fixed by the specification and is not configurable; only the grace period is (ADR-003).
/// </remarks>
public static class GracePeriod
{
    /// <summary>
    /// Shortest grace period the user can choose, in milliseconds.
    /// </summary>
    public const int MinimumMilliseconds = 300;

    /// <summary>
    /// Longest grace period the user can choose, in milliseconds.
    /// </summary>
    public const int MaximumMilliseconds = 500;

    /// <summary>
    /// Grace period used when the user has not chosen one, in milliseconds.
    /// </summary>
    public const int DefaultMilliseconds = 400;

    /// <summary>
    /// Clamps a stored value into <see cref="MinimumMilliseconds"/>..<see cref="MaximumMilliseconds"/>.
    /// </summary>
    /// <param name="milliseconds">Value read from the configuration.</param>
    /// <returns>The value itself when it is inside the range; otherwise the nearest bound.</returns>
    public static int Normalize(int milliseconds)
        => Math.Clamp(milliseconds, MinimumMilliseconds, MaximumMilliseconds);
}
