using System.Runtime.InteropServices;

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

    /// <summary>
    /// Gets the work area for the monitor containing the window.
    /// </summary>
    public static WorkArea GetWorkAreaForWindow(nint windowHandle) => WindowHelpers.GetWorkArea(windowHandle);

    /// <summary>
    /// Gets the work area for a given monitor.
    /// </summary>
    public static WorkArea GetWorkAreaForMonitor(nint monitor)
    {
        var info = new MONITORINFO
        {
            cbSize = Marshal.SizeOf<MONITORINFO>()
        };

        if (NativeMethods.GetMonitorInfoW(monitor, ref info))
        {
            return new WorkArea(
                info.rcWork.Left,
                info.rcWork.Top,
                info.rcWork.Right - info.rcWork.Left,
                info.rcWork.Bottom - info.rcWork.Top);
        }

        var screen = GetPrimaryScreenSize();
        return new WorkArea(0, 0, screen.WidthInPixels, screen.HeightInPixels);
    }

    /// <summary>
    /// Gets the monitor from window.
    /// </summary>
    public static nint GetMonitorFromWindow(nint windowHandle) => NativeMethods.MonitorFromWindow(windowHandle, 0x00000002 /* MONITOR_DEFAULTTONEAREST */);
}
