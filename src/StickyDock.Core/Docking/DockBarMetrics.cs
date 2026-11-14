namespace StickyDock.Core.Docking;

/// <summary>
/// Declared footprint of the bar, in device-independent units (ADR-003, D2).
/// </summary>
/// <remarks>
/// <para>
/// The sizes are declared and never measured: that is what closes D-01, because a window resized from outside cannot
/// contaminate the next placement (rule R1 of ADR-003 §6.6).
/// </para>
/// <para>
/// This is the single adjustment point of the sizes. The collapsed footprint is exactly the window of E1 (120×36), so
/// the bar keeps intercepting only the surface of the tab while it is idle (E1-CA4, deuda D-15).
/// </para>
/// </remarks>
/// <param name="CollapsedLength">Length along the edge while the bar is collapsed.</param>
/// <param name="CollapsedThickness">Thickness away from the edge while the bar is collapsed.</param>
/// <param name="ExpandedLength">Length along the edge when the bar is deployed.</param>
/// <param name="ExpandedThickness">Thickness away from the edge when the bar is deployed.</param>
public readonly record struct DockBarMetrics(
    double CollapsedLength,
    double CollapsedThickness,
    double ExpandedLength,
    double ExpandedThickness)
{
    /// <summary>
    /// Gets the design values: a collapsed footprint of 120×36 —the window of E1, unchanged— and a deployed panel of
    /// 168×96.
    /// </summary>
    public static DockBarMetrics Default { get; } = new(120d, 36d, 168d, 96d);

    /// <summary>Reserved length along the deployed bar for the create-ribbon button and margins (E4-CA6).</summary>
    public const double BarActionPadding = 44d;

    /// <summary>Length occupied by each ribbon indicator slot along the bar (E4-CA6).</summary>
    public const double RibbonIndicatorExtent = 84d;

    /// <summary>Length occupied by each shortcut slot on the main bar (E4-CA6).</summary>
    public const double BarItemExtent = 44d;

    /// <summary>Additional perpendicular thickness added when a ribbon is open (E4-CA1, E4-CA6).</summary>
    public const double OpenRibbonThickness = 144d;

    /// <summary>
    /// Gets the footprint of a state in the orientation of an edge.
    /// </summary>
    /// <param name="expanded">Whether the deployed footprint applies.</param>
    /// <param name="anchor">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
    /// <returns>
    /// The declared size, with length and thickness swapped on the vertical edges. A floating bar is laid out
    /// horizontally, which is how the window of E1 is designed.
    /// </returns>
    public DockBarSize GetSize(bool expanded, DockEdge? anchor)
    {
        var vertical = anchor is DockEdge.Left or DockEdge.Right;

        var length = expanded ? ExpandedLength : CollapsedLength;
        var thickness = expanded ? ExpandedThickness : CollapsedThickness;

        return vertical ? new DockBarSize(thickness, length) : new DockBarSize(length, thickness);
    }

    /// <summary>
    /// Gets the dynamic footprint of the bar considering its ribbons, items and open-ribbon state (E4-CA6).
    /// </summary>
    /// <param name="expanded">Whether the deployed footprint applies.</param>
    /// <param name="anchor">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
    /// <param name="ribbonCount">Number of ribbons currently defined on the bar.</param>
    /// <param name="barItemCount">Number of shortcut items directly on the bar.</param>
    /// <param name="hasOpenRibbon">Whether a ribbon is currently open.</param>
    /// <returns>
    /// The dynamic footprint. When collapsed, returns the idle tab size. When expanded with zero ribbons and zero items,
    /// preserves <see cref="CollapsedLength"/> as the minimum length.
    /// </returns>
    public DockBarSize GetDynamicSize(
        bool expanded,
        DockEdge? anchor,
        int ribbonCount,
        int barItemCount,
        bool hasOpenRibbon)
    {
        if (!expanded)
        {
            return GetSize(expanded: false, anchor);
        }

        var vertical = anchor is DockEdge.Left or DockEdge.Right;
        var ribbons = Math.Max(0, ribbonCount);
        var items = Math.Max(0, barItemCount);

        var length = ribbons == 0 && items == 0
            ? CollapsedLength
            : Math.Max(
                CollapsedLength,
                BarActionPadding + ribbons * RibbonIndicatorExtent + items * BarItemExtent);

        var thickness = ExpandedThickness + (hasOpenRibbon ? OpenRibbonThickness : 0d);

        return vertical ? new DockBarSize(thickness, length) : new DockBarSize(length, thickness);
    }
}
