using System.Diagnostics;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Shortcuts;

/// <summary>
/// Reads a <c>.lnk</c> file via <see cref="IShellLinkReader"/> and starts its target with exact arguments and working
/// directory (E5-CA2, §4.5-03, Q-06).
/// </summary>
public sealed class ShortcutLauncher : IShortcutLauncher
{
    private readonly IShellLinkReader _reader;
    private readonly Func<ProcessStartInfo, bool> _processStarter;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortcutLauncher"/> class using <see cref="Process.Start(ProcessStartInfo)"/>.
    /// </summary>
    public ShortcutLauncher(IShellLinkReader reader, ILogger? logger = null)
        : this(reader, DefaultStartProcess, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortcutLauncher"/> class with an injectable process starter for tests.
    /// </summary>
    public ShortcutLauncher(
        IShellLinkReader reader,
        Func<ProcessStartInfo, bool> processStarter,
        ILogger? logger = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
        _logger = logger;
    }

    /// <inheritdoc />
    public ShortcutLaunchResult Launch(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath))
        {
            return new ShortcutLaunchResult(false, "The shortcut path is empty.");
        }

        ResolvedShortcut resolved;

        try
        {
            resolved = _reader.Read(shortcutPath);
        }
        catch (Exception exception)
        {
            _logger?.Error($"Failed to read shortcut '{shortcutPath}'.", exception);
            return new ShortcutLaunchResult(false, $"Could not read shortcut '{shortcutPath}': {exception.Message}");
        }

        if (string.IsNullOrWhiteSpace(resolved.TargetPath))
        {
            var message = $"Shortcut '{shortcutPath}' has no target path.";
            _logger?.Warning(message);
            return new ShortcutLaunchResult(false, message);
        }

        var workingDirectory = !string.IsNullOrWhiteSpace(resolved.WorkingDirectory)
            ? resolved.WorkingDirectory
            : Path.GetDirectoryName(resolved.TargetPath) ?? string.Empty;

        var startInfo = new ProcessStartInfo
        {
            FileName = resolved.TargetPath,
            Arguments = resolved.Arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true,
        };

        try
        {
            var started = _processStarter(startInfo);

            if (!started)
            {
                var message = $"Could not start '{resolved.TargetPath}'.";
                _logger?.Warning(message);
                return new ShortcutLaunchResult(false, message);
            }

            _logger?.Information($"Launched shortcut '{resolved.DisplayName}' -> '{resolved.TargetPath}'.");
            return new ShortcutLaunchResult(true);
        }
        catch (Exception exception)
        {
            _logger?.Error($"Failed to launch '{resolved.TargetPath}'.", exception);
            return new ShortcutLaunchResult(false, $"Could not launch '{resolved.TargetPath}': {exception.Message}");
        }
    }

    private static bool DefaultStartProcess(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo);
        return process is not null;
    }
}
