using StickyDock.Core.Docking;
using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// Verifies that the window movement wrapper degrades safely when it is given no window.
/// </summary>
/// <remarks>
/// The wrapper cannot be exercised against a real window in a unit test; what is checked here is that the guard which
/// keeps the docking off an invalid handle is in place, because that guard is what runs during the teardown
/// (E2-CA5) and whenever the coordinator is used before the window exists.
/// </remarks>
public sealed class WindowPlacementTests
{
    [Fact]
    public void GetBounds_WithoutWindow_ReturnsEmpty()
    {
        var bounds = WindowPlacement.GetBounds(nint.Zero);

        Assert.True(bounds.IsEmpty);
        Assert.Equal(PixelRect.Empty, bounds);
    }

    [Fact]
    public void MoveResize_WithoutWindow_DoesNothing()
    {
        var exception = Record.Exception(() => WindowPlacement.MoveResize(nint.Zero, new PixelRect(10, 20, 30, 40)));

        Assert.Null(exception);
    }

    [Fact]
    public void BeginNativeMove_WithoutWindow_DoesNothing()
    {
        var exception = Record.Exception(() => WindowPlacement.BeginNativeMove(nint.Zero));

        Assert.Null(exception);
    }
}
