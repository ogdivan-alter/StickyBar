using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// E2-CA5 on the native side: the filtered hook must leave no handle installed when it is disposed, and disposing it
/// twice must not throw.
/// </summary>
/// <remarks>
/// The test is deliberately independent of the desktop session: when <c>Shell_TrayWnd</c> is not reachable the source
/// installs no hook at all, which is also a valid final state. What is asserted is the state after disposal.
/// </remarks>
public sealed class WinEventDockingEventSourceTests
{
    [Fact]
    public void Dispose_ReleasesTheHooks()
    {
        var source = new WinEventDockingEventSource();

        source.Dispose();

        Assert.False(source.IsActive);
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var source = new WinEventDockingEventSource();
        source.Dispose();

        var exception = Record.Exception(source.Dispose);

        Assert.Null(exception);
        Assert.False(source.IsActive);
    }

    [Fact]
    public void Dispose_OfTwoInstances_ReleasesEachHookIndependently()
    {
        var first = new WinEventDockingEventSource();
        var second = new WinEventDockingEventSource();

        first.Dispose();

        Assert.False(first.IsActive);

        var exception = Record.Exception(second.Dispose);

        Assert.Null(exception);
        Assert.False(second.IsActive);
    }
}
