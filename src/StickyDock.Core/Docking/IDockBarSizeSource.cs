namespace StickyDock.Core.Docking;

/// <summary>
/// Declares the size the bar occupies for the state it is in.
/// </summary>
/// <remarks>
/// Implemented in Core over the dock state, so that the size depends only on the state and never on the size the window
/// happens to have (D-01, rule R1 of ADR-003).
/// </remarks>
public interface IDockBarSizeSource
{
    /// <summary>
    /// Gets the size of the bar for the current state.
    /// </summary>
    /// <param name="anchor">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
    /// <returns>The declared footprint, in device-independent units.</returns>
    DockBarSize GetCurrentSize(DockEdge? anchor);
}
