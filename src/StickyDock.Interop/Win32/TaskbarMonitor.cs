using System.Runtime.InteropServices;
using StickyDock.Core.Docking;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Safe wrapper of <c>SHAppBarMessage(ABM_GETTASKBARPOS / ABM_GETSTATE)</c> with the <c>Shell_TrayWnd</c> fallback.
/// </summary>
/// <remarks>
/// <c>SHAppBarMessage</c> describes the primary taskbar; the fallback covers sessions where the shell does not answer.
/// The edge is derived against the monitor that hosts the bar, never against the primary screen, so the result is
/// usable with mixed DPI (R-05).
/// </remarks>
public static class TaskbarMonitor
{
    private const int AbmGetState = 0x00000004;
    private const int AbmGetTaskbarPos = 0x00000005;
    private const int AbsAutoHide = 0x00000001;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const string TaskbarWindowClass = "Shell_TrayWnd";

    /// <summary>
    /// Probes the taskbar as seen from the monitor that hosts the given window.
    /// </summary>
    /// <param name="windowHandle">Handle of the bar window, used to locate its monitor.</param>
    /// <returns>
    /// The placement of the taskbar. When the probe cannot locate the taskbar, the returned placement reports
    /// <see cref="TaskbarPlacement.IsOnCurrentMonitor"/> as <see langword="false"/> and the bar excludes no edge.
    /// </returns>
    public static TaskbarPlacement GetTaskbarPlacement(nint windowHandle)
    {
        var state = QueryState();
        var monitor = GetMonitorBounds(windowHandle);

        if (TryQueryFromShell(out var shellBounds, out var shellEdge))
        {
            return new TaskbarPlacement(state, shellEdge, shellBounds, Intersects(shellBounds, monitor));
        }

        var taskbarWindow = NativeMethods.FindWindowW(TaskbarWindowClass, null);

        if (taskbarWindow == nint.Zero || !NativeMethods.GetWindowRect(taskbarWindow, out var rect))
        {
            return new TaskbarPlacement(state, DockEdge.Bottom, PixelRect.Empty, IsOnCurrentMonitor: false);
        }

        var bounds = ToPixelRect(rect);

        return new TaskbarPlacement(state, DeriveEdge(bounds, monitor), bounds, Intersects(bounds, monitor));
    }

    private static bool TryQueryFromShell(out PixelRect bounds, out DockEdge edge)
    {
        var data = new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        };

        if (NativeMethods.SHAppBarMessage(AbmGetTaskbarPos, ref data) != 0
            && AppBarEdgeMapping.TryFromAppBarEdge(data.uEdge, out edge))
        {
            bounds = ToPixelRect(data.rc);
            return true;
        }

        bounds = PixelRect.Empty;
        edge = default;
        return false;
    }

    private static TaskbarState QueryState()
    {
        var data = new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
        };

        var state = NativeMethods.SHAppBarMessage(AbmGetState, ref data);

        return (state & (nint)AbsAutoHide) != 0 ? TaskbarState.AutoHide : TaskbarState.Visible;
    }

    private static PixelRect GetMonitorBounds(nint windowHandle)
    {
        var monitor = NativeMethods.MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        var info = new MONITORINFO
        {
            cbSize = Marshal.SizeOf<MONITORINFO>(),
        };

        return monitor != nint.Zero && NativeMethods.GetMonitorInfoW(monitor, ref info)
            ? ToPixelRect(info.rcMonitor)
            : PixelRect.Empty;
    }

    /// <summary>
    /// Derives the edge of a taskbar from its rectangle: a wide taskbar sits at the top or at the bottom, a tall one at
    /// the left or at the right, and the half of the monitor it falls in tells which.
    /// </summary>
    private static DockEdge DeriveEdge(PixelRect taskbar, PixelRect monitor)
    {
        if (taskbar.Height <= taskbar.Width)
        {
            var taskbarCentre = (long)taskbar.Y + (taskbar.Height / 2);
            var monitorCentre = (long)monitor.Y + (monitor.Height / 2);

            return taskbarCentre < monitorCentre ? DockEdge.Top : DockEdge.Bottom;
        }

        var taskbarMiddle = (long)taskbar.X + (taskbar.Width / 2);
        var monitorMiddle = (long)monitor.X + (monitor.Width / 2);

        return taskbarMiddle < monitorMiddle ? DockEdge.Left : DockEdge.Right;
    }

    private static bool Intersects(PixelRect first, PixelRect second)
        => first.X < second.Right
        && first.Right > second.X
        && first.Y < second.Bottom
        && first.Bottom > second.Y;

    private static PixelRect ToPixelRect(RECT rect)
        => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
}
