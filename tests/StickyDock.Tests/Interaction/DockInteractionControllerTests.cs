using StickyDock.Core.Configuration;
using StickyDock.Core.Interaction;
using StickyDock.Core.Services;
using StickyDock.Tests.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Interaction;

/// <summary>
/// E3-CA1 (hover of ~200 ms deploys the bar), E3-CA2 (collapse after the grace period), E3-CA3 (moving between the bar and
/// its ribbons does not collapse it), E3-CA4 (the activation is cancelled before 200 ms) and E3-CA5 (the grace is read
/// from the configuration when the timer is armed), asserted on the state machine with a manual clock: no test waits for
/// real time (ADR-003, D4, §7 and §9).
/// </summary>
public sealed class DockInteractionControllerTests
{
    [Fact]
    public void TheBarStartsCollapsedAndWithoutTimers()
    {
        // §4.2: the bar always appears at rest, because the state is never persisted.
        using var fixture = new Fixture();

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
        Assert.Empty(fixture.States);
    }

    [Fact]
    public void EnterTab_ArmsActivation_AndEntersArming()
    {
        // E3-CA1: Arming is the state that emits the micro-expansion, so it must be entered before deploying.
        using var fixture = new Fixture();

        fixture.Controller.EnterRegion(DockRegion.Tab);

        Assert.Equal(DockState.Arming, fixture.Controller.State);
        Assert.Equal(
            TimeSpan.FromMilliseconds(InteractionTimings.ActivationMilliseconds),
            fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void ActivationFires_ExactlyAt200ms_NotBefore()
    {
        // E3-CA1: the activation is a 200 ms timer fixed by RF02, not a property of the request.
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(InteractionTimings.ActivationMilliseconds - 1);

        Assert.Equal(DockState.Arming, fixture.Controller.State);

        fixture.Scheduler.Advance(1);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void ActivationFires_DeploysTheBar()
    {
        using var fixture = new Fixture();

        Expand(fixture);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
        Assert.Equal([DockState.Arming, DockState.Expanded], fixture.States);
    }

    [Fact]
    public void EnterTab_Twice_DoesNotRestartTheActivation()
    {
        // A duplicated entry cannot extend the hover: the region set has no duplicates.
        using var fixture = new Fixture();

        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Controller.EnterRegion(DockRegion.Tab);

        Assert.Equal(1, fixture.Scheduler.ScheduleCount);
        Assert.Single(fixture.Controller.ActiveRegions);
    }

    [Fact]
    public void LeaveRegion_SchedulesTheGraceWithoutChangingState()
    {
        // E3-CA2: the grace is not a state, it is a pending timer inside Expanded, so the bar stays deployed.
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.Equal(TimeSpan.FromMilliseconds(GracePeriod.DefaultMilliseconds), fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void GraceTimerFires_EntersCollapsing()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);

        Assert.Equal(DockState.Collapsing, fixture.Controller.State);
        Assert.Equal(
            TimeSpan.FromMilliseconds(InteractionTimings.SlideMilliseconds),
            fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void SettleTimerFires_CollapsesToTab()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);
        fixture.Scheduler.Advance(InteractionTimings.SlideMilliseconds);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
        Assert.Equal(
            [DockState.Arming, DockState.Expanded, DockState.Collapsing, DockState.Tab],
            fixture.States);
    }

    [Fact]
    public void Grace_UsesTheValueOfTheDocument()
    {
        // E3-CA5: the value is read when the timer is armed, so editing settings.json takes effect without restarting.
        using var fixture = new Fixture(graceMilliseconds: 500);
        Expand(fixture);

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(TimeSpan.FromMilliseconds(500), fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void Grace_ChangesTakeEffectOnTheNextTimer()
    {
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Document.Update(document => document with { GracePeriodMilliseconds = 300 });
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(TimeSpan.FromMilliseconds(300), fixture.Scheduler.PendingDelay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(299)]
    [InlineData(9000)]
    public void Grace_IsNormalizedWhenTheStoredValueIsOutOfRange(int stored)
    {
        // Q-03: the range is 300..500 ms. A hand-edited file cannot leave the timer in an unusable value.
        using var fixture = new Fixture(graceMilliseconds: stored);
        Expand(fixture);

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(
            TimeSpan.FromMilliseconds(GracePeriod.Normalize(stored)),
            fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void EnteringBar_CancelsTheGrace()
    {
        // E3-CA3: the interactive area is the union of the regions (A4), so moving into the bar keeps it open.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Controller.EnterRegion(DockRegion.Bar);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void EnteringTheBarRepeatedly_IsIdempotent_AndKeepsTheGraceCancelled()
    {
        // D-E3-01: the reporter reaffirms the region on every pointer move and after every geometry change, so the same
        // entry arrives many times. The region set must not grow and the single slot must stay empty.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        var schedulesBefore = fixture.Scheduler.ScheduleCount;
        var cancelsBefore = fixture.Scheduler.CancelCount;

        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Controller.EnterRegion(DockRegion.Bar);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.Equal(DockRegion.Bar, Assert.Single(fixture.Controller.ActiveRegions));
        Assert.False(fixture.Scheduler.IsPending);
        Assert.Equal(schedulesBefore, fixture.Scheduler.ScheduleCount);
        Assert.Equal(cancelsBefore + 1, fixture.Scheduler.CancelCount);
        Assert.Equal([DockState.Arming, DockState.Expanded], fixture.States);
    }

    [Fact]
    public void LeavingAndReenteringTheBarWithinTheGrace_KeepsTheBarDeployed()
    {
        // D-E3-01: the pointer that leaves the panel and comes back —or a report that arrives after the exit— is absorbed
        // by the grace period, and the cancellation must leave no timer behind.
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds - 1);

        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.DoesNotContain(DockState.Collapsing, fixture.States);
        Assert.DoesNotContain(DockState.Tab, fixture.States);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void ReconciledReportsOfTheSameSide_DoNotArmOrCancelTheSlot()
    {
        // D-E3-01: the reconciliation asks the real pointer position after every geometry change and reports only the
        // transition the tracker sees. A run of identical observations must leave the slot exactly as it was.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Controller.EnterRegion(DockRegion.Bar);

        var schedulesBefore = fixture.Scheduler.ScheduleCount;
        var cancelsBefore = fixture.Scheduler.CancelCount;

        for (var observation = 0; observation < 5; observation++)
        {
            fixture.Controller.EnterRegion(DockRegion.Bar);
        }

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
        Assert.Equal(schedulesBefore, fixture.Scheduler.ScheduleCount);
        Assert.Equal(cancelsBefore, fixture.Scheduler.CancelCount);
    }

    [Fact]
    public void EnteringRibbon_CancelsTheGrace()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Controller.EnterRegion(DockRegion.Ribbon);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void EnteringTheRibbonWithinTheGrace_KeepsTheBarDeployed()
    {
        // E3-CA3: the transit between the bar and a ribbon is absorbed by the grace period.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds - 1);
        fixture.Controller.EnterRegion(DockRegion.Ribbon);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void TabBarRibbonSequence_NeverCollapses()
    {
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);

        fixture.Controller.EnterRegion(DockRegion.Ribbon);
        fixture.Controller.LeaveRegion(DockRegion.Bar);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void LeavingEveryRegion_CollapsesAfterTheGrace()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Controller.LeaveRegion(DockRegion.Bar);
        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);
        fixture.Scheduler.Advance(InteractionTimings.SlideMilliseconds);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
    }

    [Fact]
    public void LeaveBefore200ms_CancelsActivation()
    {
        // E3-CA4: leaving the tab before the activation expires disarms the timer and returns to rest.
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(100);

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(199)]
    public void LeaveAt199ms_DoesNotExpand(int elapsed)
    {
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(elapsed);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.Equal([DockState.Arming, DockState.Tab], fixture.States);
    }

    [Fact]
    public void LeaveTab_WhenAnotherRegionIsStillActive_DeploysDirectly()
    {
        // The pointer is already inside the interactive area, so there is nothing to hover over: no dead 200 ms wait.
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Bar);
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void EnteringACollapsingBar_ReExpandsIt()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);
        Assert.Equal(DockState.Collapsing, fixture.Controller.State);

        fixture.Controller.EnterRegion(DockRegion.Bar);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void EnteringTheTabAfterTheCollapse_StartsANewActivation()
    {
        // The pointer may be back on the tab when the settle timer fires: a fresh activation covers it.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Scheduler.Advance(InteractionTimings.SlideMilliseconds);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void BeginDrag_FromArming_CancelsTheActivationAndSettlesToTab()
    {
        // D3: dragging suppresses the automatic transitions, and neither Arming nor Collapsing has a stable footprint.
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Controller.BeginDrag();

        Assert.True(fixture.Controller.IsDragging);
        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void BeginDrag_WhileExpanded_KeepsTheState()
    {
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.BeginDrag();

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void BeginDrag_FromCollapsing_SettlesToTab()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(GracePeriod.DefaultMilliseconds);

        fixture.Controller.BeginDrag();

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void Drag_SuppressesExpansion_EvenAfter200ms()
    {
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Controller.BeginDrag();

        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
    }

    [Fact]
    public void EnteringTheTabWhileDragging_DoesNotArmTheActivation()
    {
        using var fixture = new Fixture();
        fixture.Controller.BeginDrag();

        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void EndDrag_ReevaluatesWithCurrentRegions()
    {
        using var fixture = new Fixture();
        fixture.Controller.BeginDrag();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Controller.EndDrag();

        Assert.False(fixture.Controller.IsDragging);
        Assert.Equal(DockState.Arming, fixture.Controller.State);
        Assert.Equal(
            TimeSpan.FromMilliseconds(InteractionTimings.ActivationMilliseconds),
            fixture.Scheduler.PendingDelay);
    }

    [Fact]
    public void EndDrag_WhenThePointerIsGone_StaysCollapsed()
    {
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Controller.BeginDrag();
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Controller.EndDrag();

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void Drag_WhileExpanded_ThenEndDragWithThePointerInside_StaysExpanded()
    {
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.EnterRegion(DockRegion.Bar);

        fixture.Controller.BeginDrag();
        fixture.Controller.EndDrag();
        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
    }

    [Fact]
    public void ExplicitExpand_CancelsTheGrace()
    {
        // E6 needs the explicit override for the tray menu: it must not collapse a moment later.
        using var fixture = new Fixture();
        Expand(fixture);
        fixture.Controller.LeaveRegion(DockRegion.Tab);

        fixture.Controller.Expand();

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void Expand_WhenAlreadyExpanded_DoesNotRaiseTheEvent()
    {
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.Expand();

        Assert.Equal([DockState.Arming, DockState.Expanded], fixture.States);
    }

    [Fact]
    public void CollapseToTab_FromArming_CancelsTheActivation()
    {
        using var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Controller.CollapseToTab();

        Assert.Equal(DockState.Tab, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);

        fixture.Scheduler.Advance(5_000);

        Assert.Equal(DockState.Tab, fixture.Controller.State);
    }

    [Fact]
    public void ActiveRegions_TracksEntriesAndExits()
    {
        using var fixture = new Fixture();

        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Controller.EnterRegion(DockRegion.Bar);

        Assert.Equal([DockRegion.Tab, DockRegion.Bar], fixture.Controller.ActiveRegions.Order());

        fixture.Controller.LeaveRegion(DockRegion.Tab);

        Assert.Equal([DockRegion.Bar], fixture.Controller.ActiveRegions);
    }

    [Fact]
    public void EnterRegion_WithNone_IsIgnored()
    {
        using var fixture = new Fixture();

        fixture.Controller.EnterRegion(DockRegion.None);

        Assert.Empty(fixture.Controller.ActiveRegions);
        Assert.Equal(DockState.Tab, fixture.Controller.State);
    }

    [Fact]
    public void LeaveRegion_WhenTheRegionWasNotActive_IsIgnored()
    {
        using var fixture = new Fixture();
        Expand(fixture);

        fixture.Controller.LeaveRegion(DockRegion.Ribbon);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);
        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void Dispose_CancelsThePendingTimer()
    {
        var fixture = new Fixture();
        fixture.Controller.EnterRegion(DockRegion.Tab);

        fixture.Controller.Dispose();

        Assert.False(fixture.Scheduler.IsPending);

        fixture.Dispose();
    }

    [Fact]
    public void Dispose_Twice_IsIdempotent()
    {
        // The composition root registers the controller under two contracts and disposes it once per registration.
        using var fixture = new Fixture();

        fixture.Controller.Dispose();
        fixture.Controller.Dispose();

        Assert.False(fixture.Scheduler.IsPending);
    }

    [Fact]
    public void Dispose_ClearsTheSubscribers()
    {
        var fixture = new Fixture();

        fixture.Controller.Dispose();
        fixture.Scheduler.Advance(5_000);

        Assert.Empty(fixture.States);

        fixture.Dispose();
    }

    [Fact]
    public void MembersAfterDispose_Throw()
    {
        using var fixture = new Fixture();
        fixture.Controller.Dispose();

        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.EnterRegion(DockRegion.Tab));
        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.LeaveRegion(DockRegion.Tab));
        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.BeginDrag());
        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.EndDrag());
        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.Expand());
        Assert.Throws<ObjectDisposedException>(() => fixture.Controller.CollapseToTab());
    }

    [Fact]
    public void ImplementsTheStateServiceAndTheRegionSink()
    {
        // D7: the state is consulted through one contract and the pointer input notified through another.
        using var fixture = new Fixture();

        Assert.IsAssignableFrom<IDockStateService>(fixture.Controller);
        Assert.IsAssignableFrom<IDockRegionSink>(fixture.Controller);
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_Throws()
    {
        var logger = new RecordingLogger();

        using var document = new ConfigurationDocument(
            new RecordingConfigurationStore(),
            new StickyDockConfiguration(),
            logger);

        Assert.Throws<ArgumentNullException>(() => new DockInteractionController(null!, document, logger));
        Assert.Throws<ArgumentNullException>(() => new DockInteractionController(new ManualScheduler(), null!, logger));
        Assert.Throws<ArgumentNullException>(() => new DockInteractionController(new ManualScheduler(), document, null!));
    }

    /// <summary>Reaches <see cref="DockState.Expanded"/> through the real flow: entry on the tab and the activation timer.</summary>
    /// <param name="fixture">Machine under test.</param>
    private static void Expand(Fixture fixture)
    {
        fixture.Controller.EnterRegion(DockRegion.Tab);
        fixture.Scheduler.Advance(InteractionTimings.ActivationMilliseconds);
    }

    /// <summary>
    /// Machine under test with a manual clock, a real configuration document over an in-memory store and the sequence of
    /// states it reports.
    /// </summary>
    private sealed class Fixture : IDisposable
    {
        private readonly RecordingLogger _logger = new();

        public Fixture(int graceMilliseconds = GracePeriod.DefaultMilliseconds)
        {
            Document = new ConfigurationDocument(
                Store,
                new StickyDockConfiguration { GracePeriodMilliseconds = graceMilliseconds },
                _logger);

            Controller = new DockInteractionController(Scheduler, Document, _logger);
            Controller.StateChanged += (_, arguments) => States.Add(arguments.State);
        }

        public ManualScheduler Scheduler { get; } = new();

        public RecordingConfigurationStore Store { get; } = new();

        public ConfigurationDocument Document { get; }

        public DockInteractionController Controller { get; }

        public List<DockState> States { get; } = [];

        public void Dispose()
        {
            Controller.Dispose();
            Document.Dispose();
        }
    }
}
