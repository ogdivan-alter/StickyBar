using StickyDock.Core.Docking;
using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// The translation between <c>APPBARDATA.uEdge</c> and <see cref="DockEdge"/> is the only place where the two orders
/// meet; the persisted values of <c>settings.json</c> depend on it (ADR-002).
/// </summary>
public sealed class AppBarEdgeMappingTests
{
    [Theory]
    [InlineData(AppBarEdgeMapping.AppBarLeft, DockEdge.Left)]
    [InlineData(AppBarEdgeMapping.AppBarTop, DockEdge.Top)]
    [InlineData(AppBarEdgeMapping.AppBarRight, DockEdge.Right)]
    [InlineData(AppBarEdgeMapping.AppBarBottom, DockEdge.Bottom)]
    public void FromAppBarEdge_MapsEveryNativeEdge(uint appBarEdge, DockEdge expected)
    {
        Assert.Equal(expected, AppBarEdgeMapping.FromAppBarEdge(appBarEdge));
    }

    [Theory]
    [InlineData(DockEdge.Left, AppBarEdgeMapping.AppBarLeft)]
    [InlineData(DockEdge.Top, AppBarEdgeMapping.AppBarTop)]
    [InlineData(DockEdge.Right, AppBarEdgeMapping.AppBarRight)]
    [InlineData(DockEdge.Bottom, AppBarEdgeMapping.AppBarBottom)]
    public void ToAppBarEdge_MapsEveryDockEdge(DockEdge edge, uint expected)
    {
        Assert.Equal(expected, AppBarEdgeMapping.ToAppBarEdge(edge));
    }

    [Fact]
    public void TheTwoOrdersDiffer_SoTheTranslationCannotBeACast()
    {
        // ABE_LEFT = 0 and ABE_TOP = 1, while DockEdge.Top = 0 and DockEdge.Left = 1.
        Assert.NotEqual((uint)DockEdge.Top, AppBarEdgeMapping.AppBarTop);
        Assert.NotEqual((uint)DockEdge.Left, AppBarEdgeMapping.AppBarLeft);
    }

    [Fact]
    public void EveryEdge_RoundTripsThroughTheNativeValue()
    {
        foreach (var edge in Enum.GetValues<DockEdge>())
        {
            var appBarEdge = AppBarEdgeMapping.ToAppBarEdge(edge);

            Assert.Equal(edge, AppBarEdgeMapping.FromAppBarEdge(appBarEdge));
        }
    }

    [Theory]
    [InlineData(uint.MaxValue)]
    [InlineData(4u)]
    public void FromAppBarEdge_WithAnUnknownValue_Throws(uint appBarEdge)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AppBarEdgeMapping.FromAppBarEdge(appBarEdge));
    }

    [Fact]
    public void TryFromAppBarEdge_WithAnUnknownValue_ReportsFailureAndReturnsTheDefault()
    {
        var succeeded = AppBarEdgeMapping.TryFromAppBarEdge(4u, out var edge);

        Assert.False(succeeded);
        Assert.Equal(default(DockEdge), edge);
    }

    [Fact]
    public void TryFromAppBarEdge_WithAKnownValue_ReportsSuccess()
    {
        var succeeded = AppBarEdgeMapping.TryFromAppBarEdge(AppBarEdgeMapping.AppBarBottom, out var edge);

        Assert.True(succeeded);
        Assert.Equal(DockEdge.Bottom, edge);
    }
}
