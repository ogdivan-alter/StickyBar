using System.Globalization;
using System.Text;

namespace StickyDock.Core.Diagnostics;

/// <summary>
/// Appends log entries to a file and rotates it by size, keeping a bounded number of archives.
/// </summary>
/// <remarks>
/// The logger writes synchronously and flushes every entry: the application is idle most of the time, so the
/// volume is low, and a flushed file survives a crash. It starts no timers and no background threads, which is
/// required not to compromise the idle CPU budget (RNF-02).
/// </remarks>
public sealed class RotatingFileLogger : ILogger, IDisposable
{
    private readonly RotatingFileLoggerOptions _options;
    private readonly Lock _gate = new();
    private StreamWriter? _writer;
    private long _currentSizeInBytes;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RotatingFileLogger"/> class and opens the active file.
    /// </summary>
    /// <param name="options">Logger configuration.</param>
    public RotatingFileLogger(RotatingFileLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DirectoryPath))
        {
            throw new ArgumentException("A log directory must be provided.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FileName))
        {
            throw new ArgumentException("A log file name must be provided.", nameof(options));
        }

        if (options.MaxFileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxFileSizeBytes, "The maximum file size must be positive.");
        }

        if (options.MaxArchiveFiles < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxArchiveFiles, "The number of archive files cannot be negative.");
        }

        _options = options;
        Directory.CreateDirectory(_options.DirectoryPath);
        OpenWriter();
    }

    /// <summary>
    /// Gets the lowest severity that is written.
    /// </summary>
    public LogLevel MinimumLevel => _options.MinimumLevel;

    /// <summary>
    /// Gets the full path of the active log file.
    /// </summary>
    public string CurrentLogFilePath => Path.Combine(_options.DirectoryPath, _options.FileName);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level) => !_disposed && level >= MinimumLevel;

    /// <inheritdoc />
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (level < MinimumLevel)
            {
                return;
            }

            var writer = _writer ?? throw new ObjectDisposedException(nameof(RotatingFileLogger));

            writer.WriteLine(FormatEntry(level, message));

            if (exception is not null)
            {
                writer.WriteLine(FormatException(exception));
            }

            writer.Flush();
            _currentSizeInBytes = writer.BaseStream.Length;

            if (_currentSizeInBytes >= _options.MaxFileSizeBytes)
            {
                Rotate();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _writer?.Dispose();
            _writer = null;
        }

        GC.SuppressFinalize(this);
    }

    private static string FormatEntry(LogLevel level, string message)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{level}] {message}");

    private static string FormatException(Exception exception)
        => string.Create(CultureInfo.InvariantCulture, $"exception: {exception}");

    private string GetArchiveFilePath(int index)
        => Path.Combine(
            _options.DirectoryPath,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetFileNameWithoutExtension(_options.FileName)}.{index}{Path.GetExtension(_options.FileName)}"));

    private void OpenWriter()
    {
        var filePath = CurrentLogFilePath;

        _currentSizeInBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void Rotate()
    {
        _writer?.Dispose();
        _writer = null;

        if (_options.MaxArchiveFiles == 0)
        {
            File.Delete(CurrentLogFilePath);
        }
        else
        {
            for (var index = _options.MaxArchiveFiles - 1; index >= 1; index--)
            {
                MoveIfExists(GetArchiveFilePath(index), GetArchiveFilePath(index + 1));
            }

            MoveIfExists(CurrentLogFilePath, GetArchiveFilePath(1));
        }

        OpenWriter();
    }

    private static void MoveIfExists(string sourcePath, string destinationPath)
    {
        if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, destinationPath, overwrite: true);
        }
    }
}
