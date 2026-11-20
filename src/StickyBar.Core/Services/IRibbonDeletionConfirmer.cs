namespace StickyBar.Core.Services;

/// <summary>
/// Confirms the deletion of a ribbon that contains shortcut items (E4-CA5, Q-07).
/// </summary>
public interface IRibbonDeletionConfirmer
{
    /// <summary>
    /// Prompts the user to confirm deleting <paramref name="ribbonName"/> along with its
    /// <paramref name="itemCount"/> shortcut items.
    /// </summary>
    /// <param name="ribbonName">Display name of the ribbon to delete.</param>
    /// <param name="itemCount">Number of shortcut items that will be permanently removed.</param>
    /// <returns><see langword="true"/> when the user confirms deletion; otherwise, <see langword="false"/>.</returns>
    bool ConfirmDeleteRibbon(string ribbonName, int itemCount);
}
