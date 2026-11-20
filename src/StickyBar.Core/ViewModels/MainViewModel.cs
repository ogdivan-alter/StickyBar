using System.Globalization;
using StickyBar.Core.Configuration;
using StickyBar.Core.Diagnostics;
using StickyBar.Core.Mvvm;

namespace StickyBar.Core.ViewModels;

/// <summary>
/// State of the application shell. It holds no view references, so it can be exercised without WPF (§5.2-01).
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private string _statusText;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="configuration">Configuration loaded at startup.</param>
    /// <param name="logger">Logger used to record the state of the shell.</param>
    public MainViewModel(StickyBarConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        ConfigurationSchemaVersion = configuration.SchemaVersion;
        _statusText = string.Create(
            CultureInfo.InvariantCulture,
            $"Configuration schema v{ConfigurationSchemaVersion}");

        logger.Information("Shell view model created.");
    }

    /// <summary>
    /// Gets the schema version of the configuration loaded at startup.
    /// </summary>
    public int ConfigurationSchemaVersion { get; }

    /// <summary>
    /// Gets or sets the text shown by the shell.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
}
