using StickyDock.Core.Diagnostics;

namespace StickyDock.Tests.TestInfrastructure;

/// <summary>
/// Logger that keeps the entries in memory so tests can inspect what was reported.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    private readonly List<string> _entries = [];

    /// <summary>
    /// Gets the recorded entries, in order.
    /// </summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <inheritdoc />
    public LogLevel MinimumLevel => LogLevel.Trace;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level) => true;

    /// <inheritdoc />
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        var entry = $"{level}: {message}";

        if (exception is not null)
        {
            entry = $"{entry} | {exception.GetType().Name}";
        }

        _entries.Add(entry);
    }
}
