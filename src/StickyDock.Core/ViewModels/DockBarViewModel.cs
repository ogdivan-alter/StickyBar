using System.Windows.Input;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Mvvm;
using StickyDock.Core.Services;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// View model for the dock bar. Holds the dock state, the theme state and the docked edge, without any view reference
/// and without owning any interaction, docking or animation decision.
/// </summary>
/// <remarks>
/// <para>
/// The docked edge is decided and persisted by <see cref="DockPlacementCoordinator"/>; this view model only mirrors what
/// the coordinator reports (ADR-002). The interaction policy lives in <see cref="DockInteractionController"/>, which is
/// the single implementation of both <see cref="IDockStateService"/> and <see cref="IDockRegionSink"/> (ADR-003, D7).
/// </para>
/// <para>
/// The animations are storyboards of the view triggered by <see cref="DockState"/>: no duration and no offset crosses
/// this class, because the animation timelines cannot receive them by binding (ADR-003, D5).
/// </para>
/// </remarks>
public sealed class DockBarViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IDockStateService _dockStateService;
    private readonly IDockRegionSink _regions;
    private readonly IThemeService? _themeService;

    private bool _isLightTheme;
    private DockState _dockState = DockState.Tab;
    private DockEdge? _dockEdge;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger of the view model.</param>
    /// <param name="dockStateService">Service that owns the dock state.</param>
    /// <param name="regions">Sink that receives the pointer entries and exits of the interactive regions.</param>
    /// <param name="themeService">Optional source of the Windows light/dark theme.</param>
    public DockBarViewModel(
        ILogger logger,
        IDockStateService dockStateService,
        IDockRegionSink regions,
        IThemeService? themeService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockStateService = dockStateService ?? throw new ArgumentNullException(nameof(dockStateService));
        _regions = regions ?? throw new ArgumentNullException(nameof(regions));
        _themeService = themeService;

        _isLightTheme = _themeService?.IsLightTheme ?? true;
        _dockState = _dockStateService.State;

        DragCommand = new RelayCommand(() => DragRequested?.Invoke(this, EventArgs.Empty));
        EnterRegionCommand = new RelayCommand<DockRegion>(region => _regions.EnterRegion(region));
        LeaveRegionCommand = new RelayCommand<DockRegion>(region => _regions.LeaveRegion(region));

        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
        }

        _dockStateService.StateChanged += OnDockStateChanged;

        _logger.Information("Dock bar view model created.");
    }

    /// <summary>
    /// Raised when the view asks for a drag of the bar. The composition root forwards it to the docking coordinator.
    /// </summary>
    public event EventHandler? DragRequested;

    /// <summary>
    /// Gets the command that starts a drag of the bar.
    /// </summary>
    /// <remarks>
    /// The window is entirely client area, so the pointer events of E1 and E3 keep arriving and the drag cannot be a
    /// non-client caption. The gesture ends up in <c>IDockWindowHost.BeginDrag</c> (contingency of ADR-002).
    /// </remarks>
    public ICommand DragCommand { get; }

    /// <summary>
    /// Gets the command an element executes when the pointer enters its region. The parameter is the
    /// <see cref="DockRegion"/> attached to that element.
    /// </summary>
    public ICommand EnterRegionCommand { get; }

    /// <summary>
    /// Gets the command an element executes when the pointer leaves its region. The parameter is the
    /// <see cref="DockRegion"/> attached to that element.
    /// </summary>
    public ICommand LeaveRegionCommand { get; }

    /// <summary>Gets whether Windows is using the light theme.</summary>
    public bool IsLightTheme
    {
        get => _isLightTheme;
        private set => SetProperty(ref _isLightTheme, value);
    }

    /// <summary>Gets the current dock state. It drives every visual trigger of the view.</summary>
    public DockState DockState
    {
        get => _dockState;
        private set => SetProperty(ref _dockState, value);
    }

    /// <summary>Gets the tab background color as ARGB string for binding.</summary>
    public string TabBackground
    {
        get
        {
            // Inverse of system theme for high contrast (HU02 CA #1)
            // Dark theme -> light translucent tab; Light theme -> dark translucent tab
            if (IsLightTheme)
            {
                // Light theme: use dark translucent
                return "#CC101010";
            }

            // Dark theme: use light translucent
            return "#CCF0F0F0";
        }
    }

    /// <summary>Gets the tab foreground color.</summary>
    public string TabForeground
    {
        get
        {
            if (IsLightTheme)
            {
                return "#FFFFFFFF";
            }

            return "#FF000000";
        }
    }

    /// <summary>
    /// Gets the edge the bar is docked to, or <see langword="null"/> while it floats.
    /// </summary>
    public DockEdge? DockEdge
    {
        get => _dockEdge;
        private set => SetProperty(ref _dockEdge, value);
    }

    /// <summary>
    /// Mirrors the placement reported by the docking coordinator.
    /// </summary>
    /// <param name="edge">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
    public void ApplyPlacement(DockEdge? edge) => DockEdge = edge;

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        IsLightTheme = e.IsLightTheme;
        OnPropertyChanged(nameof(TabBackground));
        OnPropertyChanged(nameof(TabForeground));
        _logger.Information($"Theme changed to {(e.IsLightTheme ? "Light" : "Dark")}.");
    }

    private void OnDockStateChanged(object? sender, DockStateChangedEventArgs e)
    {
        DockState = e.State;
    }
}
