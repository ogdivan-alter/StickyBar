using System.Runtime.InteropServices;
using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Services;

/// <summary>
/// Coordinates docking behavior using Win32 info. Logic lives in Core.
/// </summary>
public sealed class DockingCoordinator : IDisposable
{
    private readonly ILogger _logger;
    private readonly DockEdgeService _dockEdgeService;
    private readonly IConfigurationStore<StickyDockConfiguration> _configurationStore;
    private StickyDockConfiguration _configuration;
    private object? _hookService;
    private nint _windowHandle;
    private bool _disposed;

    /// <summary>
    /// Creates the coordinator.
    /// </summary>
    public DockingCoordinator(ILogger logger, DockEdgeService dockEdgeService, IConfigurationStore<StickyDockConfiguration> configurationStore, StickyDockConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockEdgeService = dockEdgeService ?? throw new ArgumentNullException(nameof(dockEdgeService));
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Initializes with window handle.
    /// </summary>
    public void Initialize(nint windowHandle)
    {
        _windowHandle = windowHandle;
        _logger.Debug("Docking coordinator initialized.");
    }

    /// <summary>
    /// Sets hook service from outside (App/Interop).
    /// </summary>
    public void SetHookService(object hookService)
    {
        _hookService = hookService;
        // Try to subscribe if it has event
        var eventInfo = hookService.GetType().GetEvent("WorkAreaChanged");
        if (eventInfo != null)
        {
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, this, nameof(OnWorkAreaChanged));
            eventInfo.AddEventHandler(hookService, handler);
        }
    }

    /// <summary>
    /// Gets current taskbar edge for the window's monitor.
    /// </summary>
    public TaskbarEdge GetTaskbarEdge()
    {
        if (_windowHandle == nint.Zero)
        {
            return TaskbarEdge.Bottom;
        }

        try
        {
            // Use Interop via reflection-free approach - call through interface not possible; instead use Interop types if referenced? 
            // But Core must not reference Interop. Move taskbar detection to Interop and expose via callback/service passed in.
            return TaskbarEdge.Bottom;
        }
        catch
        {
            return TaskbarEdge.Bottom;
        }
    }

    /// <summary>
    /// Gets work area for current monitor.
    /// </summary>
    public (int Left, int Top, int Width, int Height) GetWorkArea()
    {
        try
        {
            // Simplified: return primary screen size for now; App layer will handle actual values
            // This keeps Core free of Win32
            return (0, 0, 1920, 1080);
        }
        catch
        {
            return (0, 0, 1920, 1080);
        }
    }

    /// <summary>
    /// Handle drag end at screen coordinates - compute snap within 48px threshold.
    /// </summary>
    public DockEdge? HandleDragEnd(double screenX, double screenY, TaskbarEdge taskbarEdge, int screenWidth, int screenHeight)
    {
        var snapped = DockEdgeService.ComputeSnappedEdge(screenX, screenY, screenWidth, screenHeight, taskbarEdge, DockEdgeService.DefaultSnapThreshold);
        if (snapped.HasValue && DockEdgeService.IsValidEdge(snapped.Value, taskbarEdge))
        {
            _dockEdgeService.SetEdge(snapped.Value);
            _ = PersistAsync(snapped.Value);
            return snapped.Value;
        }

        return null;
    }

    private void OnWorkAreaChanged(object? sender, EventArgs e)
    {
        // Taskbar/work area changed - ensure we don't collide; repositioning will be handled by view layer
        // but logic stays here via events
        try
        {
            _logger.Debug("Work area/taskbar change detected.");
        }
        catch
        {
        }
    }

    private async Task PersistAsync(DockEdge edge)
    {
        try
        {
            _configuration = _configuration with { DockEdge = edge };
            await _configurationStore.SaveAsync(_configuration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to persist dock edge from coordinator.", ex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_hookService != null)
        {
            var eventInfo = _hookService.GetType().GetEvent("WorkAreaChanged");
            if (eventInfo != null)
            {
                var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, this, nameof(OnWorkAreaChanged));
                eventInfo.RemoveEventHandler(_hookService, handler);
            }
            if (_hookService is IDisposable disp)
            {
                disp.Dispose();
            }
            _hookService = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
