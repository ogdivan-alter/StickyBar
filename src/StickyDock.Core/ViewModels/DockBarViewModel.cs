using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Mvvm;
using StickyDock.Core.Services;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// View model for the dock bar. Holds state (tab/expanded) and theme state without any view references.
/// </summary>
public sealed class DockBarViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IDockStateService _dockStateService;
    private readonly IThemeService? _themeService;
    private readonly DockEdgeService _dockEdgeService;
    private readonly IConfigurationStore<StickyDockConfiguration> _configurationStore;
    private StickyDockConfiguration _configuration;
    private bool _isLightTheme;
    private DockState _dockState = DockState.Tab;
    private bool _isHovered;
    private double _tabOpacity = 1.0;
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;
    private DockEdge? _dockEdge;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarViewModel"/> class.
    /// </summary>
    public DockBarViewModel(
        ILogger logger,
        IDockStateService dockStateService,
        IThemeService? themeService,
        DockEdgeService dockEdgeService,
        StickyDockConfiguration configuration,
        IConfigurationStore<StickyDockConfiguration> configurationStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockStateService = dockStateService ?? throw new ArgumentNullException(nameof(dockStateService));
        _themeService = themeService;
        _dockEdgeService = dockEdgeService ?? throw new ArgumentNullException(nameof(dockEdgeService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));

        _isLightTheme = _themeService?.IsLightTheme ?? true;
        _dockState = _dockStateService.State;
        _dockEdge = _dockEdgeService.CurrentEdge;

        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
        }
        _dockStateService.StateChanged += OnDockStateChanged;
        _dockEdgeService.EdgeChanged += OnDockEdgeChanged;

        _logger.Information("Dock bar view model created.");
    }

    /// <summary>Gets whether Windows is using the light theme.</summary>
    public bool IsLightTheme
    {
        get => _isLightTheme;
        private set => SetProperty(ref _isLightTheme, value);
    }

    /// <summary>Gets the current dock state.</summary>
    public DockState DockState
    {
        get => _dockState;
        private set => SetProperty(ref _dockState, value);
    }

    /// <summary>Gets or sets whether the tab is hovered.</summary>
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (SetProperty(ref _isHovered, value))
            {
                // Micro-expansion prepared for E3; just update scale now.
                if (value)
                {
                    ScaleX = 1.08;
                    ScaleY = 1.08;
                }
                else
                {
                    ScaleX = 1.0;
                    ScaleY = 1.0;
                }
            }
        }
    }

    /// <summary>Tab opacity.</summary>
    public double TabOpacity
    {
        get => _tabOpacity;
        set => SetProperty(ref _tabOpacity, value);
    }

    /// <summary>Scale X for micro-expansion.</summary>
    public double ScaleX
    {
        get => _scaleX;
        set => SetProperty(ref _scaleX, value);
    }

    /// <summary>Scale Y for micro-expansion.</summary>
    public double ScaleY
    {
        get => _scaleY;
        set => SetProperty(ref _scaleY, value);
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

    /// <summary>Gets the current dock edge.</summary>
    public DockEdge? DockEdge
    {
        get => _dockEdge;
        private set => SetProperty(ref _dockEdge, value);
    }

    private void OnDockEdgeChanged(object? sender, DockEdgeChangedEventArgs e)
    {
        DockEdge = e.Edge;
        _ = PersistDockEdgeAsync(e.Edge);
    }

    private async Task PersistDockEdgeAsync(DockEdge? edge)
    {
        try
        {
            _configuration = _configuration with { DockEdge = edge };
            await _configurationStore.SaveAsync(_configuration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to persist dock edge.", ex);
        }
    }
}
