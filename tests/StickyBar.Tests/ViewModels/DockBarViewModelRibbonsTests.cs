using StickyBar.Core.Configuration;
using StickyBar.Core.Interaction;
using StickyBar.Core.Ribbons;
using StickyBar.Core.Services;
using StickyBar.Core.ViewModels;
using StickyBar.Tests.Docking;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.ViewModels;

/// <summary>
/// E4-CA2, E4-CA3, E4-CA4, E4-CA5, E4-CA6, Q-07, Q-10: ribbon CRUD, exclusive accordion deployment, confirmation on
/// non-empty deletion, flat structure and collapse-closes-ribbon semantics.
/// </summary>
public sealed class DockBarViewModelRibbonsTests
{
    [Fact]
    public void CreateRibbon_PersistsImmediately_AndRaisesLayoutChanged()
    {
        var fixture = CreateFixture();
        var layoutChanges = 0;
        fixture.ViewModel.LayoutChanged += (_, _) => layoutChanges++;

        var ribbon = fixture.ViewModel.CreateRibbon("Work");

        Assert.Single(fixture.ViewModel.Ribbons);
        Assert.Equal("Work", ribbon.Name);
        Assert.Equal(0, ribbon.Order);
        Assert.Equal(1, layoutChanges);
        Assert.Single(fixture.Document.Current.Ribbons);
        Assert.Equal("Work", fixture.Document.Current.Ribbons[0].Name);
    }

    [Fact]
    public void RenameRibbon_PersistsNewName_AndIgnoresBlank()
    {
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Alpha");

        Assert.False(fixture.ViewModel.RenameRibbon(ribbon.Id, "   "));
        Assert.Equal("Alpha", ribbon.Name);

        Assert.True(fixture.ViewModel.RenameRibbon(ribbon.Id, "Beta"));
        Assert.Equal("Beta", ribbon.Name);
        Assert.Equal("Beta", fixture.Document.Current.Ribbons[0].Name);
    }

    [Fact]
    public void MoveRibbon_ReordersAndPersistsOrder()
    {
        var fixture = CreateFixture();
        var first = fixture.ViewModel.CreateRibbon("A");
        var second = fixture.ViewModel.CreateRibbon("B");
        var third = fixture.ViewModel.CreateRibbon("C");

        Assert.True(fixture.ViewModel.MoveRibbonLater(first.Id));
        Assert.Equal(["B", "A", "C"], fixture.ViewModel.Ribbons.Select(r => r.Name).ToArray());
        Assert.Equal([0, 1, 2], fixture.ViewModel.Ribbons.Select(r => r.Order).ToArray());
        Assert.Equal(["B", "A", "C"], fixture.Document.Current.Ribbons.Select(r => r.Name).ToArray());

        Assert.True(fixture.ViewModel.MoveRibbonEarlier(third.Id));
        Assert.Equal(["B", "C", "A"], fixture.ViewModel.Ribbons.Select(r => r.Name).ToArray());
        Assert.Equal(second.Id, fixture.ViewModel.Ribbons[0].Id);
    }

    [Fact]
    public void DeleteRibbon_EmptyRibbon_RemovesWithoutConfirmation()
    {
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Empty");

        var deleted = fixture.ViewModel.DeleteRibbon(ribbon.Id);

        Assert.True(deleted);
        Assert.Empty(fixture.ViewModel.Ribbons);
        Assert.Empty(fixture.Document.Current.Ribbons);
        Assert.Empty(fixture.Confirmer.Requests);
    }

    [Fact]
    public void DeleteRibbon_WithItems_WhenConfirmed_RemovesRibbonAndPersists()
    {
        var initial = new StickyBarConfiguration
        {
            Ribbons =
            [
                new Ribbon
                {
                    Id = "r1",
                    Name = "Dev",
                    Order = 0,
                    Items =
                    [
                        new RibbonItem("i1", "Code", @"C:\Apps\code.lnk", 0),
                        new RibbonItem("i2", "Shell", @"C:\Apps\wt.lnk", 1),
                    ],
                },
            ],
        };

        var fixture = CreateFixture(initial, confirmResult: true);

        var deleted = fixture.ViewModel.DeleteRibbon("r1");

        Assert.True(deleted);
        Assert.Empty(fixture.ViewModel.Ribbons);
        Assert.Empty(fixture.Document.Current.Ribbons);
        var request = Assert.Single(fixture.Confirmer.Requests);
        Assert.Equal(("Dev", 2), request);
    }

    [Fact]
    public void DeleteRibbon_WithItems_WhenCancelled_LeavesRibbonIntact()
    {
        var initial = new StickyBarConfiguration
        {
            Ribbons =
            [
                new Ribbon
                {
                    Id = "r1",
                    Name = "Dev",
                    Order = 0,
                    Items = [new RibbonItem("i1", "Code", @"C:\Apps\code.lnk", 0)],
                },
            ],
        };

        var fixture = CreateFixture(initial, confirmResult: false);

        var deleted = fixture.ViewModel.DeleteRibbon("r1");

        Assert.False(deleted);
        Assert.Single(fixture.ViewModel.Ribbons);
        Assert.Single(fixture.Document.Current.Ribbons);
        Assert.Equal(1, fixture.Document.Current.Ribbons[0].ItemCount);
        Assert.Single(fixture.Confirmer.Requests);
    }

    [Fact]
    public void ToggleRibbon_OpensClosedRibbon_AndClosesOtherOpenRibbon_Accordion()
    {
        // E4-CA2 / HU03 CA #2: opening B automatically closes A.
        var fixture = CreateFixture();
        var ribbonA = fixture.ViewModel.CreateRibbon("A");
        var ribbonB = fixture.ViewModel.CreateRibbon("B");

        fixture.ViewModel.ToggleRibbon(ribbonA.Id);
        Assert.True(ribbonA.IsOpen);
        Assert.False(ribbonB.IsOpen);
        Assert.Same(ribbonA, fixture.ViewModel.OpenRibbon);
        Assert.True(fixture.ViewModel.HasOpenRibbon);

        fixture.ViewModel.ToggleRibbon(ribbonB.Id);
        Assert.False(ribbonA.IsOpen);
        Assert.True(ribbonB.IsOpen);
        Assert.Same(ribbonB, fixture.ViewModel.OpenRibbon);
    }

    [Fact]
    public void ToggleRibbon_ClickingOpenRibbon_ClosesIt()
    {
        var fixture = CreateFixture();
        var ribbonA = fixture.ViewModel.CreateRibbon("A");

        fixture.ViewModel.ToggleRibbon(ribbonA.Id);
        Assert.True(ribbonA.IsOpen);

        fixture.ViewModel.ToggleRibbon(ribbonA.Id);
        Assert.False(ribbonA.IsOpen);
        Assert.Null(fixture.ViewModel.OpenRibbon);
        Assert.False(fixture.ViewModel.HasOpenRibbon);
    }

    [Fact]
    public void Collapse_LeavingExpanded_ClosesOpenRibbon_AndRemainsClosedOnRedeploy()
    {
        // Q-10: leaving Expanded (via grace collapse or explicit collapse) closes any open ribbon.
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("A");

        fixture.States.SetState(DockState.Expanded);
        fixture.ViewModel.ToggleRibbon(ribbon.Id);
        Assert.True(ribbon.IsOpen);

        fixture.States.SetState(DockState.Collapsing);
        Assert.False(ribbon.IsOpen);
        Assert.Null(fixture.ViewModel.OpenRibbon);

        fixture.States.SetState(DockState.Tab);
        fixture.States.SetState(DockState.Expanded);
        Assert.False(ribbon.IsOpen);
        Assert.Null(fixture.ViewModel.OpenRibbon);
    }

    [Fact]
    public void Ribbons_Restart_RestoresRibbonsClosed()
    {
        // E4-CA4 + Q-10: persisted ribbons are restored on startup, all initially closed.
        var store = new RecordingConfigurationStore();
        var logger = new RecordingLogger();
        var document = new ConfigurationDocument(store, new StickyBarConfiguration(), logger);
        var states = new FakeDockStateService();
        var regions = new FakeDockRegionSink();

        var firstVm = new DockBarViewModel(logger, states, regions, null, document, new FakeConfirmer(true));
        var created = firstVm.CreateRibbon("Persisted");
        firstVm.ToggleRibbon(created.Id);
        Assert.True(created.IsOpen);

        var restartedDoc = new ConfigurationDocument(store, store.Saved.Last(), logger);
        var secondVm = new DockBarViewModel(logger, states, regions, null, restartedDoc, new FakeConfirmer(true));

        var restored = Assert.Single(secondVm.Ribbons);
        Assert.Equal("Persisted", restored.Name);
        Assert.False(restored.IsOpen);
        Assert.Null(secondVm.OpenRibbon);
    }

    [Fact]
    public void RibbonModel_IsFlat_CannotContainNestedRibbons()
    {
        // E4-CA3: structural check that Ribbon.Items only holds RibbonItem, never Ribbon.
        var itemsProperty = typeof(Ribbon).GetProperty(nameof(Ribbon.Items));
        Assert.NotNull(itemsProperty);
        Assert.Equal(typeof(IReadOnlyList<RibbonItem>), itemsProperty.PropertyType);
        Assert.Null(typeof(Ribbon).GetProperty("Ribbons"));
    }

    [Fact]
    public void RibbonDisplayNumber_TracksOneBasedOrderAndUpdatesOnReorder()
    {
        var fixture = CreateFixture();
        var first = fixture.ViewModel.CreateRibbon("A");
        var second = fixture.ViewModel.CreateRibbon("B");

        Assert.Equal(1, first.DisplayNumber);
        Assert.Equal(2, second.DisplayNumber);

        second.MoveEarlierCommand.Execute(null);

        Assert.Equal(1, second.DisplayNumber);
        Assert.Equal(2, first.DisplayNumber);
    }

    [Fact]
    public void RibbonToggleActionsCommand_TogglesInlineActionsAndResetsWhenClosed()
    {
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("A");

        ribbon.ToggleCommand.Execute(null);
        Assert.True(ribbon.IsOpen);
        Assert.False(ribbon.IsActionsOpen);

        ribbon.ToggleActionsCommand.Execute(null);
        Assert.True(ribbon.IsActionsOpen);

        ribbon.ToggleCommand.Execute(null);
        Assert.False(ribbon.IsOpen);
        Assert.False(ribbon.IsActionsOpen);
    }

    [Fact]
    public void OpenRibbonOffset_AlignsWithOpenRibbonIndex()
    {
        var fixture = CreateFixture();
        var first = fixture.ViewModel.CreateRibbon("A");
        var second = fixture.ViewModel.CreateRibbon("B");

        Assert.Equal(0d, fixture.ViewModel.OpenRibbonOffset);

        first.ToggleCommand.Execute(null);
        var firstOffset = fixture.ViewModel.OpenRibbonOffset;
        Assert.True(firstOffset > 0d);

        second.ToggleCommand.Execute(null);
        var secondOffset = fixture.ViewModel.OpenRibbonOffset;
        Assert.Equal(firstOffset + StickyBar.Core.Docking.DockBarMetrics.RibbonIndicatorExtent, secondOffset);
    }

    private static Fixture CreateFixture(StickyBarConfiguration? initial = null, bool confirmResult = true)
    {
        var store = new RecordingConfigurationStore();
        var logger = new RecordingLogger();
        var document = new ConfigurationDocument(store, initial ?? new StickyBarConfiguration(), logger);
        var states = new FakeDockStateService();
        var regions = new FakeDockRegionSink();
        var confirmer = new FakeConfirmer(confirmResult);
        var vm = new DockBarViewModel(logger, states, regions, null, document, confirmer);

        return new Fixture(vm, document, states, confirmer);
    }

    private sealed record Fixture(
        DockBarViewModel ViewModel,
        ConfigurationDocument Document,
        FakeDockStateService States,
        FakeConfirmer Confirmer);

    private sealed class FakeDockStateService : IDockStateService
    {
        public DockState State { get; private set; } = DockState.Tab;

        public bool IsPinned { get; set; }

        public bool IsContextMenuOpen { get; set; }

        public event EventHandler<DockStateChangedEventArgs>? StateChanged;

        public void SetState(DockState state)
        {
            State = state;
            StateChanged?.Invoke(this, new DockStateChangedEventArgs(state));
        }

        public void Expand() => SetState(DockState.Expanded);

        public void CollapseToTab() => SetState(DockState.Tab);

        public void BeginDrag() { }

        public void EndDrag() { }
    }

    private sealed class FakeDockRegionSink : IDockRegionSink
    {
        public void EnterRegion(DockRegion region) { }

        public void LeaveRegion(DockRegion region) { }
    }

    private sealed class FakeConfirmer(bool result) : IRibbonDeletionConfirmer
    {
        public List<(string Name, int ItemCount)> Requests { get; } = [];

        public bool ConfirmDeleteRibbon(string ribbonName, int itemCount)
        {
            Requests.Add((ribbonName, itemCount));
            return result;
        }
    }
}
