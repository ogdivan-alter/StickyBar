using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Taskbar and appbar information.
/// </summary>
public static class TaskbarMonitor
{
    private const int ABM_GETSTATE = 0x00000004;
    private const int ABS_AUTOHIDE = 0x00000001;

    private const int ABM_GETTASKBARPOS = 0x00000005;

    /// <summary>
    /// Gets the taskbar position/edge and its rect on the monitor containing the given window.
    /// </summary>
    public static TaskbarInfo GetTaskbarInfo(nint windowHandle)
    {
        var abd = new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>()
        };

        var result = NativeMethods.SHAppBarMessage((uint)ABM_GETTASKBARPOS, ref abd);
        if (result != 0)
        {
            var edge = (TaskbarEdge)abd.uEdge;
            return new TaskbarInfo(edge, ToRectInterop(abd.rc), IsAutoHide());
        }

        var taskbarHwnd = NativeMethods.FindWindowW("Shell_TrayWnd", null);
        if (taskbarHwnd != nint.Zero)
        {
            if (NativeMethods.GetWindowRect(taskbarHwnd, out var rect))
            {
                var screen = DisplayMetrics.GetPrimaryScreenSize();
                var edge = rect.Bottom >= screen.HeightInPixels - 2 ? TaskbarEdge.Bottom
                          : rect.Top <= 2 ? TaskbarEdge.Top
                          : rect.Left <= 2 ? TaskbarEdge.Left
                          : rect.Right >= screen.WidthInPixels - 2 ? TaskbarEdge.Right
                          : TaskbarEdge.Bottom;
                return new TaskbarInfo(edge, new InteropRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top), IsAutoHide());
            }
        }

        return new TaskbarInfo(TaskbarEdge.Bottom, new InteropRect(0, 0, 0, 0), IsAutoHide());
    }

    private static bool IsAutoHide()
    {
        var abd = new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>()
        };
        var state = NativeMethods.SHAppBarMessage((uint)ABM_GETSTATE, ref abd);
        return (state & ABS_AUTOHIDE) == ABS_AUTOHIDE;
    }

    private static InteropRect ToRectInterop(RECT r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
}

/// <summary>
/// Taskbar info.
/// </summary>
public readonly record struct TaskbarInfo(TaskbarEdge Edge, InteropRect Rect, bool AutoHide);

/// <summary>
/// Taskbar edge.
/// </summary>
public enum TaskbarEdge
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3
}

/// <summary>
/// Rectangle.
/// </summary>
public readonly record struct InteropRect(int X, int Y, int Width, int Height);
