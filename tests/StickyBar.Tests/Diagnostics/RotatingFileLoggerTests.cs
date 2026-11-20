using StickyBar.Core.Diagnostics;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Diagnostics;

/// <summary>
/// Level filtering and size rotation of the file logger.
/// </summary>
public sealed class RotatingFileLoggerTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Log_WritesEntriesAtOrAboveTheMinimumLevel()
    {
        string logPath;
        using (var logger = CreateLogger())
        {
            logger.Debug("discarded");
            logger.Information("recorded");
            logPath = logger.CurrentLogFilePath;
        }

        var content = File.ReadAllText(logPath);
        Assert.DoesNotContain("discarded", content, StringComparison.Ordinal);
        Assert.Contains("recorded", content, StringComparison.Ordinal);
        Assert.Contains("[Information]", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_WhenAnExceptionIsProvided_WritesItsDetails()
    {
        string logPath;
        using (var logger = CreateLogger())
        {
            logger.Error("operation failed", new InvalidOperationException("boom"));
            logPath = logger.CurrentLogFilePath;
        }

        var content = File.ReadAllText(logPath);
        Assert.Contains("operation failed", content, StringComparison.Ordinal);
        Assert.Contains("boom", content, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_WhenTheFileReachesTheMaximumSize_RotatesIntoArchives()
    {
        using var logger = CreateLogger(maxFileSizeBytes: 200, maxArchiveFiles: 2);

        for (var index = 0; index < 40; index++)
        {
            logger.Information("an entry long enough to trigger the rotation of the file");
        }

        Assert.True(File.Exists(_directory.GetFilePath("stickybar.1.log")));
        Assert.False(File.Exists(_directory.GetFilePath("stickybar.3.log")));
    }

    [Fact]
    public void Log_WhenNoArchiveIsAllowed_TruncatesTheFileInstead()
    {
        using var logger = CreateLogger(maxFileSizeBytes: 200, maxArchiveFiles: 0);

        for (var index = 0; index < 20; index++)
        {
            logger.Information("an entry long enough to trigger the rotation of the file");
        }

        Assert.Empty(_directory.GetFiles("stickybar.*.log"));
    }

    [Fact]
    public void IsEnabled_ReflectsTheMinimumLevel()
    {
        using var logger = CreateLogger(minimumLevel: LogLevel.Warning);

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.Equal(LogLevel.Warning, logger.MinimumLevel);
    }

    [Fact]
    public void Log_WhenTheLoggerIsDisposed_Throws()
    {
        var logger = CreateLogger();
        logger.Dispose();

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.Throws<ObjectDisposedException>(() => logger.Log(LogLevel.Information, "late entry"));
    }

    [Fact]
    public void Constructor_WhenTheDirectoryIsMissing_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RotatingFileLogger(new RotatingFileLoggerOptions { DirectoryPath = " " }));
    }

    [Fact]
    public void Constructor_WhenTheSizeLimitIsNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RotatingFileLogger(new RotatingFileLoggerOptions
            {
                DirectoryPath = _directory.Path,
                MaxFileSizeBytes = 0,
            }));
    }

    private RotatingFileLogger CreateLogger(
        LogLevel minimumLevel = LogLevel.Information,
        long maxFileSizeBytes = RotatingFileLoggerOptions.DefaultMaxFileSizeBytes,
        int maxArchiveFiles = RotatingFileLoggerOptions.DefaultMaxArchiveFiles)
        => new(new RotatingFileLoggerOptions
        {
            DirectoryPath = _directory.Path,
            MinimumLevel = minimumLevel,
            MaxFileSizeBytes = maxFileSizeBytes,
            MaxArchiveFiles = maxArchiveFiles,
        });
}
