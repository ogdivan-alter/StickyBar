namespace StickyDock.Core.Interaction;

/// <summary>
/// Port the view model uses to report that the pointer entered or left an interactive region of the bar.
/// </summary>
/// <remarks>
/// <para>
/// It is deliberately separate from <see cref="StickyDock.Core.Services.IDockStateService"/>: the state is consulted and
/// the pointer input is notified, so the state contract does not grow with every region a later stage adds (ADR-003, D7).
/// </para>
/// <para>
/// Single-threaded: the implementation must be called on the UI thread.
/// </para>
/// </remarks>
public interface IDockRegionSink
{
    /// <summary>
    /// Records that the pointer entered <paramref name="region"/>.
    /// </summary>
    /// <param name="region">Region the pointer entered.</param>
    void EnterRegion(DockRegion region);

    /// <summary>
    /// Records that the pointer left <paramref name="region"/>.
    /// </summary>
    /// <param name="region">Region the pointer left.</param>
    void LeaveRegion(DockRegion region);
}
