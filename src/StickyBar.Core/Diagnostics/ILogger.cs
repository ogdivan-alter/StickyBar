namespace StickyBar.Core.Diagnostics;

/// <summary>
/// Minimal logging abstraction of the application. It is intentionally small: a background utility does not need
/// a full logging framework, and every extra dependency costs memory at startup (RNF-01).
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Gets the lowest severity that is written.
    /// </summary>
    LogLevel MinimumLevel { get; }

    /// <summary>
    /// Determines whether an entry of the given severity would be written.
    /// </summary>
    /// <param name="level">Severity to test.</param>
    /// <returns><see langword="true"/> when the entry would be written.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Writes an entry.
    /// </summary>
    /// <param name="level">Severity of the entry.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    void Log(LogLevel level, string message, Exception? exception = null);
}
