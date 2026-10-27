using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Work area of a monitor, in physical pixels.
/// </summary>
/// <param name="Left">Left coordinate of the work area.</param>
/// <param name="Top">Top coordinate of the work area.</param>
/// <param name="Width">Width of the work area.</param>
/// <param name="Height">Height of the work area.</param>
public readonly record struct WorkArea(int Left, int Top, int Width, int Height);

/// <summary>
/// Safe accessors for window and display helpers.
/// </summary>
public static class WindowHelpers
{
    private const uint MonitorDefaultToNearest = 0x00000002;

    /// <summary>
    /// Gets the work area of the monitor that contains the window.
    /// </summary>
    /// <param name="windowHandle">Handle of the window.</param>
    /// <returns>The work area of the monitor containing the window.</returns>
    public static WorkArea GetWorkArea(nint windowHandle)
    {
        var monitor = NativeMethods.MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
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

        var screen = DisplayMetrics.GetPrimaryScreenSize();
        return new WorkArea(0, 0, screen.WidthInPixels, screen.HeightInPixels);
    }
}
