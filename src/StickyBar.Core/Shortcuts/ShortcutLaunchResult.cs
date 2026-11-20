namespace StickyBar.Core.Shortcuts;

/// <summary>
/// Result of attempting to launch a <c>.lnk</c> shortcut (E5-CA2).
/// </summary>
/// <param name="Succeeded">Whether the process was started.</param>
/// <param name="ErrorMessage">Human-readable failure reason when <paramref name="Succeeded"/> is <see langword="false"/>.</param>
public sealed record ShortcutLaunchResult(bool Succeeded, string? ErrorMessage = null);
