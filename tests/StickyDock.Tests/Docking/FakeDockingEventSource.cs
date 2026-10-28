using StickyDock.Core.Docking;

namespace StickyDock.Tests.Docking;

/// <summary>
/// Double of <see cref="IDockingEventSource"/> that raises the environment changes on demand and exposes how many
/// subscribers it holds, which is how E2-CA5 is verified on the coordinator.
/// </summary>
internal sealed class FakeDockingEventSource : IDockingEventSource
{
    private EventHandler<DockingEventArgs>? _changed;

    /// <inheritdoc />
    public event EventHandler<DockingEventArgs>? Changed
    {
        add => _changed += value;
        remove => _changed -= value;
    }

    /// <summary>Gets the number of handlers currently subscribed.</summary>
    public int SubscriberCount => _changed?.GetInvocationList().Length ?? 0;

    /// <summary>Gets the number of times the source was disposed.</summary>
    public int DisposeCount { get; private set; }

    /// <summary>Gets a value indicating whether the source was disposed at least once.</summary>
    public bool IsDisposed => DisposeCount > 0;

    /// <summary>Simulates an environment change.</summary>
    /// <param name="kind">Kind of change to report.</param>
    public void Raise(DockingEventKind kind) => _changed?.Invoke(this, new DockingEventArgs(kind));

    /// <inheritdoc />
    public void Dispose() => DisposeCount++;
}
