using System.Windows;
using StickyDock.Core.Services;

namespace StickyDock.App.Services;

/// <summary>
/// WPF message-box implementation of <see cref="IUserAlertService"/> (E5-CA2, E5-CA3).
/// </summary>
internal sealed class WpfUserAlertService : IUserAlertService
{
    /// <inheritdoc />
    public void ShowWarning(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    /// <inheritdoc />
    public void ShowError(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
