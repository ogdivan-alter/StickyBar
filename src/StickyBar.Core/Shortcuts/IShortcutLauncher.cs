namespace StickyBar.Core.Shortcuts;

/// <summary>
/// Launches a <c>.lnk</c> shortcut while preserving its target, arguments and working directory (E5-CA2, §4.5-03).
/// </summary>
public interface IShortcutLauncher
{
    /// <summary>
    /// Reads and launches the shortcut at <paramref name="shortcutPath"/>.
    /// </summary>
    /// <param name="shortcutPath">Path to the <c>.lnk</c> file.</param>
    /// <returns>The launch result.</returns>
    ShortcutLaunchResult Launch(string shortcutPath);
}
