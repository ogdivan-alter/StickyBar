namespace StickyDock.Interop.Win32;

/// <summary>
/// Safe access to display metrics. It is the seed of the Win32 boundary that E1 and E2 extend with window, work area
/// and taskbar queries; every native call of the product goes through this assembly.
/// </summary>
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
