using StickyBar.Core.Configuration;
using StickyBar.Core.Docking;
using StickyBar.Core.Interaction;
using StickyBar.Core.Services;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Docking;

/// <summary>
/// E4-CA1, E4-CA6, E4-CA7: dynamic bar sizing with ribbons, minimum tab length at zero content, and work-area
/// containment when a ribbon opens on all four edges and in floating mode.
/// </summary>
public sealed class RibbonDynamicSizingTests
{
    private static readonly PixelRect MonitorBounds = new(0, 0, 1920, 1080);
    private static readonly PixelRect WorkArea = new(0, 0, 1920, 1040);

    [Fact]
    public void GetDynamicSize_Collapsed_ReturnsTabSizeRegardlessOfRibbons()
    {
        var horizontal = DockBarMetrics.Default.GetDynamicSize(
            expanded: false,
            DockEdge.Top,
            ribbonCount: 5,
            barItemCount: 3,
            hasOpenRibbon: true);

        var vertical = DockBarMetrics.Default.GetDynamicSize(
            expanded: false,
            DockEdge.Left,
            ribbonCount: 5,
            barItemCount: 3,
            hasOpenRibbon: true);

        Assert.Equal(new DockBarSize(120d, 36d), horizontal);
        Assert.Equal(new DockBarSize(36d, 120d), vertical);
    }

    [Fact]
    public void GetDynamicSize_ExpandedWithZeroRibbonsAndZeroItems_EqualsTabLength()
    {
        // E4-CA6: with 0 ribbons and 0 items, the bar length equals the idle tab length (120).
        var size = DockBarMetrics.Default.GetDynamicSize(
            expanded: true,
            DockEdge.Top,
            ribbonCount: 0,
            barItemCount: 0,
            hasOpenRibbon: false);

        Assert.Equal(DockBarMetrics.Default.CollapsedLength, size.WidthDips);
        Assert.Equal(DockBarMetrics.Default.ExpandedThickness, size.HeightDips);
    }

    [Fact]
    public void GetDynamicSize_ExpandedWithRibbons_GrowsLengthDynamically()
    {
        var one = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Top, ribbonCount: 1, barItemCount: 0, hasOpenRibbon: false);
        var two = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Top, ribbonCount: 2, barItemCount: 0, hasOpenRibbon: false);
        var three = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Top, ribbonCount: 3, barItemCount: 0, hasOpenRibbon: false);

        Assert.True(two.WidthDips > one.WidthDips);
        Assert.True(three.WidthDips > two.WidthDips);
    }

    [Fact]
    public void GetDynamicSize_WithOpenRibbon_ExpandsPerpendicularly()
    {
        var horizontalClosed = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Top, 2, 0, hasOpenRibbon: false);
        var horizontalOpen = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Top, 2, 0, hasOpenRibbon: true);

        Assert.Equal(horizontalClosed.WidthDips, horizontalOpen.WidthDips);
        Assert.True(horizontalOpen.HeightDips > horizontalClosed.HeightDips);

        var verticalClosed = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Left, 2, 0, hasOpenRibbon: false);
        var verticalOpen = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Left, 2, 0, hasOpenRibbon: true);

        Assert.Equal(verticalClosed.HeightDips, verticalOpen.HeightDips);
        Assert.True(verticalOpen.WidthDips > verticalClosed.WidthDips);
    }

    [Theory]
    [InlineData(DockEdge.Top)]
    [InlineData(DockEdge.Bottom)]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    public void OpenRibbon_OnAllFourEdges_StaysInsideWorkArea(DockEdge edge)
    {
        // E4-CA7: opening a ribbon on any docked edge keeps the window strictly inside the work area.
        var taskbarEdge = edge == DockEdge.Bottom ? DockEdge.Top : DockEdge.Bottom;
        var events = new FakeDockingEventSource();
        var geometry = new FakeWindowGeometrySource
        {
            Monitor = new MonitorGeometry(MonitorBounds, WorkArea, 96),
            Taskbar = new TaskbarPlacement(TaskbarState.Visible, taskbarEdge, PixelRect.Empty, IsOnCurrentMonitor: true),
        };
        var host = new FakeDockWindowHost();
        var states = new StubDockStateService(DockState.Expanded);
        var layout = new StubRibbonLayoutState { RibbonCount = 3, HasOpenRibbon = false };
        var sizeSource = new DockBarSizeSource(states, DockBarMetrics.Default, layout);
        var logger = new RecordingLogger();
        var document = new ConfigurationDocument(new RecordingConfigurationStore(), new StickyBarConfiguration { DockEdge = edge }, logger);

        using var coordinator = new DockPlacementCoordinator(events, geometry, host, sizeSource, document, logger);
        coordinator.Start();

        layout.HasOpenRibbon = true;
        coordinator.NotifyBarSizeChanged();

        Assert.True(host.Bounds.X >= WorkArea.X);
        Assert.True(host.Bounds.Y >= WorkArea.Y);
        Assert.True(host.Bounds.Right <= WorkArea.Right);
        Assert.True(host.Bounds.Bottom <= WorkArea.Bottom);
    }

    [Fact]
    public void OpenRibbon_InFloatingMode_StaysInsideWorkArea()
    {
        var events = new FakeDockingEventSource();
        var geometry = new FakeWindowGeometrySource
        {
            Monitor = new MonitorGeometry(MonitorBounds, WorkArea, 96),
            Taskbar = new TaskbarPlacement(TaskbarState.Visible, DockEdge.Bottom, PixelRect.Empty, IsOnCurrentMonitor: true),
        };
        var host = new FakeDockWindowHost { Bounds = new PixelRect(1780, 990, 120, 36) };
        var states = new StubDockStateService(DockState.Expanded);
        var layout = new StubRibbonLayoutState { RibbonCount = 2, HasOpenRibbon = true };
        var sizeSource = new DockBarSizeSource(states, DockBarMetrics.Default, layout);
        var logger = new RecordingLogger();
        var document = new ConfigurationDocument(new RecordingConfigurationStore(), new StickyBarConfiguration { DockEdge = null }, logger);

        using var coordinator = new DockPlacementCoordinator(events, geometry, host, sizeSource, document, logger);
        coordinator.Start();

        Assert.True(host.Bounds.X >= WorkArea.X);
        Assert.True(host.Bounds.Y >= WorkArea.Y);
        Assert.True(host.Bounds.Right <= WorkArea.Right);
        Assert.True(host.Bounds.Bottom <= WorkArea.Bottom);
    }

    [Fact]
    public void GetDynamicSize_OpenRibbon_GrowsProportionallyWithItemCount()
    {
        var empty = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Left, 1, 0, hasOpenRibbon: true, openRibbonItemCount: 0);
        var one = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Left, 1, 0, hasOpenRibbon: true, openRibbonItemCount: 1);
        var three = DockBarMetrics.Default.GetDynamicSize(true, DockEdge.Left, 1, 0, hasOpenRibbon: true, openRibbonItemCount: 3);

        Assert.True(empty.WidthDips < 200d);
        Assert.Equal(DockBarMetrics.OpenRibbonItemExtent, one.WidthDips - empty.WidthDips);
        Assert.Equal(2d * DockBarMetrics.OpenRibbonItemExtent, three.WidthDips - one.WidthDips);
    }

    private sealed class StubDockStateService(DockState state) : IDockStateService
    {
        public DockState State => state;

        public bool IsPinned { get; set; }

        public bool IsContextMenuOpen { get; set; }

        public event EventHandler<DockStateChangedEventArgs>? StateChanged
        {
            add { }
            remove { }
        }

        public void Expand() { }

        public void CollapseToTab() { }

        public void BeginDrag() { }

        public void EndDrag() { }
    }

    private sealed class StubRibbonLayoutState : IRibbonLayoutState
    {
        public int RibbonCount { get; set; }

        public int BarItemCount { get; set; }

        public bool HasOpenRibbon { get; set; }
    }
}
