namespace StickyDock.Core.Docking;

/// <summary>
/// What changed in the environment of the bar (ADR-002).
/// </summary>
public enum DockingEventKind
{
    /// <summary>The work area of the monitor changed, typically because the taskbar moved or was resized.</summary>
    WorkAreaChanged = 0,

    /// <summary>The resolution, the display mode or the scale of the window changed.</summary>
    DisplayChanged = 1,

    /// <summary>The taskbar moved or changed its visibility without a work area notification.</summary>
    TaskbarChanged = 2,

    /// <summary>The user finished dragging the bar, so the snap target must be decided.</summary>
    WindowDragCompleted = 3,
}

/// <summary>
/// Arguments of <see cref="IDockingEventSource.Changed"/>.
/// </summary>
/// <param name="Kind">Kind of change that was observed.</param>
public sealed record DockingEventArgs(DockingEventKind Kind);

/// <summary>
/// Source of the environment changes the docking reacts to.
/// </summary>
/// <remarks>
/// Implementations are event-driven by contract: no timer and no polling, so the cost while the bar is idle stays at
/// zero (RNF-02). Disposal must release every native resource held by the source.
/// </remarks>
public interface IDockingEventSource : IDisposable
{
    /// <summary>
    /// Raised when the environment of the bar changed.
    /// </summary>
    event EventHandler<DockingEventArgs>? Changed;
}

/// <summary>
/// Fans several sources in and owns their lifetime.
/// </summary>
public sealed class CompositeDockingEventSource : IDockingEventSource
{
    private readonly IDockingEventSource[] _sources;
    private readonly EventHandler<DockingEventArgs> _forwarder;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDockingEventSource"/> class.
    /// </summary>
    /// <param name="sources">Sources to compose; they are unsubscribed and disposed with this instance.</param>
    public CompositeDockingEventSource(params IDockingEventSource[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        _sources = [.. sources];

        foreach (var source in _sources)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(sources));
        }

        _forwarder = (_, args) => Changed?.Invoke(this, args);

        foreach (var source in _sources)
        {
            source.Changed += _forwarder;
        }
    }

    /// <inheritdoc />
    public event EventHandler<DockingEventArgs>? Changed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var source in _sources)
        {
            source.Changed -= _forwarder;
            source.Dispose();
        }

        Changed = null;
    }
}
