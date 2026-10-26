namespace StickyDock.Core.Diagnostics;

/// <summary>
/// Convenience entry points over <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>Writes a <see cref="LogLevel.Trace"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Trace(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Trace, message, exception);

    /// <summary>Writes a <see cref="LogLevel.Debug"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Debug(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Debug, message, exception);

    /// <summary>Writes an <see cref="LogLevel.Information"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Information(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Information, message, exception);

    /// <summary>Writes a <see cref="LogLevel.Warning"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Warning(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Warning, message, exception);

    /// <summary>Writes an <see cref="LogLevel.Error"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Error(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Error, message, exception);

    /// <summary>Writes a <see cref="LogLevel.Critical"/> entry.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="message">Message of the entry.</param>
    /// <param name="exception">Optional exception associated with the entry.</param>
    public static void Critical(this ILogger logger, string message, Exception? exception = null)
        => Write(logger, LogLevel.Critical, message, exception);

    private static void Write(ILogger logger, LogLevel level, string message, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Log(level, message, exception);
    }
}
