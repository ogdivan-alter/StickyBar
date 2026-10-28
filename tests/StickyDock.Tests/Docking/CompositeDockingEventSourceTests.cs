using StickyDock.Core.Docking;

namespace StickyDock.Tests.Docking;

/// <summary>
/// E2-CA5 at the composition level: the composite must release every child source, so the native hook installed by
/// <c>WinEventDockingEventSource</c> is released even when the message route is also present (ADR-002).
/// </summary>
public sealed class CompositeDockingEventSourceTests
{
    [Fact]
    public void Changed_IsRaisedForEveryChildSource()
    {
        var messages = new FakeDockingEventSource();
        var hook = new FakeDockingEventSource();
        using var composite = new CompositeDockingEventSource(messages, hook);
        var received = new List<DockingEventKind>();
        composite.Changed += (_, args) => received.Add(args.Kind);

        messages.Raise(DockingEventKind.WorkAreaChanged);
        hook.Raise(DockingEventKind.TaskbarChanged);

        Assert.Equal(new[] { DockingEventKind.WorkAreaChanged, DockingEventKind.TaskbarChanged }, received);
    }

    [Fact]
    public void Dispose_UnsubscribesAndDisposesEveryChildSource()
    {
        var messages = new FakeDockingEventSource();
        var hook = new FakeDockingEventSource();
        var composite = new CompositeDockingEventSource(messages, hook);
        Assert.Equal(1, messages.SubscriberCount);
        Assert.Equal(1, hook.SubscriberCount);

        composite.Dispose();

        Assert.Equal(0, messages.SubscriberCount);
        Assert.Equal(0, hook.SubscriberCount);
        Assert.True(messages.IsDisposed);
        Assert.True(hook.IsDisposed);
    }

    [Fact]
    public void Dispose_CalledTwice_DisposesEachChildOnce()
    {
        var messages = new FakeDockingEventSource();
        var hook = new FakeDockingEventSource();
        var composite = new CompositeDockingEventSource(messages, hook);

        composite.Dispose();
        composite.Dispose();

        Assert.Equal(1, messages.DisposeCount);
        Assert.Equal(1, hook.DisposeCount);
    }

    [Fact]
    public void Changed_AfterDispose_DoesNotReachTheSubscriber()
    {
        var messages = new FakeDockingEventSource();
        var composite = new CompositeDockingEventSource(messages);
        var raised = 0;
        composite.Changed += (_, _) => raised++;

        composite.Dispose();
        messages.Raise(DockingEventKind.WorkAreaChanged);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Constructor_WithANullArray_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeDockingEventSource(null!));
    }

    [Fact]
    public void Constructor_WithANullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeDockingEventSource(new FakeDockingEventSource(), null!));
    }
}
