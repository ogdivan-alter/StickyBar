namespace StickyDock.Core.Diagnostics;

/// <summary>
/// Severity of a log entry, ordered from the most verbose to the most severe.
/// </summary>
public enum LogLevel
{
    /// <summary>Fine-grained detail, disabled in normal operation.</summary>
    Trace = 0,

    /// <summary>Diagnostic detail useful while developing.</summary>
    Debug = 1,

    /// <summary>Normal application events.</summary>
    Information = 2,

    /// <summary>An unexpected condition that does not stop the application.</summary>
    Warning = 3,

    /// <summary>An operation failed.</summary>
    Error = 4,

    /// <summary>The application cannot continue.</summary>
    Critical = 5,
}
