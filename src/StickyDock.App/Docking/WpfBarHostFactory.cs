using System.Windows;
using System.Windows.Interop;
using StickyDock.App.Services;
using StickyDock.App.Timing;
using StickyDock.App.Views;
using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Services;
using StickyDock.Core.Shortcuts;
using StickyDock.Core.ViewModels;
using StickyDock.Interop.Theme;
using StickyDock.Interop.Win32;

namespace StickyDock.App.Docking;

/// <summary>
/// Creates mirrored <see cref="MainWindow"/> replica hosts bound to secondary monitors (E7-CA1..E7-CA5).
/// </summary>
internal sealed class WpfBarHostFactory : IBarHostFactory
{
    private readonly IConfigurationDocument _configuration;
    private readonly IMonitorCatalog _monitorCatalog;
    private readonly MainViewModel _mainViewModel;
    private readonly IWindowsThemeService _themeService;
    private readonly IShellLinkReader _shellLinkReader;
    private readonly IShortcutLauncher _shortcutLauncher;
    private readonly IUserAlertService _userAlertService;
    private readonly IRibbonDeletionConfirmer _deletionConfirmer;
    private readonly ILogger _logger;

    public WpfBarHostFactory(
        IConfigurationDocument configuration,
        IMonitorCatalog monitorCatalog,
        MainViewModel mainViewModel,
        IWindowsThemeService themeService,
        IShellLinkReader shellLinkReader,
        IShortcutLauncher shortcutLauncher,
        IUserAlertService userAlertService,
        IRibbonDeletionConfirmer deletionConfirmer,
        ILogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _monitorCatalog = monitorCatalog ?? throw new ArgumentNullException(nameof(monitorCatalog));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _shellLinkReader = shellLinkReader ?? throw new ArgumentNullException(nameof(shellLinkReader));
        _shortcutLauncher = shortcutLauncher ?? throw new ArgumentNullException(nameof(shortcutLauncher));
        _userAlertService = userAlertService ?? throw new ArgumentNullException(nameof(userAlertService));
        _deletionConfirmer = deletionConfirmer ?? throw new ArgumentNullException(nameof(deletionConfirmer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IBarHost CreateReplica(DisplayMonitorInfo monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var controller = new DockInteractionController(new DispatcherTimerScheduler(), _configuration, _logger);
        var viewModel = new DockBarViewModel(
            _logger,
            controller,
            controller,
            _themeService,
            _configuration,
            _deletionConfirmer,
            _shellLinkReader,
            _shortcutLauncher,
            _userAlertService);
        var sizeSource = new DockBarSizeSource(controller, DockBarMetrics.Default, viewModel);

        var window = new MainWindow
        {
            DataContext = viewModel,
            MainViewModel = _mainViewModel,
        };

        var host = new ReplicaBarHost(
            monitor,
            window,
            controller,
            viewModel,
            sizeSource,
            _monitorCatalog,
            _configuration,
            _logger);

        host.Show();
        return host;
    }

    private sealed class ReplicaBarHost : IBarHost
    {
        private readonly DisplayMonitorInfo _monitor;
        private readonly DockInteractionController _controller;
        private readonly DockBarViewModel _viewModel;
        private readonly IDockBarSizeSource _sizeSource;
        private readonly IMonitorCatalog _monitorCatalog;
        private readonly IConfigurationDocument _configuration;
        private readonly ILogger _logger;

        private MainWindow? _window;
        private DockPlacementCoordinator? _coordinator;
        private bool _disposed;

        public ReplicaBarHost(
            DisplayMonitorInfo monitor,
            MainWindow window,
            DockInteractionController controller,
            DockBarViewModel viewModel,
            IDockBarSizeSource sizeSource,
            IMonitorCatalog monitorCatalog,
            IConfigurationDocument configuration,
            ILogger logger)
        {
            _monitor = monitor;
            _window = window;
            _controller = controller;
            _viewModel = viewModel;
            _sizeSource = sizeSource;
            _monitorCatalog = monitorCatalog;
            _configuration = configuration;
            _logger = logger;

            _window.SourceInitialized += OnSourceInitialized;
            _window.Closed += OnWindowClosed;
        }

        public string DeviceName => _monitor.DeviceName;

        public void Show() => _window?.Show();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DetachCoordinator();

            if (_window is not null)
            {
                var window = _window;
                _window = null;
                window.SourceInitialized -= OnSourceInitialized;
                window.Closed -= OnWindowClosed;
                window.Close();
            }

            _viewModel.Dispose();
            _controller.Dispose();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            if (_disposed || _window is null)
            {
                return;
            }

            var handle = new WindowInteropHelper(_window).Handle;
            var messageSource = handle != nint.Zero ? HwndSource.FromHwnd(handle) : null;

            if (handle == nint.Zero || messageSource is null)
            {
                _logger.Error($"Replica docking skipped for '{_monitor.DeviceName}': missing window handle.");
                return;
            }

            // Place the window inside its target monitor's work area before Start() resolves placement (E7-CA3).
            var initialWidth = Math.Min((int)DockBarMetrics.Default.CollapsedLength, Math.Max(1, _monitor.WorkArea.Width));
            var initialHeight = Math.Min((int)DockBarMetrics.Default.CollapsedThickness, Math.Max(1, _monitor.WorkArea.Height));
            WindowPlacement.MoveResize(
                handle,
                new PixelRect(_monitor.WorkArea.X, _monitor.WorkArea.Y, initialWidth, initialHeight));

            var events = new CompositeDockingEventSource(
                new WpfMessageDockingEventSource(messageSource),
                new WinEventDockingEventSource());

            var coordinator = new DockPlacementCoordinator(
                events,
                new Win32WindowGeometrySource(handle, _monitorCatalog, _monitor.DeviceName),
                new WpfDockWindowHost(_window),
                _sizeSource,
                _configuration,
                _logger,
                persistEdge: false);

            _coordinator = coordinator;
            coordinator.PlacementChanged += OnPlacementChanged;
            coordinator.DragStarted += OnDragStarted;
            coordinator.DragCompleted += OnDragCompleted;
            _controller.StateChanged += OnDockStateChanged;
            _viewModel.DragRequested += OnDragRequested;
            _viewModel.LayoutChanged += OnLayoutChanged;

            try
            {
                coordinator.Start();
            }
            catch (Exception exception)
            {
                _logger.Critical($"Replica docking failed for '{_monitor.DeviceName}'.", exception);
                DetachCoordinator();
            }
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _window = null;
            Dispose();
        }

        private void OnPlacementChanged(object? sender, DockPlacementChangedEventArgs e)
            => _viewModel.ApplyPlacement(e.Edge);

        private void OnDockStateChanged(object? sender, DockStateChangedEventArgs e)
            => _coordinator?.NotifyBarSizeChanged();

        private void OnLayoutChanged(object? sender, EventArgs e)
            => _coordinator?.NotifyBarSizeChanged();

        private void OnDragStarted(object? sender, EventArgs e) => _controller.BeginDrag();

        private void OnDragCompleted(object? sender, EventArgs e) => _controller.EndDrag();

        private void OnDragRequested(object? sender, EventArgs e) => _coordinator?.BeginDrag();

        private void DetachCoordinator()
        {
            var coordinator = _coordinator;

            if (coordinator is null)
            {
                return;
            }

            _coordinator = null;
            coordinator.PlacementChanged -= OnPlacementChanged;
            coordinator.DragStarted -= OnDragStarted;
            coordinator.DragCompleted -= OnDragCompleted;
            _controller.StateChanged -= OnDockStateChanged;
            _viewModel.DragRequested -= OnDragRequested;
            _viewModel.LayoutChanged -= OnLayoutChanged;
            coordinator.Dispose();
        }
    }
}
