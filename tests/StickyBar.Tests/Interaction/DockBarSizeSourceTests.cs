using StickyBar.Core.Docking;
using StickyBar.Core.Interaction;
using StickyBar.Core.Services;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Interaction;

/// <summary>
/// ADR-003, D2: the footprint the coordinator applies is declared for the dock state, so it never depends on the size the
/// window happens to have (D-01) and the bar grows inward when it deploys (E3-CA1).
/// </summary>
public sealed class DockBarSizeSourceTests
{
    [Fact]
    public void Tab_UsesTheCollapsedFootprint()
    {
        var source = CreateSource(DockState.Tab);

        Assert.Equal(new DockBarSize(120d, 36d), source.GetCurrentSize(DockEdge.Top));
    }

    [Fact]
    public void Arming_UsesTheCollapsedFootprint()
    {
        // The micro-expansion of E3-CA1 does not change the footprint: it is a render transform of the tab.
        var source = CreateSource(DockState.Arming);

        Assert.Equal(new DockBarSize(120d, 36d), source.GetCurrentSize(DockEdge.Top));
    }

    [Fact]
    public void Expanded_UsesTheExpandedFootprint()
    {
        var source = CreateSource(DockState.Expanded);

        Assert.Equal(new DockBarSize(168d, 96d), source.GetCurrentSize(DockEdge.Top));
    }

    [Fact]
    public void Collapsing_KeepsTheExpandedFootprint()
    {
        // The window only shrinks once the slide-out has settled, so the deploy animation is not clipped midway.
        var source = CreateSource(DockState.Collapsing);

        Assert.Equal(new DockBarSize(168d, 96d), source.GetCurrentSize(DockEdge.Top));
    }

    [Theory]
    [InlineData(DockState.Tab, 36d, 120d)]
    [InlineData(DockState.Arming, 36d, 120d)]
    [InlineData(DockState.Expanded, 96d, 168d)]
    [InlineData(DockState.Collapsing, 96d, 168d)]
    public void VerticalEdge_SwapsTheFootprint(DockState state, double expectedWidth, double expectedHeight)
    {
        var source = CreateSource(state);

        Assert.Equal(new DockBarSize(expectedWidth, expectedHeight), source.GetCurrentSize(DockEdge.Left));
        Assert.Equal(new DockBarSize(expectedWidth, expectedHeight), source.GetCurrentSize(DockEdge.Right));
    }

    [Fact]
    public void WithoutAnAnchor_LaysTheBarOutHorizontally()
    {
        var source = CreateSource(DockState.Expanded);

        Assert.Equal(new DockBarSize(168d, 96d), source.GetCurrentSize(anchor: null));
    }

    [Fact]
    public void CustomMetrics_AreUsed()
    {
        var states = new FakeDockStateService();
        states.SetState(DockState.Expanded);
        var source = new DockBarSizeSource(states, new DockBarMetrics(200d, 40d, 320d, 120d));

        Assert.Equal(new DockBarSize(320d, 120d), source.GetCurrentSize(DockEdge.Top));
        Assert.Equal(new DockBarSize(120d, 320d), source.GetCurrentSize(DockEdge.Right));
    }

    [Fact]
    public void TheStateIsReadOnEveryCall()
    {
        // No cache: the size follows the controller, which is what makes NotifyBarSizeChanged() meaningful (D-09).
        var states = new FakeDockStateService();
        var source = new DockBarSizeSource(states, DockBarMetrics.Default);

        Assert.Equal(new DockBarSize(120d, 36d), source.GetCurrentSize(DockEdge.Top));

        states.SetState(DockState.Expanded);

        Assert.Equal(new DockBarSize(168d, 96d), source.GetCurrentSize(DockEdge.Top));
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DockBarSizeSource(null!, DockBarMetrics.Default));
    }

    private static DockBarSizeSource CreateSource(DockState state)
    {
        var states = new FakeDockStateService();
        states.SetState(state);

        return new DockBarSizeSource(states, DockBarMetrics.Default);
    }
}
