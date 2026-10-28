using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using StickyDock.App.Docking;
using StickyDock.App.Views;
using StickyDock.Core.Configuration;
using StickyDock.Core.DependencyInjection;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Docking;
using StickyDock.Core.Services;
using StickyDock.Core.ViewModels;
using StickyDock.Interop.Theme;
using StickyDock.Interop.Win32;

namespace StickyDock.App;

/// <summary>
/// Composition root of the application: builds the services, loads the configuration and creates the shell window.
/// </summary>
/// <remarks>
/// The docking graph is composed here too, in <c>SourceInitialized</c>, because the native handle only exists once the
/// window has been created (H-E2-01). The coordinator owns the event source and is disposed when the window closes.
/// </remarks>
internal sealed class Bootstrapper : IDisposable
{
    private readonly ServiceContainer _services;
    private readonly RotatingFileLogger _logger;

    private DockPlacementCoordinator? _dockingCoordinator;
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

            var store = new JsonConfigurationStore<StickyDockConfiguration>(
                paths.ConfigurationFilePath,
                static () => new StickyDockConfiguration(),
                logger: logger);

            services.AddSingleton<IConfigurationStore<StickyDockConfiguration>>(store);

            var configuration = await LoadConfigurationAsync(store, logger, cancellationToken).ConfigureAwait(false);
            services.AddSingleton(configuration);

            if (!store.Exists)
            {
                await store.SaveAsync(configuration, cancellationToken).ConfigureAwait(false);
                logger.Information($"Initial configuration created at '{store.FilePath}'.");
            }

            services.AddSingleton(new MainViewModel(configuration, logger));

            // E1: theme service and dock state service.
            var themeService = new WindowsThemeService();
            services.AddSingleton<IWindowsThemeService>(themeService);

            var dockStateService = new DockStateService(logger);
            services.AddSingleton<IDockStateService>(dockStateService);

            var dockBarViewModel = new DockBarViewModel(logger, dockStateService, themeService);
            services.AddSingleton(dockBarViewModel);

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
    /// Shows the shell window bound to its view model and composes the docking once the native handle exists.
    /// </summary>
    public void ShowMainWindow()
    {
        var mainViewModel = _services.GetRequiredService<MainViewModel>();
        var dockBarViewModel = _services.GetRequiredService<DockBarViewModel>();
        var configuration = _services.GetRequiredService<StickyDockConfiguration>();
        var store = _services.GetRequiredService<IConfigurationStore<StickyDockConfiguration>>();

        var window = new MainWindow
        {
            DataContext = dockBarViewModel,
            MainViewModel = mainViewModel,
        };

        _dockBarViewModel = dockBarViewModel;

        window.SourceInitialized += (_, _) => AttachDocking(window, dockBarViewModel, configuration, store);
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

    private void AttachDocking(
        Window window,
        DockBarViewModel viewModel,
        StickyDockConfiguration configuration,
        IConfigurationStore<StickyDockConfiguration> store)
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
            store,
            configuration,
            _logger);

        _dockingCoordinator = coordinator;
        coordinator.PlacementChanged += OnPlacementChanged;
        viewModel.DragRequested += OnDragRequested;

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

        if (_dockBarViewModel is not null)
        {
            _dockBarViewModel.DragRequested -= OnDragRequested;
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
