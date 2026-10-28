using System.Windows.Interop;
using StickyDock.Core.Docking;
using StickyDock.Interop.Win32;

namespace StickyDock.App.Docking;

/// <summary>
/// Forwards the window messages of an <see cref="HwndSource"/> that the docking cares about.
/// </summary>
/// <remarks>
/// The classification itself lives in <see cref="DockingMessageClassifier"/>, which is pure and unit-tested; this class
/// only installs and removes the hook, so it contains no decision (ADR-002).
/// </remarks>
internal sealed class WpfMessageDockingEventSource : IDockingEventSource
{
    private readonly HwndSource _source;
    private readonly HwndSourceHook _hook;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfMessageDockingEventSource"/> class.
    /// </summary>
    /// <param name="source">Source of the window messages of the bar.</param>
    public WpfMessageDockingEventSource(HwndSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _hook = OnMessage;
        _source.AddHook(_hook);
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
        _source.RemoveHook(_hook);
        Changed = null;
    }

    private nint OnMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (_disposed || !DockingMessageClassifier.TryClassify(message, wParam, out var kind))
        {
            return nint.Zero;
        }

        Changed?.Invoke(this, new DockingEventArgs(kind));

        return nint.Zero;
    }
}
