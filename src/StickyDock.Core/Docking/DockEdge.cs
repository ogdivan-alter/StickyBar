namespace StickyDock.Core.Docking;

/// <summary>
/// Edge of the screen. The single edge type of the product (ADR-002).
/// </summary>
/// <remarks>
/// The numeric values are part of the persisted contract: <c>settings.json</c> stores the docked edge as
/// <c>dockEdge</c> with this order, so they must not be reordered without a schema migration. The values of
/// <c>APPBARDATA.uEdge</c> (<c>ABE_*</c>) follow a different order and are translated in a single place by
/// <c>StickyDock.Interop.Win32.AppBarEdgeMapping</c>.
/// </remarks>
public enum DockEdge
{
    /// <summary>Top edge of the work area.</summary>
    Top = 0,

    /// <summary>Left edge of the work area.</summary>
    Left = 1,

    /// <summary>Right edge of the work area.</summary>
    Right = 2,

    /// <summary>Bottom edge of the work area.</summary>
    Bottom = 3,
}
