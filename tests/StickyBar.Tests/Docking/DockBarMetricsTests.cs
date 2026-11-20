using StickyBar.Core.Docking;

namespace StickyBar.Tests.Docking;

/// <summary>
/// ADR-003, D2: the footprint of the bar is declared for the state and the orientation, never measured, which is what
/// closes D-01 and keeps the collapsed window of E1 at 120×36 (E1-CA4, D-15).
/// </summary>
public sealed class DockBarMetricsTests
{
    [Fact]
    public void Default_MatchesTheDesign()
    {
        var metrics = DockBarMetrics.Default;

        Assert.Equal(120d, metrics.CollapsedLength);
        Assert.Equal(36d, metrics.CollapsedThickness);
        Assert.Equal(168d, metrics.ExpandedLength);
        Assert.Equal(96d, metrics.ExpandedThickness);
    }

    [Fact]
    public void CollapsedFootprint_MatchesTheWindowOfE1()
    {
        // E1-CA4: while the bar is idle its footprint is the window of E1, so it intercepts only the tab.
        Assert.Equal(new DockBarSize(120d, 36d), DockBarMetrics.Default.GetSize(expanded: false, DockEdge.Top));
        Assert.Equal(new DockBarSize(120d, 36d), DockBarMetrics.Default.GetSize(expanded: false, anchor: null));
    }

    [Fact]
    public void HorizontalEdges_UseTheLengthAsWidth()
    {
        Assert.Equal(new DockBarSize(168d, 96d), DockBarMetrics.Default.GetSize(expanded: true, DockEdge.Top));
        Assert.Equal(new DockBarSize(168d, 96d), DockBarMetrics.Default.GetSize(expanded: true, DockEdge.Bottom));
        Assert.Equal(new DockBarSize(120d, 36d), DockBarMetrics.Default.GetSize(expanded: false, DockEdge.Bottom));
    }

    [Fact]
    public void VerticalEdges_UseTheLengthAsHeight()
    {
        Assert.Equal(new DockBarSize(36d, 120d), DockBarMetrics.Default.GetSize(expanded: false, DockEdge.Left));
        Assert.Equal(new DockBarSize(36d, 120d), DockBarMetrics.Default.GetSize(expanded: false, DockEdge.Right));
        Assert.Equal(new DockBarSize(96d, 168d), DockBarMetrics.Default.GetSize(expanded: true, DockEdge.Left));
    }

    [Fact]
    public void AFloatingBar_IsLaidOutHorizontally()
    {
        // The window of E1 is designed horizontally, so a floating bar has no length and thickness to swap.
        Assert.Equal(new DockBarSize(168d, 96d), DockBarMetrics.Default.GetSize(expanded: true, anchor: null));
    }

    [Fact]
    public void ExpandedFootprint_ContainsTheCollapsedOneOnEveryEdge()
    {
        DockEdge?[] anchors = [DockEdge.Top, DockEdge.Left, DockEdge.Right, DockEdge.Bottom, null];

        foreach (var anchor in anchors)
        {
            var collapsed = DockBarMetrics.Default.GetSize(expanded: false, anchor);
            var expanded = DockBarMetrics.Default.GetSize(expanded: true, anchor);

            Assert.True(expanded.WidthDips >= collapsed.WidthDips, $"width on {anchor}");
            Assert.True(expanded.HeightDips >= collapsed.HeightDips, $"height on {anchor}");
        }
    }
}
