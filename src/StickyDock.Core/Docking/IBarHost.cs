namespace StickyDock.Core.Docking;

/// <summary>
/// Represents an active mirrored dock bar host bound to a secondary display monitor (E7).
/// </summary>
public interface IBarHost : IDisposable
{
    /// <summary>
    /// Gets the device name of the monitor this bar is bound to.
    /// </summary>
    string DeviceName { get; }
}
