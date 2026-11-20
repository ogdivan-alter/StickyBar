namespace StickyBar.Core.Services;

/// <summary>
/// Presents non-fatal warnings and errors to the user without coupling Core view models to WPF dialogs (E5-CA2, E5-CA3).
/// </summary>
public interface IUserAlertService
{
    /// <summary>Displays a warning message to the user.</summary>
    void ShowWarning(string title, string message);

    /// <summary>Displays an error message to the user.</summary>
    void ShowError(string title, string message);
}
