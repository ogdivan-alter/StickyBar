namespace StickyDock.Core.Interaction;

/// <summary>
/// What must be reported to <see cref="IDockRegionSink"/> after looking at the pointer.
/// </summary>
public enum RegionReport
{
    /// <summary>The pointer is where it already was: there is nothing to report.</summary>
    None = 0,

    /// <summary>The pointer is inside and was outside: the region must be entered.</summary>
    Enter = 1,

    /// <summary>The pointer is outside and was inside: the region must be left.</summary>
    Leave = 2,
}

/// <summary>
/// Turns repeated observations of the pointer into the transitions of one region (D-E3-01).
/// </summary>
/// <remarks>
/// <para>
/// The region of the bar is the whole window footprint, and it has to survive two situations a single event cannot cover:
/// the pointer moving inside the footprint for a while, and the window changing size under a still pointer. Both are
/// reduced to the same question —is the pointer inside?— asked as many times as needed; the tracker answers which
/// transition, if any, that observation implies.
/// </para>
/// <para>
/// It is a pure decision helper with one bit of state, so the idempotence the defect demands is verified without a window:
/// observing the same side twice reports <see cref="RegionReport.None"/> the second time, which is what keeps the grace
/// period from being re-armed or cancelled by duplicated events.
/// </para>
/// </remarks>
public sealed class WindowRegionTracker
{
    private bool _isInside;

    /// <summary>
    /// Gets a value indicating whether the last observation found the pointer inside.
    /// </summary>
    public bool IsInside => _isInside;

    /// <summary>
    /// Records an observation of the pointer and says what has to be reported because of it.
    /// </summary>
    /// <param name="isInside">Whether the pointer is inside the region at this instant.</param>
    /// <returns>The transition the observation implies, or <see cref="RegionReport.None"/> when it implies none.</returns>
    public RegionReport Observe(bool isInside)
    {
        if (isInside == _isInside)
        {
            return RegionReport.None;
        }

        _isInside = isInside;

        return isInside ? RegionReport.Enter : RegionReport.Leave;
    }
}
