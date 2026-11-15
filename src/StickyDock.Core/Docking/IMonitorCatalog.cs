namespace StickyDock.Core.Docking;

/// <summary>
/// Enumerates the currently attached display monitors and their geometry in device pixels (RF05, E7).
/// </summary>
public interface IMonitorCatalog
{
    /// <summary>
    /// Returns the display monitors currently attached to the desktop.
    /// </summary>
    IReadOnlyList<DisplayMonitorInfo> GetMonitors();
}
