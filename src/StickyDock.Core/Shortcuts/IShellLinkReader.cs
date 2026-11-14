namespace StickyDock.Core.Shortcuts;

/// <summary>
/// Reads target, arguments, working directory and icon metadata from a <c>.lnk</c> file without modifying it (E5-CA1, Q-06).
/// </summary>
public interface IShellLinkReader
{
    /// <summary>
    /// Resolves the metadata of the <c>.lnk</c> file at <paramref name="shortcutPath"/>.
    /// </summary>
    /// <param name="shortcutPath">Path to an existing <c>.lnk</c> file.</param>
    /// <returns>The resolved shortcut metadata without native handles.</returns>
    ResolvedShortcut Read(string shortcutPath);
}
