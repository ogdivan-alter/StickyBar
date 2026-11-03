using StickyDock.Core.Interaction;
using StickyDock.Core.Services;

namespace StickyDock.Tests.Interaction;

/// <summary>
/// ADR-004 D3: the presence matrix is the machine readable form of the <c>Visibility</c> the view declares for each
/// element, so a region can only be active while the element that paints it exists. It is what makes a stuck region
/// unrepresentable, and it is a total function of <c>(DockRegion, DockState)</c>, so the whole table is verified here.
/// </summary>
public sealed class DockRegionPresenceTests
{
    [Theory]
    [InlineData(DockRegion.None, DockState.Tab, false)]
    [InlineData(DockRegion.None, DockState.Arming, false)]
    [InlineData(DockRegion.None, DockState.Expanded, false)]
    [InlineData(DockRegion.None, DockState.Collapsing, false)]
    [InlineData(DockRegion.Tab, DockState.Tab, true)]
    [InlineData(DockRegion.Tab, DockState.Arming, true)]
    [InlineData(DockRegion.Tab, DockState.Expanded, true)]
    [InlineData(DockRegion.Tab, DockState.Collapsing, true)]
    [InlineData(DockRegion.Bar, DockState.Tab, false)]
    [InlineData(DockRegion.Bar, DockState.Arming, false)]
    [InlineData(DockRegion.Bar, DockState.Expanded, true)]
    [InlineData(DockRegion.Bar, DockState.Collapsing, true)]
    [InlineData(DockRegion.Ribbon, DockState.Tab, false)]
    [InlineData(DockRegion.Ribbon, DockState.Arming, false)]
    [InlineData(DockRegion.Ribbon, DockState.Expanded, true)]
    [InlineData(DockRegion.Ribbon, DockState.Collapsing, true)]
    public void PresenceMatrix_MatchesTheTable(DockRegion region, DockState state, bool expected)
    {
        Assert.Equal(expected, DockRegionPresence.IsPresent(region, state));
    }

    [Fact]
    public void IsContent_OnlyForTheBarAndTheRibbons()
    {
        Assert.True(DockRegionPresence.IsContent(DockRegion.Bar));
        Assert.True(DockRegionPresence.IsContent(DockRegion.Ribbon));
        Assert.False(DockRegionPresence.IsContent(DockRegion.Tab));
        Assert.False(DockRegionPresence.IsContent(DockRegion.None));
    }
}
