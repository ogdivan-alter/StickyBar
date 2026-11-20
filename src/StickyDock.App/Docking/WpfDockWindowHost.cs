using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using StickyDock.Core.Docking;
using StickyDock.Interop.Win32;

namespace StickyDock.App.Docking;

/// <summary>
/// Adapter between the docking coordinator and the WPF window. Contains threading and cache plumbing only: no geometry
/// and no decision lives here (ADR-002).
/// </summary>
internal sealed class WpfDockWindowHost : IDockWindowHost
{
    private const double DipTolerance = 0.5;

    private readonly Window _window;
    private readonly Action? _onNativeMoveStarting;
    private readonly Action? _onNativeMoveEnded;
    private bool _isMirroring;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfDockWindowHost"/> class.
    /// </summary>
    /// <param name="window">Window the coordinator drives.</param>
    /// <param name="onNativeMoveStarting">Optional callback invoked right before entering the native move loop.</param>
    /// <param name="onNativeMoveEnded">Optional callback invoked after the native move loop exits.</param>
    public WpfDockWindowHost(
        Window window,
        Action? onNativeMoveStarting = null,
        Action? onNativeMoveEnded = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _onNativeMoveStarting = onNativeMoveStarting;
        _onNativeMoveEnded = onNativeMoveEnded;
    }

    private nint Handle => new WindowInteropHelper(_window).Handle;

    /// <inheritdoc />
    public PixelRect GetBounds() => WindowPlacement.GetBounds(Handle);

    /// <inheritdoc />
    public void ApplyBounds(PixelRect bounds)
    {
        Dispatch(() =>
        {
            var handle = Handle;

            if (handle == nint.Zero)
            {
                return;
            }

            // The native rectangle is authoritative; the WPF properties are only kept in sync so that WPF does not move
            // the window back to a stale position on the next show or DPI change.
            WindowPlacement.MoveResize(handle, bounds);
            MirrorPositionIntoWpf(bounds);
        });
    }

    /// <inheritdoc />
    public void BeginDrag()
    {
        Dispatch(() =>
        {
            _onNativeMoveStarting?.Invoke();
            try
            {
                var handle = Handle;

                if (handle != nint.Zero)
                {
                    WindowPlacement.BeginNativeMove(handle);
                }
            }
            finally
            {
                _onNativeMoveEnded?.Invoke();
            }
        });
    }

    private void MirrorPositionIntoWpf(PixelRect bounds)
    {
        if (_isMirroring)
        {
            return;
        }

        _isMirroring = true;

        try
        {
            // Best effort: the conversion from device pixels to device-independent units is only exact on the primary
            // monitor when the scales differ (R-05). The native rectangle remains the source of truth.
            var dpi = VisualTreeHelper.GetDpi(_window);
            var left = bounds.X / dpi.DpiScaleX;
            var top = bounds.Y / dpi.DpiScaleY;

            if (Math.Abs(_window.Left - left) > DipTolerance)
            {
                _window.Left = left;
            }

            if (Math.Abs(_window.Top - top) > DipTolerance)
            {
                _window.Top = top;
            }
        }
        finally
        {
            _isMirroring = false;
        }
    }

    private void Dispatch(Action action)
    {
        if (_window.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _window.Dispatcher.Invoke(action);
    }
}
