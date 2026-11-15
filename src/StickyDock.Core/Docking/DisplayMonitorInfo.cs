namespace StickyDock.Core.Docking;

/// <summary>
/// Snapshot of a physical display monitor in device pixels (RF05, E7).
/// </summary>
/// <param name="DeviceName">System device name of the monitor (for example, <c>\\.\DISPLAY1</c>).</param>
/// <param name="IsPrimary"><see langword="true"/> when this is the primary display.</param>
/// <param name="Bounds">Full monitor bounds in device pixels.</param>
/// <param name="WorkArea">Usable work area excluding taskbars and appbars, in device pixels.</param>
/// <param name="Dpi">Effective DPI of the monitor (96 at 100% scaling).</param>
public sealed record DisplayMonitorInfo(
    string DeviceName,
    bool IsPrimary,
    PixelRect Bounds,
    PixelRect WorkArea,
    int Dpi);
