namespace StickyDock.Interop.Win32;

/// <summary>
/// Reads the position of the pointer from the system, in device pixels.
/// </summary>
/// <remarks>
/// <para>
/// It is the only way to know where the pointer is without listening for mouse input: no low-level hook and no polling are
/// installed, so the resting cost stays at zero (RNF-02, A6). It serves the reconciliation of the window region, which has
/// to correct a report the window may have missed while it was resizing under a still pointer (D-E3-01).
/// </para>
/// <para>
/// The application is declared <c>PerMonitorV2</c> (AMP-03), so the coordinates are physical pixels of the virtual screen,
/// the same unit <see cref="WindowPlacement.GetBounds"/> returns for the window.
/// </para>
/// </remarks>
public static class CursorProbe
{
    /// <summary>
    /// Tries to read the position of the pointer.
    /// </summary>
    /// <param name="x">Horizontal coordinate of the pointer, in device pixels.</param>
    /// <param name="y">Vertical coordinate of the pointer, in device pixels.</param>
    /// <returns><see langword="true"/> when the position was read; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetPosition(out int x, out int y)
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            x = 0;
            y = 0;
            return false;
        }

        x = point.X;
        y = point.Y;
        return true;
    }
}
