namespace StickyDock.Core.Services;

/// <summary>
/// Manages the permanent system tray icon and its context menu (§4.6-01, HU05 CA #1, E6-CA1, E6-CA2).
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Raised when the user requests opening the general settings window (left-click or Settings menu item).
    /// </summary>
    event EventHandler? SettingsRequested;

    /// <summary>
    /// Raised when the user requests exiting the application (Exit menu item).
    /// </summary>
    event EventHandler? ExitRequested;

    /// <summary>
    /// Makes the tray icon visible in the Windows notification area.
    /// </summary>
    void Show();
}
