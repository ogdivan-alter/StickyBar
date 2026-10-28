using StickyDock.Core.Docking;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Translates <c>APPBARDATA.uEdge</c> (<c>ABE_*</c>) to the product's <see cref="DockEdge"/> and back.
/// </summary>
/// <remarks>
/// The two orders differ (<c>ABE_LEFT = 0</c>, <c>ABE_TOP = 1</c>) and the product persists <see cref="DockEdge"/> as a
/// number, so the translation lives in this single table with its own test (ADR-002).
/// </remarks>
public static class AppBarEdgeMapping
{
    /// <summary><c>ABE_LEFT</c>.</summary>
    public const uint AppBarLeft = 0;

    /// <summary><c>ABE_TOP</c>.</summary>
    public const uint AppBarTop = 1;

    /// <summary><c>ABE_RIGHT</c>.</summary>
    public const uint AppBarRight = 2;

    /// <summary><c>ABE_BOTTOM</c>.</summary>
    public const uint AppBarBottom = 3;

    /// <summary>
    /// Translates a native appbar edge to <see cref="DockEdge"/>.
    /// </summary>
    /// <param name="appBarEdge">Value of <c>APPBARDATA.uEdge</c>.</param>
    /// <returns>The matching dock edge.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value is not one of the <c>ABE_*</c> constants.</exception>
    public static DockEdge FromAppBarEdge(uint appBarEdge) => appBarEdge switch
    {
        AppBarLeft => DockEdge.Left,
        AppBarTop => DockEdge.Top,
        AppBarRight => DockEdge.Right,
        AppBarBottom => DockEdge.Bottom,
        _ => throw new ArgumentOutOfRangeException(
            nameof(appBarEdge),
            appBarEdge,
            "APPBARDATA.uEdge must be one of the ABE_* constants."),
    };

    /// <summary>
    /// Translates a native appbar edge, reporting failure instead of throwing.
    /// </summary>
    /// <param name="appBarEdge">Value of <c>APPBARDATA.uEdge</c>.</param>
    /// <param name="edge">The matching dock edge, or the default value when the input is unknown.</param>
    /// <returns><see langword="true"/> when the value is one of the <c>ABE_*</c> constants.</returns>
    /// <remarks>Used by the taskbar probe, which prefers to exclude no edge over excluding the wrong one.</remarks>
    public static bool TryFromAppBarEdge(uint appBarEdge, out DockEdge edge)
    {
        switch (appBarEdge)
        {
            case AppBarLeft:
                edge = DockEdge.Left;
                return true;
            case AppBarTop:
                edge = DockEdge.Top;
                return true;
            case AppBarRight:
                edge = DockEdge.Right;
                return true;
            case AppBarBottom:
                edge = DockEdge.Bottom;
                return true;
            default:
                edge = default;
                return false;
        }
    }

    /// <summary>
    /// Translates a <see cref="DockEdge"/> to the native appbar edge.
    /// </summary>
    /// <param name="edge">Dock edge of the product.</param>
    /// <returns>The matching <c>ABE_*</c> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="DockEdge"/>.</exception>
    public static uint ToAppBarEdge(DockEdge edge) => edge switch
    {
        DockEdge.Left => AppBarLeft,
        DockEdge.Top => AppBarTop,
        DockEdge.Right => AppBarRight,
        DockEdge.Bottom => AppBarBottom,
        _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unknown dock edge."),
    };
}
