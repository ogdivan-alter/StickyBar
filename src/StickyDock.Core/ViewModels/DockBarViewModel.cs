using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Mvvm;
using StickyDock.Core.Ribbons;
using StickyDock.Core.Services;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// View model for the dock bar. Holds the dock state, the theme state, the docked edge and the flat ribbons, without
/// any view reference and without owning any interaction, docking or animation decision.
/// </summary>
/// <remarks>
/// <para>
/// The docked edge is decided and persisted by <see cref="DockPlacementCoordinator"/>; this view model only mirrors what
/// the coordinator reports (ADR-002). The interaction policy lives in <see cref="DockInteractionController"/>, which is
/// the single implementation of both <see cref="IDockStateService"/> and <see cref="IDockRegionSink"/> (ADR-003, D7).
/// </para>
/// <para>
/// The animations are storyboards of the view triggered by <see cref="DockState"/> and <see cref="HasOpenRibbon"/>: no
/// duration and no offset crosses this class, because the animation timelines cannot receive them by binding (ADR-003, D5).
/// </para>
/// </remarks>
public sealed class DockBarViewModel : ViewModelBase, IRibbonLayoutState
{
    private readonly ILogger _logger;
    private readonly IDockStateService _dockStateService;
    private readonly IDockRegionSink _regions;
    private readonly IThemeService? _themeService;
    private readonly IConfigurationDocument? _configuration;
    private readonly IRibbonDeletionConfirmer? _deletionConfirmer;

    private bool _isLightTheme;
    private DockState _dockState = DockState.Tab;
    private DockEdge? _dockEdge;
    private RibbonViewModel? _openRibbon;

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
        : this(logger, dockStateService, regions, themeService, configuration: null, deletionConfirmer: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarViewModel"/> class with ribbon persistence and deletion
    /// confirmation support (E4).
    /// </summary>
    /// <param name="logger">Logger of the view model.</param>
    /// <param name="dockStateService">Service that owns the dock state.</param>
    /// <param name="regions">Sink that receives the pointer entries and exits of the interactive regions.</param>
    /// <param name="themeService">Optional source of the Windows light/dark theme.</param>
    /// <param name="configuration">Optional configuration document where ribbons are persisted.</param>
    /// <param name="deletionConfirmer">Optional confirmer invoked when deleting a non-empty ribbon.</param>
    public DockBarViewModel(
        ILogger logger,
        IDockStateService dockStateService,
        IDockRegionSink regions,
        IThemeService? themeService,
        IConfigurationDocument? configuration,
        IRibbonDeletionConfirmer? deletionConfirmer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockStateService = dockStateService ?? throw new ArgumentNullException(nameof(dockStateService));
        _regions = regions ?? throw new ArgumentNullException(nameof(regions));
        _themeService = themeService;
        _configuration = configuration;
        _deletionConfirmer = deletionConfirmer;

        _isLightTheme = _themeService?.IsLightTheme ?? true;
        _dockState = _dockStateService.State;

        DragCommand = new RelayCommand(() => DragRequested?.Invoke(this, EventArgs.Empty));
        EnterRegionCommand = new RelayCommand<DockRegion>(region => _regions.EnterRegion(region));
        LeaveRegionCommand = new RelayCommand<DockRegion>(region => _regions.LeaveRegion(region));
        CreateRibbonCommand = new RelayCommand(() => CreateRibbon());

        LoadRibbons();

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
    /// Raised when ribbons or the open ribbon change, so the composition root can ask the docking coordinator to
    /// resize the window (E4-CA6).
    /// </summary>
    public event EventHandler? LayoutChanged;

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

    /// <summary>Gets the command that creates a new ribbon on the dock bar (E4-CA4).</summary>
    public ICommand CreateRibbonCommand { get; }

    /// <summary>Gets the flat ribbons defined on the dock bar in display order.</summary>
    public ObservableCollection<RibbonViewModel> Ribbons { get; } = [];

    /// <summary>Gets the currently open ribbon, or <see langword="null"/> when all ribbons are closed.</summary>
    public RibbonViewModel? OpenRibbon
    {
        get => _openRibbon;
        private set
        {
            if (SetProperty(ref _openRibbon, value))
            {
                OnPropertyChanged(nameof(HasOpenRibbon));
            }
        }
    }

    /// <inheritdoc />
    public bool HasOpenRibbon => _openRibbon is not null;

    /// <inheritdoc />
    public int RibbonCount => Ribbons.Count;

    /// <inheritdoc />
    public int BarItemCount => 0;

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

    /// <summary>
    /// Creates a new flat ribbon, persists it immediately and notifies the layout (E4-CA4, E4-CA6).
    /// </summary>
    /// <param name="name">Optional display name. When omitted or blank, a default name is generated.</param>
    /// <returns>The created ribbon view model.</returns>
    public RibbonViewModel CreateRibbon(string? name = null)
    {
        var trimmed = name?.Trim();
        var effectiveName = string.IsNullOrEmpty(trimmed)
            ? string.Create(CultureInfo.InvariantCulture, $"Ribbon {Ribbons.Count + 1}")
            : trimmed;

        var model = new Ribbon
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = effectiveName,
            Order = Ribbons.Count,
            Items = [],
        };

        var vm = CreateRibbonViewModel(model);
        Ribbons.Add(vm);
        PersistRibbons();
        NotifyLayoutChanged();
        _logger.Information($"Ribbon '{vm.Name}' created (total = {Ribbons.Count}).");
        return vm;
    }

    /// <summary>
    /// Renames an existing ribbon and persists the change immediately (E4-CA4).
    /// </summary>
    public bool RenameRibbon(string id, string newName)
    {
        var ribbon = FindRibbon(id);
        var trimmed = newName?.Trim();

        if (ribbon is null || string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        if (string.Equals(ribbon.Name, trimmed, StringComparison.Ordinal))
        {
            return true;
        }

        ribbon.Name = trimmed;
        ribbon.DraftName = trimmed;
        PersistRibbons();
        _logger.Information($"Ribbon '{id}' renamed to '{trimmed}'.");
        return true;
    }

    /// <summary>
    /// Moves a ribbon to <paramref name="newIndex"/> and persists the new order (E4-CA4).
    /// </summary>
    public bool MoveRibbon(string id, int newIndex)
    {
        var currentIndex = IndexOfRibbon(id);

        if (currentIndex < 0 || newIndex < 0 || newIndex >= Ribbons.Count || currentIndex == newIndex)
        {
            return false;
        }

        Ribbons.Move(currentIndex, newIndex);
        ReindexRibbons();
        PersistRibbons();
        _logger.Information($"Ribbon '{id}' moved from {currentIndex} to {newIndex}.");
        return true;
    }

    /// <summary>Moves a ribbon one slot earlier along the bar.</summary>
    public bool MoveRibbonEarlier(string id)
    {
        var index = IndexOfRibbon(id);
        return index > 0 && MoveRibbon(id, index - 1);
    }

    /// <summary>Moves a ribbon one slot later along the bar.</summary>
    public bool MoveRibbonLater(string id)
    {
        var index = IndexOfRibbon(id);
        return index >= 0 && index < Ribbons.Count - 1 && MoveRibbon(id, index + 1);
    }

    /// <summary>
    /// Deletes a ribbon, requesting explicit confirmation when it contains shortcut items (E4-CA5, Q-07).
    /// </summary>
    public bool DeleteRibbon(string id)
    {
        var index = IndexOfRibbon(id);

        if (index < 0)
        {
            return false;
        }

        var ribbon = Ribbons[index];

        if (ribbon.ItemCount > 0 && _deletionConfirmer is not null)
        {
            if (!_deletionConfirmer.ConfirmDeleteRibbon(ribbon.Name, ribbon.ItemCount))
            {
                _logger.Information($"Deletion of ribbon '{ribbon.Name}' ({ribbon.ItemCount} items) was cancelled.");
                return false;
            }
        }

        if (ReferenceEquals(OpenRibbon, ribbon))
        {
            ribbon.IsOpen = false;
            OpenRibbon = null;
        }

        Ribbons.RemoveAt(index);
        ReindexRibbons();
        PersistRibbons();
        NotifyLayoutChanged();
        _logger.Information($"Ribbon '{ribbon.Name}' deleted.");
        return true;
    }

    /// <summary>
    /// Toggles a ribbon open or closed with exclusive accordion semantics (E4-CA2, HU03 CA #2).
    /// </summary>
    public void ToggleRibbon(string id)
    {
        var target = FindRibbon(id);

        if (target is null)
        {
            return;
        }

        if (ReferenceEquals(OpenRibbon, target))
        {
            target.IsOpen = false;
            OpenRibbon = null;
            NotifyLayoutChanged();
            return;
        }

        if (OpenRibbon is not null)
        {
            OpenRibbon.IsOpen = false;
        }

        target.IsOpen = true;
        OpenRibbon = target;
        NotifyLayoutChanged();
    }

    /// <summary>
    /// Closes whichever ribbon is currently open, if any (Q-10).
    /// </summary>
    public void CloseOpenRibbon()
    {
        if (OpenRibbon is null)
        {
            return;
        }

        OpenRibbon.IsOpen = false;
        OpenRibbon = null;
        NotifyLayoutChanged();
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

        // Q-10: leaving Expanded (either via grace collapse or explicit collapse) closes any open ribbon.
        if (e.State != DockState.Expanded)
        {
            CloseOpenRibbon();
        }
    }

    private void LoadRibbons()
    {
        if (_configuration is null)
        {
            return;
        }

        foreach (var ribbon in _configuration.Current.Ribbons.OrderBy(r => r.Order))
        {
            Ribbons.Add(CreateRibbonViewModel(ribbon));
        }
    }

    private RibbonViewModel CreateRibbonViewModel(Ribbon model)
        => new(
            model,
            ToggleRibbon,
            RenameRibbon,
            DeleteRibbon,
            MoveRibbonEarlier,
            MoveRibbonLater);

    private RibbonViewModel? FindRibbon(string id)
        => Ribbons.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.Ordinal));

    private int IndexOfRibbon(string id)
    {
        for (var i = 0; i < Ribbons.Count; i++)
        {
            if (string.Equals(Ribbons[i].Id, id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void ReindexRibbons()
    {
        for (var i = 0; i < Ribbons.Count; i++)
        {
            Ribbons[i].Order = i;
        }
    }

    private void PersistRibbons()
    {
        if (_configuration is null)
        {
            return;
        }

        var snapshot = Ribbons.Select(r => r.ToModel()).ToArray();
        _configuration.Update(doc => doc with { Ribbons = snapshot });
    }

    private void NotifyLayoutChanged()
    {
        OnPropertyChanged(nameof(RibbonCount));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
}
