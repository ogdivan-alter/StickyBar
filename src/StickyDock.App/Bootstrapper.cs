using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using StickyDock.App.Docking;
using StickyDock.App.Services;
using StickyDock.App.Timing;
using StickyDock.App.Views;
using StickyDock.Core.Configuration;
using StickyDock.Core.DependencyInjection;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Services;
using StickyDock.Core.Shortcuts;
using StickyDock.Core.ViewModels;
using StickyDock.Interop.Theme;
using StickyDock.Interop.Win32;

namespace StickyDock.App;

/// <summary>
/// Composition root of the application: builds the services, loads the configuration and creates the shell window.
/// </summary>
/// <remarks>
/// <para>
/// The docking graph is composed in <c>SourceInitialized</c>, because the native handle only exists once the window has
/// been created (H-E2-01). The coordinator owns the event source and is disposed when the window closes.
/// </para>
/// <para>
/// The interaction graph is composed in <see cref="ShowMainWindow"/>, on the UI thread: the scheduler of the timers wraps
/// a <see cref="System.Windows.Threading.DispatcherTimer"/>, which must be created on that thread (ADR-003, D4).
/// </para>
/// </remarks>
internal sealed class Bootstrapper : IDisposable
{
    private readonly ServiceContainer _services;
    private readonly RotatingFileLogger _logger;

    private DockPlacementCoordinator? _dockingCoordinator;
    private DockInteractionController? _interactionController;
    private IDockStateService? _dockStates;
    private DockBarViewModel? _dockBarViewModel;
    private bool _disposed;

    private Bootstrapper(ServiceContainer services, RotatingFileLogger logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Wires the application and creates the configuration file on first run.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels the startup I/O.</param>
    /// <returns>The initialized bootstrapper.</returns>
    public static async Task<Bootstrapper> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var paths = ApplicationPaths.ForCurrentUser();
        var logger = new RotatingFileLogger(new RotatingFileLoggerOptions { DirectoryPath = paths.LogDirectory });
        var services = new ServiceContainer();

        try
        {
            services.AddSingleton<ILogger>(logger);
            services.AddSingleton(paths);

            var store = StickyDockSettings.CreateStore(paths.ConfigurationFilePath, logger);
            services.AddSingleton(store);

            var configuration = await LoadConfigurationAsync(store, logger, cancellationToken).ConfigureAwait(false);

            // The single writer of the preferences (ADR-003, D1): every consumer sees the same instance, and the write is
            // flushed when the container disposes it (D-04).
            var document = new ConfigurationDocument(store, configuration, logger);
            services.AddSingleton<IConfigurationDocument>(document);

            if (!store.Exists)
            {
                await store.SaveAsync(configuration, cancellationToken).ConfigureAwait(false);
                logger.Information($"Initial configuration created at '{store.FilePath}'.");
            }

            services.AddSingleton(new MainViewModel(configuration, logger));

            // E1: theme service.
            var themeService = new WindowsThemeService();
            services.AddSingleton<IWindowsThemeService>(themeService);

            var primaryScreen = DisplayMetrics.GetPrimaryScreenSize();
            logger.Information(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Sticky Dock Utility started. Primary screen: {primaryScreen.WidthInPixels}x{primaryScreen.HeightInPixels} px."));

            return new Bootstrapper(services, logger);
        }
        catch (Exception exception)
        {
            logger.Critical("Startup failed; the application cannot continue.", exception);
            services.Dispose();
            logger.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Composes the interaction, shows the shell window bound to its view model and composes the docking once the native
    /// handle exists.
    /// </summary>
    public void ShowMainWindow()
    {
        ComposeInteraction();

        var mainViewModel = _services.GetRequiredService<MainViewModel>();
        var dockBarViewModel = _services.GetRequiredService<DockBarViewModel>();

        var window = new MainWindow
        {
            DataContext = dockBarViewModel,
            MainViewModel = mainViewModel,
        };

        _dockBarViewModel = dockBarViewModel;

        window.SourceInitialized += (_, _) => AttachDocking(window);
        window.Closed += OnMainWindowClosed;

        window.Show();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        DetachDocking();
        _services.Dispose();
        _logger.Dispose();
    }

    /// <summary>
    /// Builds the interaction graph on the UI thread: the interaction controller is the single implementation of the dock
    /// state and of the region sink, so the same instance is registered under both contracts (ADR-003, D4 and D7).
    /// </summary>
    private void ComposeInteraction()
    {
        if (_services.IsRegistered(typeof(IDockStateService)))
        {
            return;
        }

        var configuration = _services.GetRequiredService<IConfigurationDocument>();
        var controller = new DockInteractionController(new DispatcherTimerScheduler(), configuration, _logger);

        _services.AddSingleton<IDockStateService>(controller);
        _services.AddSingleton<IDockRegionSink>(controller);

        _interactionController = controller;
        var shellLinkReader = new ShellLinkReader();
        var shortcutLauncher = new ShortcutLauncher(shellLinkReader, _logger);
        var userAlertService = new WpfUserAlertService();

        _services.AddSingleton<IShellLinkReader>(shellLinkReader);
        _services.AddSingleton<IShortcutLauncher>(shortcutLauncher);
        _services.AddSingleton<IUserAlertService>(userAlertService);

        var dockBarViewModel = new DockBarViewModel(
            _logger,
            controller,
            controller,
            _services.GetRequiredService<IWindowsThemeService>(),
            configuration,
            new WpfRibbonDeletionConfirmer(),
            shellLinkReader,
            shortcutLauncher,
            userAlertService);

        _services.AddSingleton(dockBarViewModel);

        // The footprint is declared in Core, so the size the coordinator applies is verifiable without the window (D2).
        _services.AddSingleton<IDockBarSizeSource>(
            new DockBarSizeSource(controller, DockBarMetrics.Default, dockBarViewModel));
    }

    private void AttachDocking(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;

        if (handle == nint.Zero)
        {
            _logger.Error("The docking was not started: the window has no native handle.");
            return;
        }

        var messageSource = HwndSource.FromHwnd(handle);

        if (messageSource is null)
        {
            _logger.Error("The docking was not started: the window has no message source.");
            return;
        }

        // Both sources are cheap while idle: window messages and a hook filtered by the taskbar window (ADR-002).
        var events = new CompositeDockingEventSource(
            new WpfMessageDockingEventSource(messageSource),
            new WinEventDockingEventSource());

        var coordinator = new DockPlacementCoordinator(
            events,
            new Win32WindowGeometrySource(handle),
            new WpfDockWindowHost(window),
            _services.GetRequiredService<IDockBarSizeSource>(),
            _services.GetRequiredService<IConfigurationDocument>(),
            _logger);

        var states = _services.GetRequiredService<IDockStateService>();

        _dockingCoordinator = coordinator;
        _dockStates = states;

        coordinator.PlacementChanged += OnPlacementChanged;
        coordinator.DragStarted += OnDragStarted;
        coordinator.DragCompleted += OnDragCompleted;
        states.StateChanged += OnDockStateChanged;

        if (_dockBarViewModel is not null)
        {
            _dockBarViewModel.DragRequested += OnDragRequested;
            _dockBarViewModel.LayoutChanged += OnLayoutChanged;
        }

        try
        {
            coordinator.Start();
        }
        catch (Exception exception)
        {
            // The bar keeps working where it is; only the docking stays inactive.
            _logger.Critical("The docking could not be started; the bar stays where it is.", exception);
            DetachDocking();
        }
    }

    private void OnPlacementChanged(object? sender, DockPlacementChangedEventArgs e)
        => _dockBarViewModel?.ApplyPlacement(e.Edge);

    /// <summary>
    /// Grows or shrinks the bar to the footprint of the new state (ADR-003, D-09).
    /// </summary>
    private void OnDockStateChanged(object? sender, DockStateChangedEventArgs e)
        => _dockingCoordinator?.NotifyBarSizeChanged();

    private void OnLayoutChanged(object? sender, EventArgs e)
        => _dockingCoordinator?.NotifyBarSizeChanged();

    private void OnDragStarted(object? sender, EventArgs e) => _interactionController?.BeginDrag();

    private void OnDragCompleted(object? sender, EventArgs e) => _interactionController?.EndDrag();

    private void OnDragRequested(object? sender, EventArgs e) => _dockingCoordinator?.BeginDrag();

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Closed -= OnMainWindowClosed;
        }

        DetachDocking();
    }

    private void DetachDocking()
    {
        var coordinator = _dockingCoordinator;

        if (coordinator is null)
        {
            return;
        }

        _dockingCoordinator = null;
        coordinator.PlacementChanged -= OnPlacementChanged;
        coordinator.DragStarted -= OnDragStarted;
        coordinator.DragCompleted -= OnDragCompleted;

        if (_dockStates is not null)
        {
            _dockStates.StateChanged -= OnDockStateChanged;
            _dockStates = null;
        }

        if (_dockBarViewModel is not null)
        {
            _dockBarViewModel.DragRequested -= OnDragRequested;
            _dockBarViewModel.LayoutChanged -= OnLayoutChanged;
        }

        coordinator.Dispose();
    }

    private static async Task<StickyDockConfiguration> LoadConfigurationAsync(
        IConfigurationStore<StickyDockConfiguration> store,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConfigurationException exception)
        {
            // Baseline recovery for E0: defaults are used and the invalid file is left untouched so no user data is
            // discarded. The definitive policy belongs to E8.
            logger.Error("The configuration could not be loaded; defaults are used.", exception);
            return new StickyDockConfiguration();
        }
    }
}
