using System.Globalization;

namespace StickyDock.Core.Configuration;

/// <summary>
/// Raised when the configuration file exists but cannot be read, parsed or deserialized.
/// </summary>
public sealed class ConfigurationLoadException : ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoadException"/> class.
    /// </summary>
    /// <param name="filePath">Path of the configuration file.</param>
    /// <param name="message">Description of the failure.</param>
    public ConfigurationLoadException(string filePath, string message)
        : base(FormatMessage(filePath, message))
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoadException"/> class.
    /// </summary>
    /// <param name="filePath">Path of the configuration file.</param>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">Underlying failure.</param>
    public ConfigurationLoadException(string filePath, string message, Exception innerException)
        : base(FormatMessage(filePath, message), innerException)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Gets the path of the configuration file that could not be loaded.
    /// </summary>
    public string FilePath { get; }

    private static string FormatMessage(string filePath, string message)
        => string.Create(CultureInfo.InvariantCulture, $"Configuration file '{filePath}': {message}");
}
