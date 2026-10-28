using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Docking;

/// <summary>
/// Arguments of <see cref="DockPlacementCoordinator.PlacementChanged"/>.
/// </summary>
/// <param name="Edge">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
/// <param name="Bounds">Rectangle the bar occupies, in device pixels.</param>
public sealed record DockPlacementChangedEventArgs(DockEdge? Edge, PixelRect Bounds);

/// <summary>
/// Owns the docked state of one bar, decides its placement and persists the chosen edge (E2).
/// </summary>
/// <remarks>
/// <para>
/// The coordinator is the only writer of <see cref="StickyDockConfiguration.DockEdge"/> and the only consumer of its
/// <see cref="IDockingEventSource"/>, which it disposes. It never touches Win32 and never references WPF: the reading
/// side is an <see cref="IWindowGeometrySource"/>, the writing side an <see cref="IDockWindowHost"/> (ADR-002).
/// </para>
/// <para>
/// Cost while idle is zero: every reaction is driven by an event of the source, and there is no timer.
/// </para>
/// </remarks>
public sealed class DockPlacementCoordinator : IDisposable
{
    private readonly IDockingEventSource _events;
    private readonly IWindowGeometrySource _geometry;
    private readonly IDockWindowHost _host;
    private readonly IConfigurationStore<StickyDockConfiguration> _configurationStore;
    private readonly ILogger _logger;

    private StickyDockConfiguration _configuration;
    private PixelRect? _lastApplied;
    private bool _started;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockPlacementCoordinator"/> class.
    /// </summary>
    /// <param name="events">Source of the environment changes. Owned: disposed with the coordinator.</param>
    /// <param name="geometry">Reader of the monitor and taskbar that surround the bar.</param>
    /// <param name="host">Window the coordinator moves. Not owned.</param>
    /// <param name="configurationStore">Store where the docked edge is persisted.</param>
    /// <param name="configuration">Configuration loaded at startup, with the persisted edge.</param>
    /// <param name="logger">Logger of the coordinator.</param>
    public DockPlacementCoordinator(
        IDockingEventSource events,
        IWindowGeometrySource geometry,
        IDockWindowHost host,
        IConfigurationStore<StickyDockConfiguration> configurationStore,
        StickyDockConfiguration configuration,
        ILogger logger)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the edge the bar is docked to, or <see langword="null"/> while it floats.
    /// </summary>
    public DockEdge? CurrentEdge { get; private set; }

    /// <summary>
    /// Raised after the placement changed, so that the view model can mirror the docked state.
    /// </summary>
    public event EventHandler<DockPlacementChangedEventArgs>? PlacementChanged;

    /// <summary>
    /// Subscribes to the environment events and applies the persisted edge.
    /// </summary>
    /// <remarks>Called once, by the composition root, when the window handle already exists.</remarks>
    /// <exception cref="InvalidOperationException">The coordinator was already started.</exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new InvalidOperationException("The docking coordinator was already started; Start must be called once.");
        }

        _started = true;
        CurrentEdge = _configuration.DockEdge;

        _events.Changed += OnEnvironmentChanged;

        ResolvePlacement();

        _logger.Information($"Docking started; edge = {DescribeEdge(CurrentEdge)}.");
    }

    /// <summary>
    /// Docks the bar to an explicit edge.
    /// </summary>
    /// <param name="edge">Edge requested by the user.</param>
    /// <remarks>Ignored when the taskbar occupies that edge (E2-CA4).</remarks>
    public void DockTo(DockEdge edge)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var taskbar = _geometry.GetTaskbarPlacement();

        if (!DockPlacementCalculator.IsEdgeAvailable(edge, taskbar))
        {
            _logger.Warning($"Dock edge '{edge}' is occupied by the taskbar on this monitor; the request was ignored.");
            return;
        }

        // Whatever the window shows is what it occupies now: comparing against it keeps the move idempotent.
        _lastApplied = _host.GetBounds();
        ApplyDocked(edge, _geometry.GetMonitorGeometry(), taskbar);
    }

    /// <summary>
    /// Leaves the bar floating where it is, without moving it.
    /// </summary>
    public void Undock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var previousEdge = CurrentEdge;
        CurrentEdge = null;

        var bounds = _host.GetBounds();
        _lastApplied = bounds;

        if (previousEdge is null)
        {
            return;
        }

        PersistEdge(null);
        PlacementChanged?.Invoke(this, new DockPlacementChangedEventArgs(null, bounds));
        _logger.Information("Docking released; the bar floats where it is.");
    }

    /// <summary>
    /// Starts a drag of the bar with the native move loop of the system.
    /// </summary>
    /// <remarks>
    /// The view model raises a command that ends here, so the gesture crosses no geometry and no Win32 code (ADR-002).
    /// </remarks>
    public void BeginDrag()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _host.BeginDrag();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lastApplied = null;
        PlacementChanged = null;

        _events.Changed -= OnEnvironmentChanged;
        _events.Dispose();
    }

    private void OnEnvironmentChanged(object? sender, DockingEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.Kind == DockingEventKind.WindowDragCompleted)
        {
            HandleDragCompleted();
            return;
        }

        ResolvePlacement();
    }

    /// <summary>
    /// Recomputes the placement with the environment as it is now (work area, display or taskbar changed).
    /// </summary>
    private void ResolvePlacement()
    {
        var monitor = _geometry.GetMonitorGeometry();

        if (CurrentEdge is not { } current)
        {
            ResolveFloating(monitor);
            return;
        }

        ApplyDocked(current, monitor, _geometry.GetTaskbarPlacement());
    }

    /// <summary>
    /// Keeps a floating bar on screen without docking it.
    /// </summary>
    private void ResolveFloating(MonitorGeometry monitor)
    {
        var bounds = _host.GetBounds();

        if (bounds.IsEmpty)
        {
            return;
        }

        var clamped = DockPlacementCalculator.ClampIntoWorkArea(bounds, monitor.WorkArea);

        if (clamped == bounds)
        {
            _lastApplied = bounds;
            return;
        }

        Apply(clamped);
        PlacementChanged?.Invoke(this, new DockPlacementChangedEventArgs(null, clamped));
        _logger.Information($"Floating bar brought back into the work area at {Describe(clamped)}.");
    }

    /// <summary>
    /// Docks to <paramref name="current"/> when it is still legal, or to the fallback edge the calculator chooses.
    /// </summary>
    private void ApplyDocked(DockEdge current, MonitorGeometry monitor, TaskbarPlacement taskbar)
    {
        var edge = DockPlacementCalculator.ChooseDockEdge(current, taskbar);
        var bounds = DockPlacementCalculator.GetDockedBounds(edge, monitor, _host.GetBarSize());

        var edgeChanged = CurrentEdge != edge;
        CurrentEdge = edge;

        var moved = _lastApplied != bounds;

        if (moved)
        {
            Apply(bounds);
        }

        if (edgeChanged)
        {
            PersistEdge(edge);
        }

        if (!moved && !edgeChanged)
        {
            return;
        }

        PlacementChanged?.Invoke(this, new DockPlacementChangedEventArgs(edge, bounds));

        if (edgeChanged)
        {
            _logger.Information($"Bar docked to '{edge}' at {Describe(bounds)}.");
        }
    }

    /// <summary>
    /// Decides the snap when the user released the bar after dragging it.
    /// </summary>
    private void HandleDragCompleted()
    {
        // The window is wherever the user dropped it, so that rectangle replaces the last one the coordinator applied.
        // Comparing against it keeps the application idempotent: a click with no movement moves nothing.
        var dragged = _host.GetBounds();
        _lastApplied = dragged;

        // An unreadable rectangle (a window with no size) is at distance zero from every edge, so the calculator would
        // snap the bar to the top for the wrong reason. It is treated as a release outside the snap band instead.
        if (dragged.IsEmpty)
        {
            ReleaseToFloating(dragged);
            _logger.Warning("The bar reported an empty rectangle at the end of the drag; it stays floating.");
            return;
        }

        var monitor = _geometry.GetMonitorGeometry();
        var taskbar = _geometry.GetTaskbarPlacement();
        var target = DockPlacementCalculator.ComputeSnapTarget(dragged, monitor, taskbar);

        if (target is { } edge)
        {
            ApplyDocked(edge, monitor, taskbar);
            return;
        }

        if (ReleaseToFloating(dragged))
        {
            _logger.Information($"Bar released outside the snap band; it floats at {Describe(dragged)}.");
        }
    }

    /// <summary>
    /// Leaves the bar floating where it is, clearing and persisting the docked edge when there was one.
    /// </summary>
    /// <param name="bounds">Rectangle the bar occupies, in device pixels.</param>
    /// <returns><see langword="true"/> when the bar was docked and therefore changed state.</returns>
    private bool ReleaseToFloating(PixelRect bounds)
    {
        var previousEdge = CurrentEdge;
        CurrentEdge = null;

        if (previousEdge is null)
        {
            return false;
        }

        PersistEdge(null);
        PlacementChanged?.Invoke(this, new DockPlacementChangedEventArgs(null, bounds));
        return true;
    }

    private void Apply(PixelRect bounds)
    {
        _host.ApplyBounds(bounds);
        _lastApplied = bounds;
    }

    private void PersistEdge(DockEdge? edge)
    {
        if (_configuration.DockEdge == edge)
        {
            return;
        }

        _configuration = _configuration with { DockEdge = edge };
        _ = SaveAsync(_configuration);
    }

    private async Task SaveAsync(StickyDockConfiguration document)
    {
        try
        {
            await _configurationStore.SaveAsync(document).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The docking keeps working; only the preference is lost for the next run.
            _logger.Error("The dock edge could not be persisted.", exception);
        }
    }

    private static string DescribeEdge(DockEdge? edge) => edge?.ToString() ?? "floating";

    private static string Describe(PixelRect bounds)
        => $"({bounds.X}, {bounds.Y}, {bounds.Width}x{bounds.Height})";
}
