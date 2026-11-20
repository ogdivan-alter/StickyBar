namespace StickyBar.Core.Interaction;

/// <summary>
/// Part of the interactive area of the bar the pointer is in (A4).
/// </summary>
/// <remarks>
/// The interactive area is the union of the regions the pointer occupies, so moving between the tab, the deployed bar and
/// an open ribbon never collapses it. Ribbons themselves belong to E4; the region is declared here so that E3 does not
/// have to be reopened (ADR-003, §7.6).
/// </remarks>
public enum DockRegion
{
    /// <summary>No region. Never reported by the view.</summary>
    None = 0,

    /// <summary>The idle tab, present in every state.</summary>
    Tab = 1,

    /// <summary>The deployed bar.</summary>
    Bar = 2,

    /// <summary>A deployed ribbon (E4).</summary>
    Ribbon = 3,
}
