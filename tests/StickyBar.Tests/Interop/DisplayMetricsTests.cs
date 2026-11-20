using StickyBar.Interop.Win32;

namespace StickyBar.Tests.Interop;

/// <summary>
/// Smoke test of the Win32 boundary: the safe wrapper must return a usable display size on a desktop session.
/// </summary>
public sealed class DisplayMetricsTests
{
    [Fact]
    public void GetPrimaryScreenSize_ReturnsPositiveDimensions()
    {
        var size = DisplayMetrics.GetPrimaryScreenSize();

        Assert.True(size.WidthInPixels > 0);
        Assert.True(size.HeightInPixels > 0);
    }
}
