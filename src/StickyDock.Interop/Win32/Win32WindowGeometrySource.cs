using System.Runtime.InteropServices;
using StickyDock.Core.Docking;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Reads the monitor bounds, the work area and the DPI of the monitor that hosts a bar window.
/// </summary>
/// <remarks>
/// All the values come from <c>GetMonitorInfoW</c> and <c>GetDpiForWindow</c>, which are already expressed in device
/// pixels of the right monitor, so no conversion from device-independent units takes place here (R-05, ADR-002).
/// </remarks>
public sealed class Win32WindowGeometrySource : IWindowGeometrySource
{
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const int DefaultDpi = 96;

    private readonly nint _windowHandle;
    private readonly IMonitorCatalog? _monitorCatalog;
    private readonly string? _targetDeviceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowGeometrySource"/> class.
    /// </summary>
    /// <param name="windowHandle">Handle of the bar window, already created.</param>
    /// <exception cref="ArgumentException">The handle is zero.</exception>
    public Win32WindowGeometrySource(nint windowHandle)
        : this(windowHandle, monitorCatalog: null, targetDeviceName: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowGeometrySource"/> class pinned to a specific monitor
    /// device name when <paramref name="targetDeviceName"/> is provided (E7-CA3).
    /// </summary>
    public Win32WindowGeometrySource(
        nint windowHandle,
        IMonitorCatalog? monitorCatalog,
        string? targetDeviceName)
    {
        if (windowHandle == nint.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        }

        _windowHandle = windowHandle;
        _monitorCatalog = monitorCatalog;
        _targetDeviceName = targetDeviceName;
    }

    /// <inheritdoc />
    public MonitorGeometry GetMonitorGeometry()
    {
        if (TryFindTargetMonitor(out var target))
        {
            return new MonitorGeometry(target.Bounds, target.WorkArea, target.Dpi);
        }

        var monitor = NativeMethods.MonitorFromWindow(_windowHandle, MonitorDefaultToNearest);
        var info = new MONITORINFO
        {
            cbSize = Marshal.SizeOf<MONITORINFO>(),
        };

        if (monitor == nint.Zero || !NativeMethods.GetMonitorInfoW(monitor, ref info))
        {
            // Degraded but safe: the bar keeps to the primary screen instead of receiving an empty work area, which
            // would shrink it to nothing.
            var primary = DisplayMetrics.GetPrimaryScreenSize();
            var fallback = new PixelRect(0, 0, primary.WidthInPixels, primary.HeightInPixels);
            return new MonitorGeometry(fallback, fallback, DefaultDpi);
        }

        return new MonitorGeometry(ToPixelRect(info.rcMonitor), ToPixelRect(info.rcWork), GetDpi());
    }

    /// <inheritdoc />
    public TaskbarPlacement GetTaskbarPlacement()
    {
        if (TryFindTargetMonitor(out var target))
        {
            return TaskbarMonitor.GetTaskbarPlacementForMonitor(target.Bounds, target.WorkArea);
        }

        return TaskbarMonitor.GetTaskbarPlacement(_windowHandle);
    }

    private bool TryFindTargetMonitor(out DisplayMonitorInfo target)
    {
        if (_monitorCatalog is not null && !string.IsNullOrWhiteSpace(_targetDeviceName))
        {
            foreach (var monitor in _monitorCatalog.GetMonitors())
            {
                if (string.Equals(monitor.DeviceName, _targetDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    target = monitor;
                    return true;
                }
            }
        }

        target = null!;
        return false;
    }

    private int GetDpi()
    {
        var dpi = NativeMethods.GetDpiForWindow(_windowHandle);

        return dpi > 0 ? (int)dpi : DefaultDpi;
    }

    private static PixelRect ToPixelRect(RECT rect)
        => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
}
