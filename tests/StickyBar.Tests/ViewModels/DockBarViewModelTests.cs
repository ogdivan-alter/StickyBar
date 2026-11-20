using StickyBar.Core.Docking;
using StickyBar.Core.Interaction;
using StickyBar.Core.Services;
using StickyBar.Core.ViewModels;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.ViewModels;

/// <summary>
/// ADR-003, D4, D5 and D7: the view model projects the dock state that drives the animations and forwards the pointer
/// entries and exits to the region sink. It takes no animation, docking or timer decision of its own (§5.2-01).
/// </summary>
public sealed class DockBarViewModelTests
{
    [Fact]
    public void Constructor_StartsAtTheStateOfTheService()
    {
        var states = new FakeDockStateService();

        var viewModel = CreateViewModel(states: states);

        Assert.Equal(DockState.Tab, viewModel.DockState);
    }

    [Fact]
    public void Constructor_LogsTheCreationOfTheBar()
    {
        var logger = new RecordingLogger();

        _ = CreateViewModel(logger: logger);

        Assert.Single(logger.Entries);
    }

    [Theory]
    [InlineData(DockState.Arming)]
    [InlineData(DockState.Expanded)]
    [InlineData(DockState.Collapsing)]
    [InlineData(DockState.Tab)]
    public void StateChange_IsProjectedToTheView(DockState state)
    {
        // The view triggers every storyboard from DockState, so the projection is the contract of E3-CA1 and E3-CA2.
        var states = new FakeDockStateService();
        var viewModel = CreateViewModel(states: states);

        states.SetState(state);

        Assert.Equal(state, viewModel.DockState);
    }

    [Fact]
    public void StateChange_RaisesPropertyChangedForDockState()
    {
        var states = new FakeDockStateService();
        var viewModel = CreateViewModel(states: states);
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raised.Add(arguments.PropertyName);

        states.SetState(DockState.Arming);

        Assert.Equal(new[] { nameof(DockBarViewModel.DockState) }, raised);
    }

    [Fact]
    public void EnterRegionCommand_ForwardsTheRegionToTheSink()
    {
        var regions = new FakeDockRegionSink();
        var viewModel = CreateViewModel(regions: regions);

        viewModel.EnterRegionCommand.Execute(DockRegion.Tab);

        Assert.Equal(new[] { DockRegion.Tab }, regions.Entered);
    }

    [Fact]
    public void LeaveRegionCommand_ForwardsTheRegionToTheSink()
    {
        var regions = new FakeDockRegionSink();
        var viewModel = CreateViewModel(regions: regions);

        viewModel.LeaveRegionCommand.Execute(DockRegion.Bar);

        Assert.Equal(new[] { DockRegion.Bar }, regions.Left);
    }

    [Theory]
    [InlineData(DockRegion.Tab)]
    [InlineData(DockRegion.Bar)]
    [InlineData(DockRegion.Ribbon)]
    public void RegionCommands_AreAvailableForEveryRegion(DockRegion region)
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.EnterRegionCommand.CanExecute(region));
        Assert.True(viewModel.LeaveRegionCommand.CanExecute(region));
    }

    [Fact]
    public void EnterRegionCommand_WithAParameterOfTheWrongType_IsIgnored()
    {
        var regions = new FakeDockRegionSink();
        var viewModel = CreateViewModel(regions: regions);

        Assert.False(viewModel.EnterRegionCommand.CanExecute("Tab"));
        viewModel.EnterRegionCommand.Execute("Tab");
        viewModel.EnterRegionCommand.Execute(null);

        Assert.Empty(regions.Entered);
    }

    [Fact]
    public void DragCommand_RaisesDragRequested()
    {
        var viewModel = CreateViewModel();
        var requests = 0;
        viewModel.DragRequested += (_, _) => requests++;

        viewModel.DragCommand.Execute(null);

        Assert.Equal(1, requests);
    }

    [Theory]
    [InlineData(DockEdge.Top)]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    [InlineData(DockEdge.Bottom)]
    [InlineData(null)]
    public void ApplyPlacement_MirrorsTheEdge(DockEdge? edge)
    {
        var viewModel = CreateViewModel();

        viewModel.ApplyPlacement(edge);

        Assert.Equal(edge, viewModel.DockEdge);
    }

    [Fact]
    public void ApplyPlacement_RaisesPropertyChangedForDockEdge()
    {
        // The per-edge storyboards of the view are conditioned on DockEdge, so the notification is part of the contract.
        var viewModel = CreateViewModel();
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raised.Add(arguments.PropertyName);

        viewModel.ApplyPlacement(DockEdge.Left);

        Assert.Equal(new[] { nameof(DockBarViewModel.DockEdge) }, raised);
    }

    [Fact]
    public void Constructor_WithoutAThemeService_UsesTheLightTheme()
    {
        var viewModel = CreateViewModel(theme: null);

        Assert.True(viewModel.IsLightTheme);
        Assert.Equal("#CC101010", viewModel.TabBackground);
        Assert.Equal("#FFFFFFFF", viewModel.TabForeground);
    }

    [Fact]
    public void Constructor_AdoptsTheThemeOfTheService()
    {
        var theme = new FakeThemeService(isLightTheme: false);

        var viewModel = CreateViewModel(theme: theme);

        Assert.False(viewModel.IsLightTheme);
        Assert.Equal("#CCF0F0F0", viewModel.TabBackground);
        Assert.Equal("#FF000000", viewModel.TabForeground);
    }

    [Fact]
    public void ThemeChange_InvertsTheTabColorsWithoutRestarting()
    {
        // HU02 CA #1, still in force after the split of ADR-003: the tone is inverted with respect to the system theme.
        var theme = new FakeThemeService(isLightTheme: true);
        var viewModel = CreateViewModel(theme: theme);

        theme.Change(isLightTheme: false);

        Assert.False(viewModel.IsLightTheme);
        Assert.Equal("#CCF0F0F0", viewModel.TabBackground);
        Assert.Equal("#FF000000", viewModel.TabForeground);
    }

    [Fact]
    public void ThemeChange_RaisesPropertyChangedForTheColors()
    {
        var theme = new FakeThemeService(isLightTheme: true);
        var viewModel = CreateViewModel(theme: theme);
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raised.Add(arguments.PropertyName);

        theme.Change(isLightTheme: false);

        Assert.Contains(nameof(DockBarViewModel.IsLightTheme), raised);
        Assert.Contains(nameof(DockBarViewModel.TabBackground), raised);
        Assert.Contains(nameof(DockBarViewModel.TabForeground), raised);
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_Throws()
    {
        var logger = new RecordingLogger();
        var states = new FakeDockStateService();
        var regions = new FakeDockRegionSink();

        Assert.Throws<ArgumentNullException>(() => new DockBarViewModel(null!, states, regions, null));
        Assert.Throws<ArgumentNullException>(() => new DockBarViewModel(logger, null!, regions, null));
        Assert.Throws<ArgumentNullException>(() => new DockBarViewModel(logger, states, null!, null));
    }

    private static DockBarViewModel CreateViewModel(
        RecordingLogger? logger = null,
        FakeDockStateService? states = null,
        FakeDockRegionSink? regions = null,
        IThemeService? theme = null)
        => new(logger ?? new RecordingLogger(), states ?? new FakeDockStateService(), regions ?? new FakeDockRegionSink(), theme);

    /// <summary>
    /// Theme service double that lets the test raise a theme change, as the Windows hook does in the application.
    /// </summary>
    private sealed class FakeThemeService : IThemeService
    {
        public FakeThemeService(bool isLightTheme) => IsLightTheme = isLightTheme;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public bool IsLightTheme { get; private set; }

        public void Change(bool isLightTheme)
        {
            IsLightTheme = isLightTheme;
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(isLightTheme));
        }

        public void Dispose()
        {
        }
    }
}
