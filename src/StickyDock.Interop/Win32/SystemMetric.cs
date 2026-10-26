namespace StickyDock.Interop.Win32;

/// <summary>
/// Values of the <c>nIndex</c> parameter of <c>GetSystemMetrics</c> that this assembly uses.
/// </summary>
internal enum SystemMetric
{
    /// <summary>Width of the primary monitor, in pixels (<c>SM_CXSCREEN</c>).</summary>
    PrimaryScreenWidth = 0,

    /// <summary>Height of the primary monitor, in pixels (<c>SM_CYSCREEN</c>).</summary>
    PrimaryScreenHeight = 1,
}
