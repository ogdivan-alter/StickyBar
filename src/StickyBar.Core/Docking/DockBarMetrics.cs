namespace StickyBar.Core.Docking;

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

    /// <summary>Reserved length along the deployed bar for the grip (22), app icon (38), create-ribbon button (32), pin button (28) and padding/margins (~26) (E4-CA6).</summary>
    public const double BarActionPadding = 146d;

    /// <summary>Length occupied by each ribbon indicator slot along the bar (E4-CA6).</summary>
    public const double RibbonIndicatorExtent = 42d;

    /// <summary>Length occupied by each shortcut slot on the main bar (E4-CA6).</summary>
    public const double BarItemExtent = 54d;

    /// <summary>Fixed thickness of the main bar slot along the docked edge.</summary>
    public const double BarSlotThickness = 52d;

    /// <summary>Base perpendicular thickness for an open ribbon (header + inline edit button + padding/margins).</summary>
    public const double OpenRibbonBaseThickness = 72d;

    /// <summary>Perpendicular thickness occupied by each shortcut item inside an open ribbon.</summary>
    public const double OpenRibbonItemExtent = 44d;

    /// <summary>Additional perpendicular thickness when inline ribbon management actions are expanded.</summary>
    public const double OpenRibbonActionsExtent = 80d;

    /// <summary>Legacy maximum perpendicular thickness constant kept for reference.</summary>
    public const double OpenRibbonThickness = 260d;

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
        => GetDynamicSize(
            expanded,
            anchor,
            ribbonCount,
            barItemCount,
            hasOpenRibbon,
            openRibbonItemCount: 0,
            isOpenRibbonActionsOpen: false);

    /// <summary>
    /// Gets the dynamic footprint of the bar considering its ribbons, items, open-ribbon state and open-ribbon item
    /// count so the window expands proportionally without clipping or jumping (E4-CA6).
    /// </summary>
    public DockBarSize GetDynamicSize(
        bool expanded,
        DockEdge? anchor,
        int ribbonCount,
        int barItemCount,
        bool hasOpenRibbon,
        int openRibbonItemCount,
        bool isOpenRibbonActionsOpen = false)
    {
        if (!expanded)
        {
            return GetSize(expanded: false, anchor);
        }

        var vertical = anchor is DockEdge.Left or DockEdge.Right;
        var ribbons = Math.Max(0, ribbonCount);
        var items = Math.Max(0, barItemCount);
        var ribbonItems = Math.Max(0, openRibbonItemCount);

        var length = ribbons == 0 && items == 0
            ? CollapsedLength
            : Math.Max(
                CollapsedLength,
                BarActionPadding + ribbons * RibbonIndicatorExtent + items * BarItemExtent);

        var thickness = ExpandedThickness;
        if (hasOpenRibbon)
        {
            var ribbonPanelThickness = OpenRibbonBaseThickness
                + ribbonItems * OpenRibbonItemExtent
                + (isOpenRibbonActionsOpen ? OpenRibbonActionsExtent : 0d);
            thickness = Math.Max(ExpandedThickness, BarSlotThickness + ribbonPanelThickness);
        }

        return vertical ? new DockBarSize(thickness, length) : new DockBarSize(length, thickness);
    }
}
