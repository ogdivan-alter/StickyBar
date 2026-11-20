using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Docking;

/// <summary>
/// E2-CA1 (snap and floating), E2-CA2 (reposition when the taskbar moves), E2-CA3 (resize inside the work area),
/// E2-CA4 (the taskbar edge is rejected), E2-CA5 (disposal releases the source) and E2-CA6 (the edge survives a
/// restart), verified on the coordinator with doubles and without opening a window (ADR-002).
/// </summary>
public sealed class DockPlacementCoordinatorTests
{
    private const int Dpi125 = 120;

    private static readonly PixelRect MonitorBounds = new(0, 0, 1920, 1080);
    private static readonly PixelRect WorkArea = new(0, 0, 1920, 1040);

    [Theory]
    [InlineData(DockEdge.Top, 885, 0, 150, 45)]
    [InlineData(DockEdge.Left, 0, 445, 45, 150)]
    [InlineData(DockEdge.Right, 1875, 445, 45, 150)]
    [InlineData(DockEdge.Bottom, 885, 995, 150, 45)]
    public void Start_WithAPersistedEdge_DocksToThatEdge(DockEdge edge, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
    {
        // E2-CA6: the persisted edge is applied again when the application starts.
        using var fixture = Create(edge, taskbarOnCurrentMonitor: false);
        var reported = new List<DockPlacementChangedEventArgs>();
        fixture.Coordinator.PlacementChanged += (_, args) => reported.Add(args);

        fixture.Coordinator.Start();

        var expected = new PixelRect(expectedX, expectedY, expectedWidth, expectedHeight);
        Assert.Equal(edge, fixture.Coordinator.CurrentEdge);
        Assert.Equal(expected, Assert.Single(fixture.Host.AppliedBounds));
        Assert.Equal(expected, fixture.Host.Bounds);
        Assert.Empty(fixture.Store.Saved);

        var placement = Assert.Single(reported);
        Assert.Equal(edge, placement.Edge);
        Assert.Equal(expected, placement.Bounds);
    }

    [Fact]
    public void Start_At125Percent_UsesDevicePixels()
    {
        using var fixture = Create(DockEdge.Top);

        fixture.Coordinator.Start();

        Assert.Equal(new PixelRect(885, 0, 150, 45), Assert.Single(fixture.Host.AppliedBounds));
    }

    [Fact]
    public void Start_WhenThePersistedEdgeIsTheTaskbarEdge_FallsBackToTheOppositeEdge()
    {
        // E2-CA2 at startup: the taskbar moved to the top while the bar was closed.
        using var fixture = Create(DockEdge.Top, DockEdge.Top);

        fixture.Coordinator.Start();

        Assert.Equal(DockEdge.Bottom, fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(885, 995, 150, 45), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Bottom, Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Fact]
    public void Start_WhenThePersistedEdgeIsNull_KeepsTheBarFloatingInsideTheWorkArea()
    {
        using var fixture = Create(persistedEdge: null);
        fixture.Host.Bounds = new PixelRect(-60, -40, 150, 45);

        fixture.Coordinator.Start();

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(0, 0, 150, 45), Assert.Single(fixture.Host.AppliedBounds));
    }

    [Fact]
    public void Start_WhenTheFloatingBarIsAlreadyOnScreen_DoesNotMoveTheWindow()
    {
        using var fixture = Create(persistedEdge: null);
        fixture.Host.Bounds = new PixelRect(700, 400, 150, 45);

        fixture.Coordinator.Start();

        Assert.Empty(fixture.Host.AppliedBounds);
        Assert.Null(fixture.Coordinator.CurrentEdge);
    }

    [Fact]
    public void Start_CalledTwice_Throws()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        Assert.Throws<InvalidOperationException>(fixture.Coordinator.Start);
    }

    [Fact]
    public void DragEnd_InsideTheBand_DocksAndPersistsTheEdge()
    {
        // E2-CA1: released 30 px from the top edge, inside the 48-unit band.
        using var fixture = Create(persistedEdge: null);
        fixture.Coordinator.Start();
        fixture.Host.Bounds = new PixelRect(600, 30, 150, 45);

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Equal(DockEdge.Top, fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(885, 0, 150, 45), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Top, Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Fact]
    public void DragEnd_OutsideTheBand_LeavesTheBarWhereTheUserDroppedIt_AndPersistsNull()
    {
        // E2-CA1: released far from every edge, the bar neither snaps nor moves.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        fixture.Host.Bounds = new PixelRect(600, 400, 150, 45);
        var appliedBeforeDrop = fixture.Host.AppliedBounds.Count;

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Equal(appliedBeforeDrop, fixture.Host.AppliedBounds.Count);
        Assert.Equal(new PixelRect(600, 400, 150, 45), fixture.Host.Bounds);
        Assert.Null(Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Fact]
    public void DragEnd_OutsideTheBand_WhenTheBarWasFloating_DoesNotWriteTheConfiguration()
    {
        using var fixture = Create(persistedEdge: null);
        fixture.Coordinator.Start();
        fixture.Host.Bounds = new PixelRect(600, 400, 150, 45);

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void DragEnd_WithoutMovement_DoesNotMoveTheWindow()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Single(fixture.Host.AppliedBounds);
    }

    [Fact]
    public void DragEnd_WithAnEmptyRect_DoesNotDock_AndPersistsNull()
    {
        // D-05: a window with no size is at distance zero from every edge, so the snap must not guess the top edge.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        var appliedBeforeDrop = fixture.Host.AppliedBounds.Count;

        fixture.Host.Bounds = PixelRect.Empty;

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Equal(appliedBeforeDrop, fixture.Host.AppliedBounds.Count);
        Assert.Null(Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Theory]
    [InlineData(DockingEventKind.WorkAreaChanged)]
    [InlineData(DockingEventKind.TaskbarChanged)]
    [InlineData(DockingEventKind.DisplayChanged)]
    public void EnvironmentChange_WhenTheTaskbarTakesTheDockedEdge_MigratesToTheOppositeEdge(DockingEventKind kind)
    {
        // E2-CA2: every source drives the same decision, so the bar never collides with the taskbar.
        using var fixture = Create(DockEdge.Left);
        fixture.Coordinator.Start();
        Assert.Equal(new PixelRect(0, 445, 45, 150), fixture.Host.Bounds);

        fixture.Geometry.Taskbar = TaskbarAt(DockEdge.Left);

        fixture.Events.Raise(kind);

        Assert.Equal(DockEdge.Right, fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(1875, 445, 45, 150), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Right, fixture.Store.LastSaved!.DockEdge);
    }

    [Fact]
    public void WorkAreaChange_WhenTheEdgeIsStillAvailable_KeepsItAndRecomputesTheRectangle()
    {
        // E2-CA3: a smaller work area keeps the bar docked, resized and inside the new area.
        using var fixture = Create(DockEdge.Right);
        fixture.Coordinator.Start();

        fixture.Geometry.Monitor = new MonitorGeometry(
            new PixelRect(0, 0, 1280, 720),
            new PixelRect(0, 0, 1280, 720),
            Dpi125);

        fixture.Events.Raise(DockingEventKind.WorkAreaChanged);

        Assert.Equal(DockEdge.Right, fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(1235, 285, 45, 150), fixture.Host.Bounds);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void DisplayChange_WhenTheWorkAreaShrinksBelowTheBar_ClipsTheBarIntoIt()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Geometry.Monitor = new MonitorGeometry(
            new PixelRect(0, 0, 100, 20),
            new PixelRect(0, 0, 100, 20),
            Dpi: 96);

        fixture.Events.Raise(DockingEventKind.DisplayChanged);

        Assert.Equal(new PixelRect(0, 0, 100, 20), fixture.Host.Bounds);
    }

    [Fact]
    public void DisplayChange_WithANewDpi_RecomputesTheRectangleInDevicePixels()
    {
        // R-05: after moving to a 100 % monitor the bar is measured in the pixels of the new monitor.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        Assert.Equal(new PixelRect(885, 0, 150, 45), fixture.Host.Bounds);

        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, WorkArea, Dpi: 96);

        fixture.Events.Raise(DockingEventKind.DisplayChanged);

        Assert.Equal(new PixelRect(900, 0, 120, 36), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Top, fixture.Coordinator.CurrentEdge);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void EnvironmentChange_WithoutARealChange_DoesNotMoveTheWindow()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Events.Raise(DockingEventKind.WorkAreaChanged);
        fixture.Events.Raise(DockingEventKind.DisplayChanged);

        Assert.Single(fixture.Host.AppliedBounds);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void DockTo_WhenTheTaskbarOccupiesTheEdge_IsIgnoredAndLogged()
    {
        // E2-CA4: the third of the three defences, the explicit command.
        using var fixture = Create(DockEdge.Top, DockEdge.Left);
        fixture.Coordinator.Start();
        var appliedBeforeRequest = fixture.Host.AppliedBounds.Count;

        fixture.Coordinator.DockTo(DockEdge.Left);

        Assert.Equal(appliedBeforeRequest, fixture.Host.AppliedBounds.Count);
        Assert.Equal(DockEdge.Top, fixture.Coordinator.CurrentEdge);
        Assert.Empty(fixture.Store.Saved);
        Assert.Contains(
            fixture.Logger.Entries,
            entry => entry.StartsWith("Warning", StringComparison.Ordinal)
                && entry.Contains(nameof(DockEdge.Left), StringComparison.Ordinal));
    }

    [Fact]
    public void DockTo_PersistsTheNewEdge()
    {
        using var fixture = Create(persistedEdge: null);
        fixture.Coordinator.Start();

        fixture.Coordinator.DockTo(DockEdge.Right);

        Assert.Equal(DockEdge.Right, fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(1875, 445, 45, 150), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Right, Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Fact]
    public void DockTo_TheSameEdgeTwice_MovesTheWindowOnce()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Coordinator.DockTo(DockEdge.Top);

        Assert.Single(fixture.Host.AppliedBounds);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void Undock_LeavesTheBarWhereItIs_AndPersistsNull()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        var appliedBeforeUndock = fixture.Host.AppliedBounds.Count;
        var boundsBeforeUndock = fixture.Host.Bounds;

        fixture.Coordinator.Undock();

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Equal(appliedBeforeUndock, fixture.Host.AppliedBounds.Count);
        Assert.Equal(boundsBeforeUndock, fixture.Host.Bounds);
        Assert.Null(Assert.Single(fixture.Store.Saved).DockEdge);
    }

    [Fact]
    public void Undock_WhenTheBarWasAlreadyFloating_DoesNotWriteTheConfiguration()
    {
        using var fixture = Create(persistedEdge: null);
        fixture.Coordinator.Start();

        fixture.Coordinator.Undock();

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void BeginDrag_ForwardsTheRequestToTheHost()
    {
        using var fixture = Create(DockEdge.Top);

        fixture.Coordinator.BeginDrag();

        Assert.Equal(1, fixture.Host.BeginDragCount);
    }

    [Fact]
    public void BeginDrag_RaisesDragStartedBeforeTheNativeMoveLoopBlocks()
    {
        // The interaction controller suppresses its automatic transitions from DragStarted on (ADR-003, D3).
        using var fixture = Create(DockEdge.Top);
        var started = 0;
        fixture.Coordinator.DragStarted += (_, _) => started++;

        fixture.Coordinator.BeginDrag();

        Assert.Equal(1, started);
    }

    [Fact]
    public void DragEnd_RaisesDragCompleted()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        var completed = 0;
        fixture.Coordinator.DragCompleted += (_, _) => completed++;

        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);

        Assert.Equal(1, completed);
    }

    [Fact]
    public void NotifyBarSizeChanged_GrowsTheDockedBarInward()
    {
        // D-09: the deployed footprint is applied without waiting for an environment event.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        Assert.Equal(new PixelRect(885, 0, 150, 45), fixture.Host.Bounds);

        fixture.BarSize.Expanded = true;

        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Equal(new PixelRect(855, 0, 210, 120), fixture.Host.Bounds);
        Assert.Equal(DockEdge.Top, fixture.Coordinator.CurrentEdge);
        Assert.Empty(fixture.Store.Saved);
    }

    [Fact]
    public void NotifyBarSizeChanged_OnAVerticalEdge_SwapsTheFootprint()
    {
        using var fixture = Create(DockEdge.Left);
        fixture.Coordinator.Start();
        Assert.Equal(new PixelRect(0, 445, 45, 150), fixture.Host.Bounds);

        fixture.BarSize.Expanded = true;

        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Equal(new PixelRect(0, 415, 120, 210), fixture.Host.Bounds);
    }

    [Fact]
    public void NotifyBarSizeChanged_WithTheSameSize_DoesNotMoveTheWindow()
    {
        // Rule R4: recalculating with the same footprint is a no-op.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Single(fixture.Host.AppliedBounds);
    }

    [Fact]
    public void NotifyBarSizeChanged_OfAFloatingBar_KeepsTopLeftFixed()
    {
        // Fixed top-left anchor: a floating bar keeps X and Y fixed when resized.
        using var fixture = Create(persistedEdge: null);
        fixture.Host.Bounds = new PixelRect(700, 400, 150, 45);
        fixture.Coordinator.Start();

        fixture.BarSize.Expanded = true;

        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Null(fixture.Coordinator.CurrentEdge);
        Assert.Equal(new PixelRect(700, 400, 210, 120), fixture.Host.Bounds);
    }

    [Fact]
    public void NotifyBarSizeChanged_AppliesOncePerTransition()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();
        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Equal(2, fixture.Host.AppliedBounds.Count);
    }

    [Fact]
    public void Apply_WhenTheHostReenters_AppliesOnceAndKeepsLastApplied()
    {
        // Rule R2 (ADR-003, §6.6): the rectangle is recorded as applied before the window is moved, so a message that
        // arrives while the move is running already sees it and does not move the window a second time.
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        var appliedBeforeGrowth = fixture.Host.AppliedBounds.Count;

        fixture.BarSize.Expanded = true;

        var reentered = false;
        fixture.Host.OnApplyBounds = _ =>
        {
            if (reentered)
            {
                return;
            }

            reentered = true;
            fixture.Coordinator.NotifyBarSizeChanged();
        };

        fixture.Coordinator.NotifyBarSizeChanged();

        var applied = fixture.Host.AppliedBounds;
        Assert.True(reentered);
        Assert.Equal(appliedBeforeGrowth + 1, applied.Count);
        Assert.Equal(new PixelRect(855, 0, 210, 120), applied[^1]);
        Assert.Equal(new PixelRect(855, 0, 210, 120), fixture.Host.Bounds);
    }

    [Fact]
    public void NotifyBarSizeChanged_BeforeStart_IsIgnored()
    {
        using var fixture = Create(DockEdge.Top);

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Empty(fixture.Host.AppliedBounds);
    }

    [Fact]
    public void Dispose_UnsubscribesFromTheEventSourceAndDisposesIt()
    {
        // E2-CA5: the coordinator owns the source, so closing it releases the native hook.
        var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        Assert.Equal(1, fixture.Events.SubscriberCount);

        fixture.Coordinator.Dispose();

        Assert.Equal(0, fixture.Events.SubscriberCount);
        Assert.True(fixture.Events.IsDisposed);
        Assert.Equal(1, fixture.Events.DisposeCount);
    }

    [Fact]
    public void Dispose_LeavesTheHostAndTheGeometryUsable()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        var appliedBeforeDispose = fixture.Host.AppliedBounds.Count;

        fixture.Coordinator.Dispose();

        // The application owns the window and the native reader; the coordinator only owns the event source.
        fixture.Host.Bounds = new PixelRect(1, 2, 3, 4);
        Assert.Equal(new PixelRect(1, 2, 3, 4), fixture.Host.GetBounds());
        Assert.Equal(appliedBeforeDispose, fixture.Host.AppliedBounds.Count);
        Assert.Equal(Dpi125, fixture.Geometry.Monitor.Dpi);
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();

        fixture.Coordinator.Dispose();
        fixture.Coordinator.Dispose();

        Assert.Equal(1, fixture.Events.DisposeCount);
    }

    [Fact]
    public void Members_AfterDispose_Throw()
    {
        using var fixture = Create(DockEdge.Top);
        fixture.Coordinator.Start();
        fixture.Coordinator.Dispose();

        Assert.Throws<ObjectDisposedException>(fixture.Coordinator.Start);
        Assert.Throws<ObjectDisposedException>(() => fixture.Coordinator.DockTo(DockEdge.Left));
        Assert.Throws<ObjectDisposedException>(fixture.Coordinator.Undock);
        Assert.Throws<ObjectDisposedException>(fixture.Coordinator.BeginDrag);
    }

    [Fact]
    public void FloatingCycle_DeployAndCollapse_ReturnsToTheSameRestingRect()
    {
        // Deploying from (104,104,120,36) keeps (104,104) fixed and collapsing returns to the exact resting rect.
        using var fixture = Create(persistedEdge: DockEdge.Top);
        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        fixture.Coordinator.Start();

        fixture.Host.Bounds = new PixelRect(104, 104, 120, 36);
        fixture.Events.Raise(DockingEventKind.WindowDragCompleted);
        Assert.Null(fixture.Coordinator.CurrentEdge);

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(new PixelRect(104, 104, 168, 96), fixture.Host.Bounds);

        fixture.BarSize.Expanded = false;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(new PixelRect(104, 104, 120, 36), fixture.Host.Bounds);
    }

    [Fact]
    public void NotifyBarSizeChanged_OfAFloatingBar_KeepsTheAnchoredSideFixed()
    {
        // Resizing a floating bar keeps its top-left (X, Y) fixed even when dimensions grow repeatedly.
        using var fixture = Create(persistedEdge: null);
        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        fixture.Host.Bounds = new PixelRect(104, 104, 120, 36);
        fixture.Coordinator.Start();

        fixture.BarSize.Metrics = new DockBarMetrics(120d, 36d, 260d, 140d);
        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Equal(104, fixture.Host.Bounds.X);
        Assert.Equal(104, fixture.Host.Bounds.Y);
        Assert.Equal(260, fixture.Host.Bounds.Width);
        Assert.Equal(140, fixture.Host.Bounds.Height);

        fixture.BarSize.Metrics = new DockBarMetrics(120d, 36d, 340d, 240d);
        fixture.Coordinator.NotifyBarSizeChanged();

        Assert.Equal(104, fixture.Host.Bounds.X);
        Assert.Equal(104, fixture.Host.Bounds.Y);
        Assert.Equal(340, fixture.Host.Bounds.Width);
        Assert.Equal(240, fixture.Host.Bounds.Height);
    }

    [Fact]
    public void Start_WithAPersistedFloatingEdge_DerivesTheAnchorOnce_AndKeepsItAcrossTransitions()
    {
        // Floating placement keeps (104,104) fixed across expand and collapse transitions.
        using var fixture = Create(persistedEdge: null);
        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        fixture.Host.Bounds = new PixelRect(104, 104, 120, 36);

        fixture.Coordinator.Start();
        Assert.Equal(new PixelRect(104, 104, 120, 36), fixture.Host.Bounds);

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(new PixelRect(104, 104, 168, 96), fixture.Host.Bounds);

        fixture.BarSize.Expanded = false;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(new PixelRect(104, 104, 120, 36), fixture.Host.Bounds);
    }

    [Fact]
    public void DockTo_ClearsTheAnchor_AndUndockSetsItAgain()
    {
        // Docking clears the floating anchor; undocking leaves the bar floating with fixed top-left anchor.
        using var fixture = Create(persistedEdge: null);
        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        fixture.Host.Bounds = new PixelRect(104, 104, 120, 36);
        fixture.Coordinator.Start();

        fixture.Coordinator.DockTo(DockEdge.Left);
        Assert.Equal(DockEdge.Left, fixture.Coordinator.CurrentEdge);

        fixture.Coordinator.Undock();
        Assert.Null(fixture.Coordinator.CurrentEdge);
        fixture.Coordinator.NotifyBarSizeChanged();
        var undockedRect = fixture.Host.Bounds;

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(undockedRect.X, fixture.Host.Bounds.X);
        Assert.Equal(undockedRect.Y, fixture.Host.Bounds.Y);

        fixture.BarSize.Expanded = false;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.Equal(undockedRect, fixture.Host.Bounds);
    }

    [Fact]
    public void WorkAreaChange_ThatForcesTheClamp_ReDerivesTheAnchor_AndLeavesTheBarInsideTheArea()
    {
        // ADR-005 D6: when a work area shrink clamps the floating bar, the anchor is re-derived from the clamped rect.
        using var fixture = Create(persistedEdge: null);
        fixture.Geometry.Monitor = new MonitorGeometry(MonitorBounds, MonitorBounds, 96);
        fixture.Host.Bounds = new PixelRect(1750, 400, 120, 36);
        fixture.Coordinator.Start();

        var smallerWorkArea = new PixelRect(0, 0, 1280, 720);
        fixture.Geometry.Monitor = new MonitorGeometry(smallerWorkArea, smallerWorkArea, 96);
        fixture.Events.Raise(DockingEventKind.WorkAreaChanged);

        Assert.True(fixture.Host.Bounds.X >= smallerWorkArea.X && fixture.Host.Bounds.Right <= smallerWorkArea.Right);
        Assert.True(fixture.Host.Bounds.Y >= smallerWorkArea.Y && fixture.Host.Bounds.Bottom <= smallerWorkArea.Bottom);
        Assert.Equal(1280, fixture.Host.Bounds.Right);

        fixture.BarSize.Expanded = true;
        fixture.Coordinator.NotifyBarSizeChanged();
        Assert.True(fixture.Host.Bounds.X >= smallerWorkArea.X && fixture.Host.Bounds.Right <= smallerWorkArea.Right);
        Assert.True(fixture.Host.Bounds.Y >= smallerWorkArea.Y && fixture.Host.Bounds.Bottom <= smallerWorkArea.Bottom);
        Assert.Equal(1280, fixture.Host.Bounds.Right);
    }

    private static TaskbarPlacement TaskbarAt(DockEdge edge, bool isOnCurrentMonitor = true)
        => new(TaskbarState.Visible, edge, PixelRect.Empty, isOnCurrentMonitor);

    private static Fixture Create(
        DockEdge? persistedEdge,
        DockEdge? taskbarEdge = DockEdge.Bottom,
        bool taskbarOnCurrentMonitor = true)
    {
        var events = new FakeDockingEventSource();
        var geometry = new FakeWindowGeometrySource
        {
            Monitor = new MonitorGeometry(MonitorBounds, WorkArea, Dpi125),
            Taskbar = TaskbarAt(taskbarEdge ?? DockEdge.Bottom, taskbarOnCurrentMonitor),
        };

        var host = new FakeDockWindowHost();
        var barSize = new FakeBarSizeSource();
        var store = new RecordingConfigurationStore();
        var logger = new RecordingLogger();
        var document = new ConfigurationDocument(store, new StickyDockConfiguration { DockEdge = persistedEdge }, logger);

        var coordinator = new DockPlacementCoordinator(events, geometry, host, barSize, document, logger);

        return new Fixture(coordinator, events, geometry, host, barSize, store, logger);
    }

    /// <summary>Wiring of one coordinator with its doubles, disposed together with the coordinator.</summary>
    private sealed class Fixture : IDisposable
    {
        public Fixture(
            DockPlacementCoordinator coordinator,
            FakeDockingEventSource events,
            FakeWindowGeometrySource geometry,
            FakeDockWindowHost host,
            FakeBarSizeSource barSize,
            RecordingConfigurationStore store,
            RecordingLogger logger)
        {
            Coordinator = coordinator;
            Events = events;
            Geometry = geometry;
            Host = host;
            BarSize = barSize;
            Store = store;
            Logger = logger;
        }

        public DockPlacementCoordinator Coordinator { get; }

        public FakeDockingEventSource Events { get; }

        public FakeWindowGeometrySource Geometry { get; }

        public FakeDockWindowHost Host { get; }

        public FakeBarSizeSource BarSize { get; }

        public RecordingConfigurationStore Store { get; }

        public RecordingLogger Logger { get; }

        public void Dispose() => Coordinator.Dispose();
    }
}
