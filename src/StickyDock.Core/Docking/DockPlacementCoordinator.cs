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
/// Owns the docked state of one bar, decides its placement and persists the chosen edge (E2, E3).
/// </summary>
/// <remarks>
/// <para>
/// The coordinator is the only writer of <see cref="StickyDockConfiguration.DockEdge"/> and the only consumer of its
/// <see cref="IDockingEventSource"/>, which it disposes. It never touches Win32 and never references WPF: the reading
/// side is an <see cref="IWindowGeometrySource"/>, the writing side an <see cref="IDockWindowHost"/> (ADR-002).
/// </para>
/// <para>
/// The size of the bar is declared by an <see cref="IDockBarSizeSource"/> and never measured, so a resize imposed from
/// outside cannot contaminate the next calculation (ADR-003, D-01).
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
    private readonly IDockBarSizeSource _barSize;
    private readonly IConfigurationDocument _configuration;
    private readonly ILogger _logger;
    private readonly bool _persistEdge;

    private PixelRect? _lastApplied;
    private DockEdge? _anchor;
    private bool _started;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockPlacementCoordinator"/> class.
    /// </summary>
    /// <param name="events">Source of the environment changes. Owned: disposed with the coordinator.</param>
    /// <param name="geometry">Reader of the monitor and taskbar that surround the bar.</param>
    /// <param name="host">Window the coordinator moves. Not owned.</param>
    /// <param name="barSize">Declared footprint of the bar for the state it is in.</param>
    /// <param name="configuration">Single configuration document, where the docked edge is persisted.</param>
    /// <param name="logger">Logger of the coordinator.</param>
    public DockPlacementCoordinator(
        IDockingEventSource events,
        IWindowGeometrySource geometry,
        IDockWindowHost host,
        IDockBarSizeSource barSize,
        IConfigurationDocument configuration,
        ILogger logger,
        bool persistEdge = true)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _barSize = barSize ?? throw new ArgumentNullException(nameof(barSize));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _persistEdge = persistEdge;
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
    /// Raised when the drag of the bar begins, before the native move loop takes over. The interaction controller
    /// suppresses its automatic transitions meanwhile (ADR-003, D3).
    /// </summary>
    public event EventHandler? DragStarted;

    /// <summary>
    /// Raised after the drag completed and the snap was decided, so that the interaction controller reevaluates.
    /// </summary>
    public event EventHandler? DragCompleted;

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
        CurrentEdge = _configuration.Current.DockEdge;

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
        _anchor = null;
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
        SetFloatingAnchor(bounds);

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
    /// <see cref="DragStarted"/> is raised first because the move loop blocks until the user releases the bar.
    /// </remarks>
    public void BeginDrag()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DragStarted?.Invoke(this, EventArgs.Empty);
        _host.BeginDrag();
    }

    /// <summary>
    /// Recomputes the placement with the size the bar occupies for the state it is in now (ADR-003, D-09).
    /// </summary>
    /// <remarks>
    /// Called by the composition root when the dock state changes. It never resets the last applied rectangle, so
    /// recalculating twice in a row moves nothing (rule R4).
    /// </remarks>
    public void NotifyBarSizeChanged()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_started)
        {
            // The placement has not been resolved yet; Start will apply the size that is current by then.
            return;
        }

        ResolvePlacement();
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
        DragStarted = null;
        DragCompleted = null;

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
    /// Keeps a floating bar on screen, resized to the declared footprint, without docking it.
    /// </summary>
    /// <remarks>
    /// A floating bar has no edge that anchors it, so it grows away from the nearest one (ADR-003, §6.3). The anchor is
    /// read from the current rectangle, before the size changes, so the growth always goes inward.
    /// </remarks>
    private void ResolveFloating(MonitorGeometry monitor)
    {
        var bounds = _host.GetBounds();

        if (bounds.IsEmpty)
        {
            return;
        }

        _anchor ??= DockPlacementCalculator.GetNearestEdge(bounds, monitor.WorkArea);

        var resized = DockPlacementCalculator.GetFloatingBounds(
            bounds,
            _anchor.Value,
            monitor,
            _barSize.GetCurrentSize(anchor: null));
        var clamped = DockPlacementCalculator.ClampIntoWorkArea(resized, monitor.WorkArea);

        if (clamped != resized)
        {
            SetFloatingAnchor(clamped);
        }

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
        _anchor = null;
        var edge = DockPlacementCalculator.ChooseDockEdge(current, taskbar);
        var bounds = DockPlacementCalculator.GetDockedBounds(edge, monitor, _barSize.GetCurrentSize(edge));

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
        try
        {
            // The window is wherever the user dropped it, so that rectangle replaces the last one the coordinator
            // applied. Comparing against it keeps the application idempotent: a click with no movement moves nothing.
            var dragged = _host.GetBounds();
            _lastApplied = dragged;

            // An unreadable rectangle (a window with no size) is at distance zero from every edge, so the calculator
            // would snap the bar to the top for the wrong reason. It is treated as a release outside the band instead.
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
        finally
        {
            DragCompleted?.Invoke(this, EventArgs.Empty);
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
        SetFloatingAnchor(bounds);

        if (previousEdge is null)
        {
            return false;
        }

        PersistEdge(null);
        PlacementChanged?.Invoke(this, new DockPlacementChangedEventArgs(null, bounds));
        return true;
    }

    private void SetFloatingAnchor(PixelRect bounds)
        => _anchor = DockPlacementCalculator.GetNearestEdge(bounds, _geometry.GetMonitorGeometry().WorkArea);

    /// <summary>
    /// Moves the window and records the rectangle as applied.
    /// </summary>
    /// <remarks>
    /// The rectangle is recorded before the window is moved (ADR-003, §6.6, rule R2), so any environment message or
    /// reentry that arrives while <see cref="IDockWindowHost.ApplyBounds"/> is running already sees it as applied and
    /// does not move the window a second time.
    /// </remarks>
    private void Apply(PixelRect bounds)
    {
        _lastApplied = bounds;
        _host.ApplyBounds(bounds);
    }

    private void PersistEdge(DockEdge? edge)
    {
        if (!_persistEdge || _configuration.Current.DockEdge == edge)
        {
            return;
        }

        _configuration.Update(document => document with { DockEdge = edge });
    }

    private static string DescribeEdge(DockEdge? edge) => edge?.ToString() ?? "floating";

    private static string Describe(PixelRect bounds)
        => $"({bounds.X}, {bounds.Y}, {bounds.Width}x{bounds.Height})";
}
