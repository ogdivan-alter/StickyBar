using System.Globalization;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Raised when the stored schema version cannot be handled: it is newer than this application, or no migration
/// exists for the step that is missing.
/// </summary>
public sealed class ConfigurationSchemaException : ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSchemaException"/> class.
    /// </summary>
    /// <param name="filePath">Path of the configuration file.</param>
    /// <param name="foundVersion">Schema version found in the file.</param>
    /// <param name="supportedVersion">Schema version supported by this application.</param>
    /// <param name="detail">Optional detail about the failure.</param>
    public ConfigurationSchemaException(string filePath, int foundVersion, int supportedVersion, string? detail = null)
        : base(FormatMessage(filePath, foundVersion, supportedVersion, detail))
    {
        FilePath = filePath;
        FoundVersion = foundVersion;
        SupportedVersion = supportedVersion;
    }

    /// <summary>
    /// Gets the path of the configuration file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the schema version found in the file.
    /// </summary>
    public int FoundVersion { get; }

    /// <summary>
    /// Gets the schema version supported by this application.
    /// </summary>
    public int SupportedVersion { get; }

    private static string FormatMessage(string filePath, int foundVersion, int supportedVersion, string? detail)
    {
        var message = string.Create(
            CultureInfo.InvariantCulture,
            $"Configuration file '{filePath}' declares schema version {foundVersion}, which cannot be read by an application that supports version {supportedVersion}.");

        return detail is null ? message : string.Create(CultureInfo.InvariantCulture, $"{message} {detail}");
    }
}
