namespace StickyBar.Core.Services;

/// <summary>
/// Reports the dynamic content counts and open-ribbon state used to compute the expanded bar footprint (E4-CA6).
/// </summary>
public interface IRibbonLayoutState
{
    /// <summary>Gets the number of ribbons currently defined on the bar.</summary>
    int RibbonCount { get; }

    /// <summary>Gets the number of shortcut items placed directly on the main bar.</summary>
    int BarItemCount { get; }

    /// <summary>Gets a value indicating whether a ribbon is currently open.</summary>
    bool HasOpenRibbon { get; }

    /// <summary>Gets the number of shortcut items inside the currently open ribbon, or <c>0</c> if none is open.</summary>
    int OpenRibbonItemCount => 0;

    /// <summary>Gets a value indicating whether the inline actions strip of the open ribbon is expanded.</summary>
    bool IsOpenRibbonActionsOpen => false;
}
