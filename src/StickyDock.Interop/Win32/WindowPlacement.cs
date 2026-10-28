using StickyDock.Core.Docking;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Window movement driven by <c>SetWindowPos</c>, never by <c>Window.Left</c>/<c>Window.Top</c>.
/// </summary>
/// <remarks>
/// WPF exposes the position of a window in device-independent units whose meaning is ambiguous when the monitors of
/// the session have different scales (R-05). Every rectangle handled here is in device pixels, which the window manager
/// takes as they are under <c>PerMonitorV2</c> (AMP-03).
/// </remarks>
public static class WindowPlacement
{
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint WmSysCommand = 0x0112;
    private const nint ScMoveWithMouse = 0xF012;

    /// <summary>
    /// Moves and resizes a window without changing its position in the z-order and without activating it.
    /// </summary>
    /// <param name="windowHandle">Handle of the window.</param>
    /// <param name="bounds">Authoritative rectangle, in device pixels.</param>
    public static void MoveResize(nint windowHandle, PixelRect bounds)
    {
        if (windowHandle == nint.Zero)
        {
            return;
        }

        _ = NativeMethods.SetWindowPos(
            windowHandle,
            nint.Zero,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SwpNoZOrder | SwpNoActivate);
    }

    /// <summary>
    /// Reads the rectangle a window occupies.
    /// </summary>
    /// <param name="windowHandle">Handle of the window.</param>
    /// <returns>The rectangle in device pixels, or <see cref="PixelRect.Empty"/> when it cannot be read.</returns>
    public static PixelRect GetBounds(nint windowHandle)
    {
        if (windowHandle == nint.Zero || !NativeMethods.GetWindowRect(windowHandle, out var rect))
        {
            return PixelRect.Empty;
        }

        return new PixelRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Hands the drag of a window to the native move loop of the system.
    /// </summary>
    /// <param name="windowHandle">Handle of the window.</param>
    /// <remarks>
    /// <para>
    /// Contingency of ADR-002 for a window with no non-client caption: the bar is entirely client area so that the
    /// pointer events keep arriving, and <c>WM_EXITSIZEMOVE</c> still reports the end of the drag.
    /// </para>
    /// <para>
    /// The loop is started with <c>WM_SYSCOMMAND(SC_MOVE | 0x0002)</c>, the "move with the mouse" form of the system
    /// menu command, and not with <c>WM_NCLBUTTONDOWN(HTCAPTION)</c>: measured on this build, the non-client button
    /// message returns immediately without entering the modal loop, while the system command does enter it and follows
    /// the cursor until the button is released.
    /// </para>
    /// </remarks>
    public static void BeginNativeMove(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return;
        }

        _ = NativeMethods.ReleaseCapture();
        _ = NativeMethods.SendMessage(windowHandle, WmSysCommand, ScMoveWithMouse, nint.Zero);
    }
}
