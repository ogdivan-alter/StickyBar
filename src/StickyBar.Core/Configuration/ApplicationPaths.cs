namespace StickyBar.Core.Configuration;

/// <summary>
/// Well-known locations the application reads and writes, resolved once at startup.
/// </summary>
/// <remarks>
/// The root is injectable so that tests can work on a temporary directory instead of the real profile
/// (§5.2-02 of the specification requires <c>%AppData%</c>; the location itself is not hard-coded into the stores).
/// </remarks>
public sealed class ApplicationPaths
{
    /// <summary>Folder created under the roaming application data directory.</summary>
    public const string FolderName = "StickyBar";

    /// <summary>Name of the versioned configuration file.</summary>
    public const string ConfigurationFileName = "settings.json";

    /// <summary>Folder, below <see cref="DataDirectory"/>, where log files are written.</summary>
    public const string LogsFolderName = "logs";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationPaths"/> class.
    /// </summary>
    /// <param name="dataDirectory">Directory that holds the configuration file.</param>
    /// <param name="logDirectory">Directory that holds the log files.</param>
    public ApplicationPaths(string dataDirectory, string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("A log directory is required.", nameof(logDirectory));
        }

        DataDirectory = Path.GetFullPath(dataDirectory);
        LogDirectory = Path.GetFullPath(logDirectory);
    }

    /// <summary>
    /// Gets the directory that holds the configuration file.
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>
    /// Gets the directory that holds the log files.
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// Gets the full path of the versioned configuration file.
    /// </summary>
    public string ConfigurationFilePath => Path.Combine(DataDirectory, ConfigurationFileName);

    /// <summary>
    /// Resolves the paths of the current user: <c>%AppData%\StickyBar</c>.
    /// </summary>
    /// <returns>The application paths of the current user.</returns>
    public static ApplicationPaths ForCurrentUser()
        => ForRoot(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    /// <summary>
    /// Resolves the standard application paths below an arbitrary root directory.
    /// </summary>
    /// <param name="rootDirectory">Directory that contains the application folder.</param>
    /// <returns>The application paths below <paramref name="rootDirectory"/>.</returns>
    public static ApplicationPaths ForRoot(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A root directory is required.", nameof(rootDirectory));
        }

        var dataDirectory = Path.Combine(rootDirectory, FolderName);

        return new ApplicationPaths(dataDirectory, Path.Combine(dataDirectory, LogsFolderName));
    }
}
