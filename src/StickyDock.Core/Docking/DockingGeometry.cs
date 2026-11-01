namespace StickyDock.Core.Docking;

/// <summary>
/// Rectangle in device pixels, with the origin at the top-left corner of the virtual screen.
/// </summary>
/// <remarks>
/// Device pixels are the only unambiguous unit when the monitors of a session have different scales (R-05), so every
/// decision of the docking is expressed in this type (ADR-002).
/// </remarks>
/// <param name="X">Left coordinate.</param>
/// <param name="Y">Top coordinate.</param>
/// <param name="Width">Width of the rectangle.</param>
/// <param name="Height">Height of the rectangle.</param>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Gets an empty rectangle.
    /// </summary>
    public static PixelRect Empty => new(0, 0, 0, 0);

    /// <summary>
    /// Gets the right edge of the rectangle (exclusive).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Gets the bottom edge of the rectangle (exclusive).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Gets a value indicating whether the rectangle has no area.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Gets a value indicating whether a point falls inside the rectangle.
    /// </summary>
    /// <param name="x">Horizontal coordinate of the point, in device pixels.</param>
    /// <param name="y">Vertical coordinate of the point, in device pixels.</param>
    /// <returns><see langword="true"/> when the point is inside; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The right and bottom edges are exclusive, so two rectangles that share an edge never both contain the point where
    /// they meet. It expresses the same convention as <see cref="Right"/> and <see cref="Bottom"/>.
    /// </remarks>
    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;
}

/// <summary>
/// Monitor that hosts the bar, in device pixels, together with its DPI.
/// </summary>
/// <param name="Bounds">Full bounds of the monitor.</param>
/// <param name="WorkArea">Area left free by the taskbar and other appbars.</param>
/// <param name="Dpi">Effective DPI of the monitor (96 = 100 %).</param>
public readonly record struct MonitorGeometry(PixelRect Bounds, PixelRect WorkArea, int Dpi)
{
    /// <summary>
    /// Gets the device pixels per device-independent unit.
    /// </summary>
    public double Scale => Dpi <= 0 ? 1d : Dpi / 96d;
}

/// <summary>
/// Size the bar has when WPF lays it out, in device-independent units.
/// </summary>
/// <param name="WidthDips">Width in device-independent units.</param>
/// <param name="HeightDips">Height in device-independent units.</param>
public readonly record struct DockBarSize(double WidthDips, double HeightDips);

/// <summary>
/// Whether the taskbar is permanently visible or hides itself.
/// </summary>
public enum TaskbarState
{
    /// <summary>The taskbar is always visible.</summary>
    Visible = 0,

    /// <summary>The taskbar hides itself and reappears when the pointer reaches its edge.</summary>
    AutoHide = 1,
}

/// <summary>
/// Taskbar as seen from the monitor that hosts the bar.
/// </summary>
/// <param name="State">Whether the taskbar auto-hides.</param>
/// <param name="Edge">Edge of the screen the taskbar occupies.</param>
/// <param name="Bounds">Bounds of the taskbar, in device pixels.</param>
/// <param name="IsOnCurrentMonitor">
/// <see langword="true"/> when the taskbar lives on the monitor that hosts the bar. When the probe cannot determine it,
/// this is <see langword="false"/> and the bar excludes no edge, rather than excluding a wrong one.
/// </param>
public readonly record struct TaskbarPlacement(
    TaskbarState State,
    DockEdge Edge,
    PixelRect Bounds,
    bool IsOnCurrentMonitor);
