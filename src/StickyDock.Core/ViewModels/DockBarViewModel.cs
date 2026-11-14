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
using StickyDock.Core.Shortcuts;

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
    private readonly IShellLinkReader? _shellLinkReader;
    private readonly IShortcutLauncher? _shortcutLauncher;
    private readonly IUserAlertService? _userAlertService;
    private readonly Dictionary<string, (byte[]? IconPngBytes, string TargetPath)> _metadataCache =
        new(StringComparer.OrdinalIgnoreCase);

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
        : this(
            logger,
            dockStateService,
            regions,
            themeService,
            configuration,
            deletionConfirmer,
            shellLinkReader: null,
            shortcutLauncher: null,
            userAlertService: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarViewModel"/> class with full ribbon and shortcut
    /// persistence, resolution, launching and user alert support (E4, E5).
    /// </summary>
    public DockBarViewModel(
        ILogger logger,
        IDockStateService dockStateService,
        IDockRegionSink regions,
        IThemeService? themeService,
        IConfigurationDocument? configuration,
        IRibbonDeletionConfirmer? deletionConfirmer,
        IShellLinkReader? shellLinkReader,
        IShortcutLauncher? shortcutLauncher,
        IUserAlertService? userAlertService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockStateService = dockStateService ?? throw new ArgumentNullException(nameof(dockStateService));
        _regions = regions ?? throw new ArgumentNullException(nameof(regions));
        _themeService = themeService;
        _configuration = configuration;
        _deletionConfirmer = deletionConfirmer;
        _shellLinkReader = shellLinkReader;
        _shortcutLauncher = shortcutLauncher;
        _userAlertService = userAlertService;

        _isLightTheme = _themeService?.IsLightTheme ?? true;
        _dockState = _dockStateService.State;

        DragCommand = new RelayCommand(() => DragRequested?.Invoke(this, EventArgs.Empty));
        EnterRegionCommand = new RelayCommand<DockRegion>(region => _regions.EnterRegion(region));
        LeaveRegionCommand = new RelayCommand<DockRegion>(region => _regions.LeaveRegion(region));
        CreateRibbonCommand = new RelayCommand(() => CreateRibbon());
        DropFilesOnBarCommand = new RelayCommand<IEnumerable<string>>(
            files =>
            {
                if (files is not null)
                {
                    DropFilesOnBar(files);
                }
            });
        ReorderBarItemsCommand = new RelayCommand<ShortcutReorderRequest>(
            request =>
            {
                if (request is not null)
                {
                    ReorderBarItems(request.SourceItemId, request.TargetItemId);
                }
            });

        LoadConfiguration();

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

    /// <summary>Gets the command invoked when files are dropped onto the main dock bar (E5-CA1, E5-CA3).</summary>
    public ICommand DropFilesOnBarCommand { get; }

    /// <summary>Gets the command invoked when a shortcut is reordered on the main dock bar (E5-CA5).</summary>
    public ICommand ReorderBarItemsCommand { get; }

    /// <summary>Gets the flat ribbons defined on the dock bar in display order.</summary>
    public ObservableCollection<RibbonViewModel> Ribbons { get; } = [];

    /// <summary>Gets the shortcut items pinned directly on the main dock bar in display order (E5).</summary>
    public ObservableCollection<ShortcutItemViewModel> BarItems { get; } = [];

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
    public int BarItemCount => BarItems.Count;

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

    /// <summary>
    /// Accepts dropped <c>.lnk</c> files onto the main dock bar and rejects any other file or folder (E5-CA1, E5-CA3, Q-01).
    /// </summary>
    public bool DropFilesOnBar(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var added = AddDroppedShortcuts(paths, BarItems, containerRibbonId: null);

        if (added == 0)
        {
            return false;
        }

        ReindexItems(BarItems);
        PersistBarItems();
        NotifyLayoutChanged();
        return true;
    }

    /// <summary>
    /// Accepts dropped <c>.lnk</c> files onto the specified ribbon and rejects any other file or folder (E5-CA1, E5-CA3, Q-01).
    /// </summary>
    public bool DropFilesOnRibbon(string ribbonId, IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var ribbon = FindRibbon(ribbonId);

        if (ribbon is null)
        {
            return false;
        }

        var added = AddDroppedShortcuts(paths, ribbon.ShortcutItems, containerRibbonId: ribbon.Id);

        if (added == 0)
        {
            return false;
        }

        ReindexItems(ribbon.ShortcutItems);
        PersistRibbons();
        return true;
    }

    /// <summary>
    /// Launches a shortcut while preserving its <c>.lnk</c> target, arguments and working directory (E5-CA2, §4.5-03).
    /// </summary>
    public void LaunchShortcut(ShortcutItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_shortcutLauncher is null)
        {
            return;
        }

        var result = _shortcutLauncher.Launch(item.ShortcutPath);

        if (!result.Succeeded)
        {
            _userAlertService?.ShowError(
                "Cannot Launch Shortcut",
                result.ErrorMessage ?? $"Could not launch '{item.Name}'.");
        }
    }

    /// <summary>
    /// Moves a shortcut from the main bar or another ribbon into <paramref name="targetRibbonId"/> (E5-CA4, §4.5-04).
    /// </summary>
    public bool MoveShortcutToRibbon(string itemId, string targetRibbonId)
    {
        var targetRibbon = FindRibbon(targetRibbonId);

        if (targetRibbon is null)
        {
            return false;
        }

        var found = FindShortcut(itemId);

        if (found is null || string.Equals(found.Value.Item.ContainerRibbonId, targetRibbonId, StringComparison.Ordinal))
        {
            return false;
        }

        var (item, sourceCollection, sourceRibbon) = found.Value;
        sourceCollection.Remove(item);
        ReindexItems(sourceCollection);

        item.ContainerRibbonId = targetRibbonId;
        targetRibbon.ShortcutItems.Add(item);
        ReindexItems(targetRibbon.ShortcutItems);

        PersistAllShortcuts(sourceRibbon is null);

        if (sourceRibbon is null)
        {
            NotifyLayoutChanged();
        }

        _logger.Information($"Shortcut '{item.Name}' moved to ribbon '{targetRibbon.Name}'.");
        return true;
    }

    /// <summary>
    /// Moves a shortcut from its containing ribbon back to the main dock bar (E5-CA4, §4.5-04).
    /// </summary>
    public bool MoveShortcutToBar(string itemId)
    {
        var found = FindShortcut(itemId);

        if (found is null || found.Value.SourceRibbon is null)
        {
            return false;
        }

        var (item, sourceCollection, _) = found.Value;
        sourceCollection.Remove(item);
        ReindexItems(sourceCollection);

        item.ContainerRibbonId = null;
        BarItems.Add(item);
        ReindexItems(BarItems);

        PersistAllShortcuts(includeBarItems: true);
        NotifyLayoutChanged();
        _logger.Information($"Shortcut '{item.Name}' moved to main bar.");
        return true;
    }

    /// <summary>
    /// Removes a shortcut from the main bar or from its containing ribbon (E5-CA4, §4.5-04).
    /// </summary>
    public bool RemoveShortcut(string itemId)
    {
        var found = FindShortcut(itemId);

        if (found is null)
        {
            return false;
        }

        var (item, sourceCollection, sourceRibbon) = found.Value;
        sourceCollection.Remove(item);
        ReindexItems(sourceCollection);

        if (sourceRibbon is null)
        {
            PersistBarItems();
            NotifyLayoutChanged();
        }
        else
        {
            PersistRibbons();
        }

        _logger.Information($"Shortcut '{item.Name}' removed.");
        return true;
    }

    /// <summary>
    /// Reorders a shortcut on the main bar to <paramref name="newIndex"/> and persists the order (E5-CA5).
    /// </summary>
    public bool ReorderBarItem(string itemId, int newIndex)
        => ReorderInCollection(BarItems, itemId, newIndex, PersistBarItems);

    /// <summary>
    /// Moves <paramref name="sourceItemId"/> to the slot currently occupied by <paramref name="targetItemId"/> on the
    /// main bar (E5-CA5).
    /// </summary>
    public bool ReorderBarItems(string sourceItemId, string targetItemId)
        => ReorderByIds(BarItems, sourceItemId, targetItemId, PersistBarItems);

    /// <summary>
    /// Reorders a shortcut inside a ribbon to <paramref name="newIndex"/> and persists the order (E5-CA5).
    /// </summary>
    public bool ReorderRibbonItem(string ribbonId, string itemId, int newIndex)
    {
        var ribbon = FindRibbon(ribbonId);
        return ribbon is not null && ReorderInCollection(ribbon.ShortcutItems, itemId, newIndex, PersistRibbons);
    }

    /// <summary>
    /// Moves <paramref name="sourceItemId"/> to the slot currently occupied by <paramref name="targetItemId"/> inside
    /// <paramref name="ribbonId"/> (E5-CA5).
    /// </summary>
    public bool ReorderRibbonItems(string ribbonId, string sourceItemId, string targetItemId)
    {
        var ribbon = FindRibbon(ribbonId);
        return ribbon is not null && ReorderByIds(ribbon.ShortcutItems, sourceItemId, targetItemId, PersistRibbons);
    }

    /// <summary>Moves a shortcut one slot earlier within its container (E5-CA5).</summary>
    public bool MoveShortcutEarlier(string itemId) => ShiftShortcut(itemId, delta: -1);

    /// <summary>Moves a shortcut one slot later within its container (E5-CA5).</summary>
    public bool MoveShortcutLater(string itemId) => ShiftShortcut(itemId, delta: 1);

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

    private void LoadConfiguration()
    {
        if (_configuration is null)
        {
            return;
        }

        foreach (var item in _configuration.Current.BarItems.OrderBy(i => i.Order))
        {
            BarItems.Add(CreateShortcutViewModel(item, containerRibbonId: null));
        }

        foreach (var ribbon in _configuration.Current.Ribbons.OrderBy(r => r.Order))
        {
            var ribbonVm = CreateRibbonViewModel(ribbon);

            foreach (var item in ribbon.Items.OrderBy(i => i.Order))
            {
                ribbonVm.ShortcutItems.Add(CreateShortcutViewModel(item, containerRibbonId: ribbon.Id));
            }

            Ribbons.Add(ribbonVm);
        }
    }

    private RibbonViewModel CreateRibbonViewModel(Ribbon model)
        => new(
            model,
            ToggleRibbon,
            RenameRibbon,
            DeleteRibbon,
            MoveRibbonEarlier,
            MoveRibbonLater,
            DropFilesOnRibbon,
            ReorderRibbonItems);

    private ShortcutItemViewModel CreateShortcutViewModel(RibbonItem model, string? containerRibbonId)
    {
        var (iconBytes, targetPath) = ResolveMetadataCached(model.ShortcutPath);

        return new ShortcutItemViewModel(
            model,
            containerRibbonId,
            iconBytes,
            targetPath,
            () => Ribbons,
            LaunchShortcut,
            itemId => RemoveShortcut(itemId),
            itemId => MoveShortcutToBar(itemId),
            MoveShortcutToRibbon,
            itemId => MoveShortcutEarlier(itemId),
            itemId => MoveShortcutLater(itemId));
    }

    private int AddDroppedShortcuts(
        IEnumerable<string> paths,
        ObservableCollection<ShortcutItemViewModel> targetCollection,
        string? containerRibbonId)
    {
        var added = 0;

        foreach (var path in paths)
        {
            if (!IsValidShortcutFile(path))
            {
                _logger.Warning($"Rejected non-.lnk drop item '{path}'.");
                _userAlertService?.ShowWarning(
                    "Unsupported Item",
                    $"'{Path.GetFileName(path)}' cannot be added directly. Only .lnk shortcut files are supported; please package executables, folders or scripts as a Windows shortcut (.lnk) first.");
                continue;
            }

            var resolved = TryReadShortcut(path);
            var displayName = resolved?.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Path.GetFileNameWithoutExtension(path);
            }

            var model = new RibbonItem(
                Guid.NewGuid().ToString("N"),
                displayName,
                path,
                targetCollection.Count);

            var itemVm = new ShortcutItemViewModel(
                model,
                containerRibbonId,
                resolved?.IconPngBytes,
                resolved?.TargetPath ?? string.Empty,
                () => Ribbons,
                LaunchShortcut,
                itemId => RemoveShortcut(itemId),
                itemId => MoveShortcutToBar(itemId),
                MoveShortcutToRibbon,
                itemId => MoveShortcutEarlier(itemId),
                itemId => MoveShortcutLater(itemId));

            targetCollection.Add(itemVm);
            added++;
        }

        return added;
    }

    private static bool IsValidShortcutFile(string? path)
        => !string.IsNullOrWhiteSpace(path)
            && string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase)
            && !Directory.Exists(path)
            && File.Exists(path);

    private ResolvedShortcut? TryReadShortcut(string shortcutPath)
    {
        if (_shellLinkReader is null)
        {
            return null;
        }

        try
        {
            var resolved = _shellLinkReader.Read(shortcutPath);
            _metadataCache[shortcutPath] = (resolved.IconPngBytes, resolved.TargetPath);
            return resolved;
        }
        catch (Exception exception)
        {
            _logger.Warning($"Could not resolve shortcut '{shortcutPath}': {exception.Message}");
            return null;
        }
    }

    private (byte[]? IconPngBytes, string TargetPath) ResolveMetadataCached(string shortcutPath)
    {
        if (_metadataCache.TryGetValue(shortcutPath, out var cached))
        {
            return cached;
        }

        if (_shellLinkReader is null || !File.Exists(shortcutPath))
        {
            return (null, string.Empty);
        }

        var resolved = TryReadShortcut(shortcutPath);
        return resolved is null ? (null, string.Empty) : (resolved.IconPngBytes, resolved.TargetPath);
    }

    private (ShortcutItemViewModel Item, ObservableCollection<ShortcutItemViewModel> Collection, RibbonViewModel? SourceRibbon)? FindShortcut(string itemId)
    {
        foreach (var item in BarItems)
        {
            if (string.Equals(item.Id, itemId, StringComparison.Ordinal))
            {
                return (item, BarItems, null);
            }
        }

        foreach (var ribbon in Ribbons)
        {
            foreach (var item in ribbon.ShortcutItems)
            {
                if (string.Equals(item.Id, itemId, StringComparison.Ordinal))
                {
                    return (item, ribbon.ShortcutItems, ribbon);
                }
            }
        }

        return null;
    }

    private bool ShiftShortcut(string itemId, int delta)
    {
        var found = FindShortcut(itemId);

        if (found is null)
        {
            return false;
        }

        var (_, collection, sourceRibbon) = found.Value;
        var currentIndex = IndexOfItem(collection, itemId);
        var newIndex = currentIndex + delta;

        if (currentIndex < 0 || newIndex < 0 || newIndex >= collection.Count)
        {
            return false;
        }

        return ReorderInCollection(
            collection,
            itemId,
            newIndex,
            sourceRibbon is null ? PersistBarItems : PersistRibbons);
    }

    private static bool ReorderByIds(
        ObservableCollection<ShortcutItemViewModel> collection,
        string sourceItemId,
        string targetItemId,
        Action persist)
    {
        var targetIndex = IndexOfItem(collection, targetItemId);
        return targetIndex >= 0 && ReorderInCollection(collection, sourceItemId, targetIndex, persist);
    }

    private static bool ReorderInCollection(
        ObservableCollection<ShortcutItemViewModel> collection,
        string itemId,
        int newIndex,
        Action persist)
    {
        var currentIndex = IndexOfItem(collection, itemId);

        if (currentIndex < 0 || newIndex < 0 || newIndex >= collection.Count || currentIndex == newIndex)
        {
            return false;
        }

        collection.Move(currentIndex, newIndex);
        ReindexItems(collection);
        persist();
        return true;
    }

    private static int IndexOfItem(ObservableCollection<ShortcutItemViewModel> collection, string itemId)
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (string.Equals(collection[i].Id, itemId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static void ReindexItems(ObservableCollection<ShortcutItemViewModel> collection)
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Order = i;
        }
    }

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

    private void PersistBarItems()
    {
        if (_configuration is null)
        {
            return;
        }

        var snapshot = BarItems.Select(i => i.ToModel()).ToArray();
        _configuration.Update(doc => doc with { BarItems = snapshot });
    }

    private void PersistAllShortcuts(bool includeBarItems)
    {
        if (_configuration is null)
        {
            return;
        }

        var ribbonsSnapshot = Ribbons.Select(r => r.ToModel()).ToArray();

        if (includeBarItems)
        {
            var barSnapshot = BarItems.Select(i => i.ToModel()).ToArray();
            _configuration.Update(doc => doc with { Ribbons = ribbonsSnapshot, BarItems = barSnapshot });
        }
        else
        {
            _configuration.Update(doc => doc with { Ribbons = ribbonsSnapshot });
        }
    }

    private void NotifyLayoutChanged()
    {
        OnPropertyChanged(nameof(RibbonCount));
        OnPropertyChanged(nameof(BarItemCount));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
}
