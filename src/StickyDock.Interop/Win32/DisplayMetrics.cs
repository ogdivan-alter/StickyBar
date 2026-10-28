namespace StickyDock.Interop.Win32;

/// <summary>
/// Safe access to the metrics of the primary display.
/// </summary>
/// <remarks>
/// Every native call of the product goes through this assembly (AGENTS.md). The docking reads per-monitor geometry from
/// <see cref="Win32WindowGeometrySource"/>; this type only covers the primary screen, which is what the startup log and
/// the degraded paths need.
/// </remarks>
public static class DisplayMetrics
{
    /// <summary>
    /// Reads the size of the primary monitor.
    /// </summary>
    /// <returns>The primary screen size, in physical pixels.</returns>
    public static ScreenSize GetPrimaryScreenSize()
        => new(
            NativeMethods.GetSystemMetrics((int)SystemMetric.PrimaryScreenWidth),
            NativeMethods.GetSystemMetrics((int)SystemMetric.PrimaryScreenHeight));
}
