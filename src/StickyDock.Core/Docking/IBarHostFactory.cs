namespace StickyDock.Core.Docking;

/// <summary>
/// Creates mirrored dock bar hosts for secondary display monitors (E7).
/// </summary>
public interface IBarHostFactory
{
    /// <summary>
    /// Creates and starts a mirrored dock bar bound to <paramref name="monitor"/>.
    /// </summary>
    /// <param name="monitor">Secondary monitor that hosts the replica bar.</param>
    /// <returns>The active bar host, owned by the caller until disposed.</returns>
    IBarHost CreateReplica(DisplayMonitorInfo monitor);
}
