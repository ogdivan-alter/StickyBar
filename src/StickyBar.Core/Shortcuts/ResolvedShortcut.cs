namespace StickyBar.Core.Shortcuts;

/// <summary>
/// Pure, handle-free metadata resolved from a Windows <c>.lnk</c> shortcut file (E5-CA1, §5.1-05, Q-06).
/// </summary>
/// <param name="ShortcutPath">Full path to the <c>.lnk</c> file.</param>
/// <param name="DisplayName">Display name derived from the shortcut file.</param>
/// <param name="TargetPath">Target executable, document or URI declared by the shortcut.</param>
/// <param name="Arguments">Command-line arguments declared by the shortcut.</param>
/// <param name="WorkingDirectory">Working directory declared by the shortcut.</param>
/// <param name="IconPngBytes">Optional PNG-encoded icon bytes extracted from the shortcut or its target.</param>
public sealed record ResolvedShortcut(
    string ShortcutPath,
    string DisplayName,
    string TargetPath,
    string Arguments,
    string WorkingDirectory,
    byte[]? IconPngBytes);
