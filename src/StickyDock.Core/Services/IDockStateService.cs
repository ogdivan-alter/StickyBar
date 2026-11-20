using StickyDock.Core.Mvvm;

namespace StickyDock.Core.Services;

/// <summary>
/// Tracks the state of the dock bar (tab / expanded). Logic lives in Core, not in the view.
/// </summary>
public interface IDockStateService
{
    /// <summary>Gets the current state of the dock bar.</summary>
    DockState State { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the dock bar is pinned open, preventing automatic collapse.
    /// </summary>
    bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a context menu is currently open, suspending automatic collapse.
    /// </summary>
    bool IsContextMenuOpen { get; set; }

    /// <summary>Raised when the state changes.</summary>
    event EventHandler<DockStateChangedEventArgs>? StateChanged;

    /// <summary>Sets the state to tab (collapsed).</summary>
    void CollapseToTab();

    /// <summary>Sets the state to expanded.</summary>
    void Expand();

    /// <summary>Suppresses automatic state transitions until <see cref="EndDrag"/>.</summary>
    void BeginDrag();

    /// <summary>Reenables automatic state transitions and reevaluates the current regions.</summary>
    void EndDrag();
}

/// <summary>
/// Arguments for dock state changes.
/// </summary>
/// <param name="State">The new dock state.</param>
public sealed record DockStateChangedEventArgs(DockState State);
