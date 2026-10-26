namespace StickyDock.Core.Diagnostics;

/// <summary>
/// Configuration of <see cref="RotatingFileLogger"/>.
/// </summary>
public sealed record RotatingFileLoggerOptions
{
    /// <summary>Default size at which the active file is rotated: 512 KB.</summary>
    public const long DefaultMaxFileSizeBytes = 512 * 1024;

    /// <summary>Default number of rotated files kept on disk.</summary>
    public const int DefaultMaxArchiveFiles = 3;

    /// <summary>Name of the active log file inside <see cref="DirectoryPath"/>.</summary>
    public const string DefaultFileName = "stickydock.log";

    /// <summary>
    /// Gets the directory where the log files are written. It must be provided by the caller so that the
    /// location stays injectable in tests and in production (<c>%AppData%\StickyDock\logs</c>).
    /// </summary>
    public required string DirectoryPath { get; init; }

    /// <summary>
    /// Gets the name of the active log file.
    /// </summary>
    public string FileName { get; init; } = DefaultFileName;

    /// <summary>
    /// Gets the size, in bytes, from which the active file is rotated. Must be positive.
    /// </summary>
    public long MaxFileSizeBytes { get; init; } = DefaultMaxFileSizeBytes;

    /// <summary>
    /// Gets the number of rotated files kept on disk. Zero means the active file is truncated instead of rotated.
    /// </summary>
    public int MaxArchiveFiles { get; init; } = DefaultMaxArchiveFiles;

    /// <summary>
    /// Gets the lowest severity that is written.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
}
