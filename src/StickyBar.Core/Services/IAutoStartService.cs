namespace StickyBar.Core.Services;

/// <summary>
/// Manages the per-user Windows startup registration for the application (§4.6-03, E6-CA4).
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Returns <see langword="true"/> when the application is currently registered to start with Windows.
    /// </summary>
    bool IsEnabled();

    /// <summary>
    /// Enables or disables automatic startup with Windows.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to register startup; <see langword="false"/> to remove it.</param>
    void SetEnabled(bool enabled);
}
