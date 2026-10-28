namespace StickyDock.Core.Docking;

/// <summary>
/// Reads the geometry that surrounds one bar window.
/// </summary>
/// <remarks>
/// Implemented by <c>StickyDock.Interop</c>. Core decides with this data but never touches Win32 itself, which keeps
/// the front  between the two assemblies compilable instead of reflective (ADR-002).
/// </remarks>
public interface IWindowGeometrySource
{
    /// <summary>
    /// Gets the monitor that currently hosts the bar window.
    /// </summary>
    /// <returns>Bounds, work area and DPI of the monitor, in device pixels.</returns>
    MonitorGeometry GetMonitorGeometry();

    /// <summary>
    /// Gets the taskbar as seen from the monitor that hosts the bar window.
    /// </summary>
    /// <returns>The placement of the taskbar and whether it belongs to the current monitor.</returns>
    TaskbarPlacement GetTaskbarPlacement();
}
