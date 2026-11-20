using StickyDock.Core.Docking;

namespace StickyDock.Tests.Docking;

/// <summary>
/// E2-CA1 (snap within the 48-unit band, floating outside it), E2-CA2 (legal edge after a taskbar change),
/// E2-CA3 (resize without leaving the work area) and E2-CA4 (the taskbar edge is never a destination), all decided by
/// the pure calculator (ADR-002).
/// </summary>
public sealed class DockPlacementCalculatorTests
{
    private static readonly PixelRect MonitorBounds = new(0, 0, 1920, 1080);
    private static readonly PixelRect WorkArea = new(0, 0, 1920, 1040);
    private static readonly DockBarSize BarSize = new(120d, 36d);

    [Theory]
    [InlineData(DockEdge.Top, 885, 0, 150, 45)]
    [InlineData(DockEdge.Left, 0, 497, 150, 45)]
    [InlineData(DockEdge.Right, 1770, 497, 150, 45)]
    [InlineData(DockEdge.Bottom, 885, 995, 150, 45)]
    public void GetDockedBounds_At125Percent_MatchesTheReferenceRectangle(
        DockEdge edge,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight)
    {
        // Reference of ADR-002: work area 1920x1040, bar 120x36 DIPs, DPI 120 (scale 1.25).
        var monitor = CreateMonitor(120);

        var bounds = DockPlacementCalculator.GetDockedBounds(edge, monitor, BarSize);

        Assert.Equal(new PixelRect(expectedX, expectedY, expectedWidth, expectedHeight), bounds);
    }

    [Fact]
    public void GetDockedBounds_IsAlwaysInsideTheWorkArea()
    {
        (PixelRect Bounds, PixelRect WorkArea, int Dpi)[] scenarios =
        [
            (new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96),
            (new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 120),
            (new PixelRect(0, 0, 2560, 1440), new PixelRect(0, 0, 2560, 1400), 144),
            (new PixelRect(1920, 0, 1280, 1024), new PixelRect(1920, 0, 1280, 984), 120),
            (new PixelRect(0, 0, 800, 600), new PixelRect(0, 0, 800, 600), 96),
            (new PixelRect(0, 0, 60, 30), new PixelRect(0, 0, 60, 30), 96),
        ];

        foreach (var (bounds, workArea, dpi) in scenarios)
        {
            var monitor = new MonitorGeometry(bounds, workArea, dpi);

            foreach (var edge in Enum.GetValues<DockEdge>())
            {
                var docked = DockPlacementCalculator.GetDockedBounds(edge, monitor, BarSize);

                Assert.True(
                    docked.X >= workArea.X && docked.Y >= workArea.Y
                        && docked.Right <= workArea.Right && docked.Bottom <= workArea.Bottom,
                    $"Edge {edge} left the work area: {docked} is not inside {workArea}.");
            }
        }
    }

    [Fact]
    public void GetDockedBounds_WhenTheBarIsLargerThanTheWorkArea_ClipsItToFit()
    {
        // E2-CA3: after a resolution change the bar must stay on screen even if it no longer fits.
        var monitor = new MonitorGeometry(new PixelRect(0, 0, 200, 100), new PixelRect(0, 0, 200, 100), 96);
        var oversizedBar = new DockBarSize(400d, 120d);

        var bounds = DockPlacementCalculator.GetDockedBounds(DockEdge.Top, monitor, oversizedBar);

        Assert.Equal(new PixelRect(0, 0, 200, 100), bounds);
    }

    [Fact]
    public void ComputeSnapTarget_WithAGapOf47_SnapsToTheEdge()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, 47, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Equal(DockEdge.Top, target);
    }

    [Fact]
    public void ComputeSnapTarget_WithAGapOf48_SnapsToTheEdge()
    {
        // The band is inclusive: 48 is the documented threshold of A3.
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, 48, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Equal(DockEdge.Top, target);
    }

    [Fact]
    public void ComputeSnapTarget_WithAGapOf49_StaysFloating()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, 49, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Null(target);
    }

    [Theory]
    [InlineData(120, 59, true)]
    [InlineData(120, 61, false)]
    [InlineData(144, 71, true)]
    [InlineData(144, 73, false)]
    public void ComputeSnapTarget_ScalesTheBandWithTheDpi(int dpi, int topGap, bool expectedToSnap)
    {
        // 48 DIPs are 60 px at 125 % and 72 px at 150 %: the magnet feels the same at every scale.
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, topGap, 120, 36),
            CreateMonitor(dpi),
            TaskbarAt(DockEdge.Bottom));

        Assert.Equal(expectedToSnap ? DockEdge.Top : null, target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenTheTaskbarOccupiesTheNearestEdge_SnapsToTheNextAvailableEdge()
    {
        // The bar is touching the top edge, but the taskbar is there: the left edge wins instead (E2-CA4).
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(0, 10, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Top));

        Assert.Equal(DockEdge.Left, target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenTheTaskbarOccupiesTheOnlyEdgeInTheBand_StaysFloating()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, 10, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Top));

        Assert.Null(target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenTheBarIsPastTheEdge_StillSnaps()
    {
        // A bar dropped on top of the taskbar counts as distance zero: the user dragged it against the border.
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(-40, -30, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Equal(DockEdge.Top, target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenTwoEdgesTie_BreaksByDeclarationOrder()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(0, 0, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Equal(DockEdge.Top, target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenTheTaskbarIsOnAnotherMonitor_ExcludesNoEdge()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(400, 10, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Top, isOnCurrentMonitor: false));

        Assert.Equal(DockEdge.Top, target);
    }

    [Fact]
    public void ComputeSnapTarget_WhenNoEdgeIsClose_StaysFloating()
    {
        var target = DockPlacementCalculator.ComputeSnapTarget(
            new PixelRect(700, 500, 120, 36),
            CreateMonitor(96),
            TaskbarAt(DockEdge.Bottom));

        Assert.Null(target);
    }

    [Theory]
    [InlineData(DockEdge.Top, DockEdge.Top, false)]
    [InlineData(DockEdge.Top, DockEdge.Left, true)]
    [InlineData(DockEdge.Left, DockEdge.Left, false)]
    [InlineData(DockEdge.Left, DockEdge.Right, true)]
    [InlineData(DockEdge.Right, DockEdge.Right, false)]
    [InlineData(DockEdge.Right, DockEdge.Bottom, true)]
    [InlineData(DockEdge.Bottom, DockEdge.Bottom, false)]
    [InlineData(DockEdge.Bottom, DockEdge.Top, true)]
    public void IsEdgeAvailable_ExcludesOnlyTheTaskbarEdgeOfThisMonitor(
        DockEdge taskbarEdge,
        DockEdge candidate,
        bool expected)
    {
        var available = DockPlacementCalculator.IsEdgeAvailable(candidate, TaskbarAt(taskbarEdge));

        Assert.Equal(expected, available);
    }

    [Fact]
    public void IsEdgeAvailable_WhenTheTaskbarIsOnAnotherMonitor_AcceptsEveryEdge()
    {
        foreach (var edge in Enum.GetValues<DockEdge>())
        {
            Assert.True(
                DockPlacementCalculator.IsEdgeAvailable(edge, TaskbarAt(edge, isOnCurrentMonitor: false)));
        }
    }

    [Theory]
    [InlineData(DockEdge.Top, DockEdge.Top, DockEdge.Bottom)]
    [InlineData(DockEdge.Top, DockEdge.Left, DockEdge.Left)]
    [InlineData(DockEdge.Top, DockEdge.Right, DockEdge.Right)]
    [InlineData(DockEdge.Top, DockEdge.Bottom, DockEdge.Bottom)]
    [InlineData(DockEdge.Left, DockEdge.Top, DockEdge.Top)]
    [InlineData(DockEdge.Left, DockEdge.Left, DockEdge.Right)]
    [InlineData(DockEdge.Left, DockEdge.Right, DockEdge.Right)]
    [InlineData(DockEdge.Left, DockEdge.Bottom, DockEdge.Bottom)]
    [InlineData(DockEdge.Right, DockEdge.Top, DockEdge.Top)]
    [InlineData(DockEdge.Right, DockEdge.Left, DockEdge.Left)]
    [InlineData(DockEdge.Right, DockEdge.Right, DockEdge.Left)]
    [InlineData(DockEdge.Right, DockEdge.Bottom, DockEdge.Bottom)]
    [InlineData(DockEdge.Bottom, DockEdge.Top, DockEdge.Top)]
    [InlineData(DockEdge.Bottom, DockEdge.Left, DockEdge.Left)]
    [InlineData(DockEdge.Bottom, DockEdge.Right, DockEdge.Right)]
    [InlineData(DockEdge.Bottom, DockEdge.Bottom, DockEdge.Top)]
    public void ChooseDockEdge_KeepsTheCurrentEdgeOrMigratesToTheOppositeOne(
        DockEdge taskbarEdge,
        DockEdge currentEdge,
        DockEdge expected)
    {
        // E2-CA2: the current edge survives unless the taskbar just took it; then the bar crosses the screen.
        var chosen = DockPlacementCalculator.ChooseDockEdge(currentEdge, TaskbarAt(taskbarEdge));

        Assert.Equal(expected, chosen);
    }

    [Theory]
    [InlineData(DockEdge.Top, DockEdge.Left)]
    [InlineData(DockEdge.Left, DockEdge.Top)]
    [InlineData(DockEdge.Right, DockEdge.Top)]
    [InlineData(DockEdge.Bottom, DockEdge.Top)]
    public void ChooseDockEdge_WithoutACurrentEdge_ReturnsTheFirstAvailableEdge(
        DockEdge taskbarEdge,
        DockEdge expected)
    {
        var chosen = DockPlacementCalculator.ChooseDockEdge(null, TaskbarAt(taskbarEdge));

        Assert.Equal(expected, chosen);
    }

    [Theory]
    [InlineData(DockEdge.Top)]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    [InlineData(DockEdge.Bottom)]
    public void ChooseDockEdge_WhenTheTaskbarIsOnAnotherMonitor_KeepsTheCurrentEdge(DockEdge currentEdge)
    {
        var chosen = DockPlacementCalculator.ChooseDockEdge(currentEdge, TaskbarAt(currentEdge, isOnCurrentMonitor: false));

        Assert.Equal(currentEdge, chosen);
    }

    [Fact]
    public void ChooseDockEdge_WithoutACurrentEdgeAndWithTheTaskbarElsewhere_DefaultsToTheTop()
    {
        var chosen = DockPlacementCalculator.ChooseDockEdge(null, TaskbarAt(DockEdge.Top, isOnCurrentMonitor: false));

        Assert.Equal(DockEdge.Top, chosen);
    }

    [Fact]
    public void ClampIntoWorkArea_WhenTheBarAlreadyFits_ReturnsItUnchanged()
    {
        var bounds = new PixelRect(100, 100, 150, 45);

        var clamped = DockPlacementCalculator.ClampIntoWorkArea(bounds, WorkArea);

        Assert.Equal(bounds, clamped);
    }

    [Fact]
    public void ClampIntoWorkArea_WhenTheBarIsPastTheEdges_BringsItBackWhole()
    {
        var clamped = DockPlacementCalculator.ClampIntoWorkArea(new PixelRect(1800, 1030, 150, 45), WorkArea);

        Assert.Equal(new PixelRect(1770, 995, 150, 45), clamped);
    }

    [Fact]
    public void ClampIntoWorkArea_WhenTheBarHasNegativeCoordinates_MovesItIntoTheOrigin()
    {
        var clamped = DockPlacementCalculator.ClampIntoWorkArea(new PixelRect(-50, -30, 150, 45), WorkArea);

        Assert.Equal(new PixelRect(0, 0, 150, 45), clamped);
    }

    [Fact]
    public void ClampIntoWorkArea_WhenTheBarIsLargerThanTheWorkArea_ShrinksItToTheWorkArea()
    {
        var clamped = DockPlacementCalculator.ClampIntoWorkArea(new PixelRect(100, 100, 2000, 1200), WorkArea);

        Assert.Equal(WorkArea, clamped);
    }

    [Fact]
    public void ClampIntoWorkArea_WithAnEmptyRectangle_ReturnsItUnchanged()
    {
        var clamped = DockPlacementCalculator.ClampIntoWorkArea(PixelRect.Empty, WorkArea);

        Assert.Equal(PixelRect.Empty, clamped);
    }

    [Fact]
    public void GetNearestEdge_ReturnsTheNearestSide()
    {
        // Reference of ADR-003 §6.5: 400 px to the top, 595 to the bottom, 700 to the left and 1070 to the right.
        var nearest = DockPlacementCalculator.GetNearestEdge(new PixelRect(700, 400, 150, 45), WorkArea);

        Assert.Equal(DockEdge.Top, nearest);
    }

    [Theory]
    [InlineData(0, 300, DockEdge.Left)]
    [InlineData(1770, 300, DockEdge.Right)]
    [InlineData(885, 995, DockEdge.Bottom)]
    [InlineData(885, 10, DockEdge.Top)]
    public void GetNearestEdge_FindsTheSideTheBarIsTouching(int x, int y, DockEdge expected)
    {
        var nearest = DockPlacementCalculator.GetNearestEdge(new PixelRect(x, y, 150, 45), WorkArea);

        Assert.Equal(expected, nearest);
    }

    [Theory]
    [InlineData(0, 0, DockEdge.Top)]
    [InlineData(0, 995, DockEdge.Left)]
    public void GetNearestEdge_WhenTwoSidesTie_BreaksByDeclarationOrder(int x, int y, DockEdge expected)
    {
        var nearest = DockPlacementCalculator.GetNearestEdge(new PixelRect(x, y, 150, 45), WorkArea);

        Assert.Equal(expected, nearest);
    }

    [Fact]
    public void GetFloatingBounds_WithTheSameSize_ReturnsTheSameRect()
    {
        // Rule R4: recalculating twice in a row is an exact no-op, which is what keeps the placement idempotent.
        var current = new PixelRect(700, 400, 210, 120);

        var bounds = DockPlacementCalculator.GetFloatingBounds(
            current,
            DockEdge.Top,
            CreateMonitor(120),
            DockBarMetrics.Default.GetSize(expanded: true, anchor: null));

        Assert.Equal(current, bounds);
    }

    [Theory]
    [InlineData(DockEdge.Top)]
    [InlineData(DockEdge.Bottom)]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    public void GetFloatingBounds_KeepsTopLeftFixedRegardlessOfAnchor(DockEdge anchor)
    {
        // Fixed top-left anchor: resizing a floating bar keeps X = current.X and Y = current.Y.
        var bounds = DockPlacementCalculator.GetFloatingBounds(
            new PixelRect(700, 400, 150, 45),
            anchor,
            CreateMonitor(120),
            DockBarMetrics.Default.GetSize(expanded: true, anchor: null));

        Assert.Equal(new PixelRect(700, 400, 210, 120), bounds);
    }

    [Fact]
    public void GetFloatingBounds_DeployAndCollapse_ReturnsToExactTopLeftRect()
    {
        var monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        var collapsedRect = new PixelRect(104, 104, 120, 36);

        var expandedRect = DockPlacementCalculator.GetFloatingBounds(
            collapsedRect,
            DockEdge.Top,
            monitor,
            DockBarMetrics.Default.GetSize(expanded: true, anchor: null));

        Assert.Equal(new PixelRect(104, 104, 168, 96), expandedRect);

        var roundTripRect = DockPlacementCalculator.GetFloatingBounds(
            expandedRect,
            DockEdge.Top,
            monitor,
            DockBarMetrics.Default.GetSize(expanded: false, anchor: null));

        Assert.Equal(collapsedRect, roundTripRect);
    }

    [Fact]
    public void ExpandedBounds_ContainCollapsedBounds()
    {
        // Invariant I1: the bar grows inward from its outer border on every edge, at every scale and on every geometry.
        foreach (var monitor in CreateScenarios())
        {
            foreach (var edge in Enum.GetValues<DockEdge>())
            {
                var collapsed = DockPlacementCalculator.GetDockedBounds(
                    edge,
                    monitor,
                    DockBarMetrics.Default.GetSize(expanded: false, edge));

                var expanded = DockPlacementCalculator.GetDockedBounds(
                    edge,
                    monitor,
                    DockBarMetrics.Default.GetSize(expanded: true, edge));

                Assert.True(
                    expanded.X <= collapsed.X
                        && expanded.Y <= collapsed.Y
                        && expanded.Right >= collapsed.Right
                        && expanded.Bottom >= collapsed.Bottom,
                    $"Edge {edge} at DPI {monitor.Dpi}: {expanded} does not contain {collapsed}.");
            }
        }
    }

    [Fact]
    public void Growth_KeepsTheOuterBorderFixed()
    {
        // Invariant I2: the tab is the visual anchor, so the side touching the edge cannot move when the bar deploys.
        foreach (var monitor in CreateScenarios())
        {
            var workArea = monitor.WorkArea;

            foreach (var edge in Enum.GetValues<DockEdge>())
            {
                var collapsed = DockPlacementCalculator.GetDockedBounds(
                    edge,
                    monitor,
                    DockBarMetrics.Default.GetSize(expanded: false, edge));

                var expanded = DockPlacementCalculator.GetDockedBounds(
                    edge,
                    monitor,
                    DockBarMetrics.Default.GetSize(expanded: true, edge));

                switch (edge)
                {
                    case DockEdge.Top:
                        Assert.Equal(workArea.Y, expanded.Y);
                        Assert.Equal(collapsed.Y, expanded.Y);
                        break;

                    case DockEdge.Bottom:
                        Assert.Equal(workArea.Bottom, expanded.Bottom);
                        Assert.Equal(collapsed.Bottom, expanded.Bottom);
                        break;

                    case DockEdge.Left:
                        Assert.Equal(workArea.X, expanded.X);
                        Assert.Equal(collapsed.X, expanded.X);
                        break;

                    default:
                        Assert.Equal(workArea.Right, expanded.Right);
                        Assert.Equal(collapsed.Right, expanded.Right);
                        break;
                }
            }
        }
    }

    [Fact]
    public void ExpandedBounds_StayInsideWorkArea()
    {
        // E2-CA3 is kept after the deploy: the deployed bar never leaves the work area, even when it does not fit.
        foreach (var monitor in CreateScenarios())
        {
            var workArea = monitor.WorkArea;

            foreach (var edge in Enum.GetValues<DockEdge>())
            {
                var expanded = DockPlacementCalculator.GetDockedBounds(
                    edge,
                    monitor,
                    DockBarMetrics.Default.GetSize(expanded: true, edge));

                Assert.True(
                    expanded.X >= workArea.X && expanded.Y >= workArea.Y
                        && expanded.Right <= workArea.Right && expanded.Bottom <= workArea.Bottom,
                    $"Edge {edge} at DPI {monitor.Dpi} left the work area: {expanded} is not inside {workArea}.");
            }
        }
    }

    /// <summary>
    /// Monitors and scales the growth invariants are checked against, including a work area smaller than the bar.
    /// </summary>
    private static MonitorGeometry[] CreateScenarios() =>
    [
        new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96),
        new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 120),
        new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 144),
        new(new PixelRect(1920, 0, 1280, 1024), new PixelRect(1920, 0, 1280, 984), 120),
        new(new PixelRect(0, 0, 2560, 1440), new PixelRect(0, 0, 2560, 1400), 144),
        new(new PixelRect(0, 0, 800, 600), new PixelRect(0, 0, 800, 600), 96),
        new(new PixelRect(0, 0, 60, 30), new PixelRect(0, 0, 60, 30), 96),
    ];

    private static MonitorGeometry CreateMonitor(int dpi) => new(MonitorBounds, WorkArea, dpi);

    private static TaskbarPlacement TaskbarAt(DockEdge edge, bool isOnCurrentMonitor = true)
        => new(TaskbarState.Visible, edge, PixelRect.Empty, isOnCurrentMonitor);
}
