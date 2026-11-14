using System.Globalization;
using System.Windows;
using StickyDock.Core.Services;

namespace StickyDock.App.Services;

/// <summary>
/// WPF confirmation prompt for deleting a ribbon that contains shortcuts (E4-CA5, Q-07).
/// </summary>
internal sealed class WpfRibbonDeletionConfirmer : IRibbonDeletionConfirmer
{
    /// <inheritdoc />
    public bool ConfirmDeleteRibbon(string ribbonName, int itemCount)
    {
        var message = string.Create(
            CultureInfo.CurrentCulture,
            $"Delete ribbon \"{ribbonName}\" and its {itemCount} shortcut(s)? This action cannot be undone.");

        var result = MessageBox.Show(
            message,
            "Sticky Dock Utility",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
