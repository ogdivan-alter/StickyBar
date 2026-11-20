using System.Runtime.InteropServices;
using StickyBar.Core.Docking;

namespace StickyBar.Interop.Win32;

/// <summary>
/// Enumerates attached display monitors using pure Win32 APIs (<c>EnumDisplayMonitors</c>, <c>GetMonitorInfoW</c> and
/// <c>GetDpiForMonitor</c>) without loading WinForms (RF05, E7, R-05).
/// </summary>
public sealed class Win32MonitorCatalog : IMonitorCatalog
{
    private const uint MonitorInfoPrimaryFlag = 0x00000001;
    private const int MdtEffectiveDpi = 0;
    private const int DefaultDpi = 96;
    private const int DeviceNameBufferLength = 32;

    /// <inheritdoc />
    public IReadOnlyList<DisplayMonitorInfo> GetMonitors()
    {
        var monitors = new List<DisplayMonitorInfo>();
        var handle = GCHandle.Alloc(monitors);

        try
        {
            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, OnMonitorEnumerated, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        return monitors;
    }

    private static unsafe bool OnMonitorEnumerated(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData)
    {
        if (GCHandle.FromIntPtr(dwData).Target is not List<DisplayMonitorInfo> monitors)
        {
            return false;
        }

        var info = new MONITORINFOEXW
        {
            cbSize = sizeof(MONITORINFOEXW),
        };

        if (!NativeMethods.GetMonitorInfoExW(hMonitor, ref info))
        {
            return true;
        }

        var deviceName = ExtractDeviceName(info.szDevice, DeviceNameBufferLength);
        var isPrimary = (info.dwFlags & MonitorInfoPrimaryFlag) != 0;
        var bounds = ToPixelRect(info.rcMonitor);
        var workArea = ToPixelRect(info.rcWork);
        var dpi = QueryMonitorDpi(hMonitor);

        monitors.Add(new DisplayMonitorInfo(deviceName, isPrimary, bounds, workArea, dpi));
        return true;
    }

    private static int QueryMonitorDpi(nint hMonitor)
    {
        try
        {
            var hr = NativeMethods.GetDpiForMonitor(hMonitor, MdtEffectiveDpi, out var dpiX, out _);
            return hr == 0 && dpiX > 0 ? (int)dpiX : DefaultDpi;
        }
        catch (DllNotFoundException)
        {
            return DefaultDpi;
        }
        catch (EntryPointNotFoundException)
        {
            return DefaultDpi;
        }
    }

    private static unsafe string ExtractDeviceName(char* buffer, int maxLength)
    {
        var length = 0;

        while (length < maxLength && buffer[length] != '\0')
        {
            length++;
        }

        return new string(buffer, 0, length);
    }

    private static PixelRect ToPixelRect(RECT rect)
        => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
}
