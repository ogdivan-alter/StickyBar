using System.Windows.Input;
using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Interaction;
using StickyDock.Core.Mvvm;
using StickyDock.Core.Services;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// View model for the general settings window opened from the system tray (§4.6-01, HU05 CA #1, Q-09, E6-CA3).
/// </summary>
public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigurationDocument _configuration;
    private readonly IAutoStartService _autoStartService;
    private readonly ILogger _logger;

    private int _gracePeriodMilliseconds;
    private bool _autoStartEnabled;
    private string _dockedEdgeLabel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="configuration">Shared configuration document.</param>
    /// <param name="autoStartService">Service that reads and writes the Windows startup registration.</param>
    /// <param name="logger">Application logger.</param>
    public SettingsViewModel(
        IConfigurationDocument configuration,
        IAutoStartService autoStartService,
        ILogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _autoStartService = autoStartService ?? throw new ArgumentNullException(nameof(autoStartService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _gracePeriodMilliseconds = GracePeriod.Normalize(_configuration.Current.GracePeriodMilliseconds);
        _dockedEdgeLabel = FormatDockEdge(_configuration.Current.DockEdge);
        _autoStartEnabled = ReadAutoStartState();

        CloseCommand = new RelayCommand(RequestClose);
        _configuration.Changed += OnConfigurationChanged;
    }

    /// <summary>
    /// Raised when the user invokes <see cref="CloseCommand"/> to close the settings window.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Gets the minimum configurable collapse grace period, in milliseconds.
    /// </summary>
    public int GracePeriodMinimum => GracePeriod.MinimumMilliseconds;

    /// <summary>
    /// Gets the maximum configurable collapse grace period, in milliseconds.
    /// </summary>
    public int GracePeriodMaximum => GracePeriod.MaximumMilliseconds;

    /// <summary>
    /// Gets or sets the collapse grace period in milliseconds (300–500 ms, 400 ms default).
    /// Changes apply immediately to the shared configuration document and persist to disk (E6-CA3).
    /// </summary>
    public int GracePeriodMilliseconds
    {
        get => _gracePeriodMilliseconds;
        set
        {
            var normalized = GracePeriod.Normalize(value);

            if (!SetProperty(ref _gracePeriodMilliseconds, normalized))
            {
                if (value != normalized)
                {
                    OnPropertyChanged(nameof(GracePeriodMilliseconds));
                }

                return;
            }

            _configuration.Update(current => current with { GracePeriodMilliseconds = normalized });
            _logger.Information($"Grace period updated to {normalized} ms.");
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application starts automatically with Windows (E6-CA4).
    /// </summary>
    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set
        {
            if (_autoStartEnabled == value)
            {
                return;
            }

            try
            {
                _autoStartService.SetEnabled(value);
                _autoStartEnabled = value;
                OnPropertyChanged();
                _logger.Information($"Windows auto-start {(value ? "enabled" : "disabled")}.");
            }
            catch (Exception exception)
            {
                _logger.Error("Could not update the Windows auto-start registration.", exception);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets a human-readable label of the current dock edge (or Floating).
    /// </summary>
    public string DockedEdgeLabel
    {
        get => _dockedEdgeLabel;
        private set => SetProperty(ref _dockedEdgeLabel, value);
    }

    /// <summary>
    /// Gets the command that closes the settings window.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _configuration.Changed -= OnConfigurationChanged;
        CloseRequested = null;
    }

    private bool ReadAutoStartState()
    {
        try
        {
            return _autoStartService.IsEnabled();
        }
        catch (Exception exception)
        {
            _logger.Error("Could not query the Windows auto-start registration.", exception);
            return false;
        }
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        var normalized = GracePeriod.Normalize(e.Document.GracePeriodMilliseconds);
        SetProperty(ref _gracePeriodMilliseconds, normalized, nameof(GracePeriodMilliseconds));
        DockedEdgeLabel = FormatDockEdge(e.Document.DockEdge);
    }

    private void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private static string FormatDockEdge(Docking.DockEdge? edge)
        => edge?.ToString() ?? "Floating";
}
