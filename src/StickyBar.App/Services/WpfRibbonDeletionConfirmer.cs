using System.Windows;
using StickyBar.Core.Localization;
using StickyBar.Core.Services;

namespace StickyBar.App.Services;

/// <summary>
/// WPF confirmation prompt for deleting a ribbon that contains shortcuts (E4-CA5, Q-07).
/// </summary>
internal sealed class WpfRibbonDeletionConfirmer : IRibbonDeletionConfirmer
{
    private readonly LocalizedStrings _strings;

    public WpfRibbonDeletionConfirmer(LocalizedStrings? strings = null)
    {
        _strings = strings ?? new LocalizedStrings();
    }

    /// <inheritdoc />
    public bool ConfirmDeleteRibbon(string ribbonName, int itemCount)
    {
        var message = _strings.FormatConfirmDeleteRibbon(ribbonName, itemCount);

        var result = MessageBox.Show(
            message,
            _strings.AppTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
