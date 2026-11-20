using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using StickyBar.Core.Configuration;
using StickyBar.Core.Diagnostics;
using StickyBar.Core.Docking;
using StickyBar.Core.Interaction;
using StickyBar.Core.Localization;
using StickyBar.Core.Mvvm;
using StickyBar.Core.Services;

namespace StickyBar.Core.ViewModels;

/// <summary>
/// Represents a selectable UI language option in settings.
/// </summary>
/// <param name="Code">Language code (<c>"es"</c> or <c>"en"</c>).</param>
/// <param name="DisplayName">Human-readable native name.</param>
public sealed record LanguageOption(string Code, string DisplayName);

/// <summary>
/// View model for the general settings window opened from the system tray (§4.6-01, HU05 CA #1, Q-09, E6-CA3).
/// </summary>
public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigurationDocument _configuration;
    private readonly IAutoStartService _autoStartService;
    private readonly IMonitorCatalog? _monitorCatalog;
    private readonly ILogger _logger;
    private readonly LocalizedStrings _strings;

    private int _gracePeriodMilliseconds;
    private bool _autoStartEnabled;
    private string _dockedEdgeLabel;
    private string _selectedLanguage;
    private bool _isUpdatingMonitors;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="configuration">Shared configuration document.</param>
    /// <param name="autoStartService">Service that reads and writes the Windows startup registration.</param>
    /// <param name="logger">Application logger.</param>
    /// <param name="monitorCatalog">Optional catalog of attached display monitors (E7).</param>
    /// <param name="strings">Optional shared localized strings instance.</param>
    public SettingsViewModel(
        IConfigurationDocument configuration,
        IAutoStartService autoStartService,
        ILogger logger,
        IMonitorCatalog? monitorCatalog = null,
        LocalizedStrings? strings = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _autoStartService = autoStartService ?? throw new ArgumentNullException(nameof(autoStartService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitorCatalog = monitorCatalog;
        _strings = strings ?? new LocalizedStrings(_configuration.Current.Language);

        _selectedLanguage = SupportedLanguages.Normalize(_configuration.Current.Language);
        _strings.SetLanguage(_selectedLanguage);
        _gracePeriodMilliseconds = GracePeriod.Normalize(_configuration.Current.GracePeriodMilliseconds);
        _dockedEdgeLabel = _strings.FormatDockEdge(_configuration.Current.DockEdge);
        _autoStartEnabled = ReadAutoStartState();

        RefreshMonitors();

        CloseCommand = new RelayCommand(RequestClose);
        _configuration.Changed += OnConfigurationChanged;
    }

    /// <summary>Gets the localized UI strings bound by the settings window.</summary>
    public LocalizedStrings Strings => _strings;

    /// <summary>Gets the available UI languages.</summary>
    public IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new(SupportedLanguages.Spanish, "Español"),
        new(SupportedLanguages.English, "English"),
    ];

    /// <summary>Gets or sets the active UI language code (<c>"es"</c> or <c>"en"</c>).</summary>
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            var normalized = SupportedLanguages.Normalize(value);

            if (!SetProperty(ref _selectedLanguage, normalized))
            {
                return;
            }

            _strings.SetLanguage(normalized);
            DockedEdgeLabel = _strings.FormatDockEdge(_configuration.Current.DockEdge);
            RefreshMonitors();
            _configuration.Update(current => current with { Language = normalized });
            _logger.Information($"UI language updated to '{normalized}'.");
        }
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
    /// Gets the secondary display monitors detected on the system (E7, §4.6-02).
    /// </summary>
    public ObservableCollection<SecondaryMonitorItemViewModel> SecondaryMonitors { get; } = [];

    /// <summary>
    /// Gets whether at least one secondary display monitor is attached.
    /// </summary>
    public bool HasSecondaryMonitors => SecondaryMonitors.Count > 0;

    /// <summary>
    /// Gets whether no secondary display monitors are attached.
    /// </summary>
    public bool HasNoSecondaryMonitors => SecondaryMonitors.Count == 0;

    /// <summary>
    /// Refreshes the detected secondary display monitors from <see cref="IMonitorCatalog"/>.
    /// </summary>
    public void RefreshMonitors()
    {
        SecondaryMonitors.Clear();

        if (_monitorCatalog is not null)
        {
            var enabled = new HashSet<string>(
                _configuration.Current.EnabledReplicaMonitors ?? [],
                StringComparer.OrdinalIgnoreCase);

            var index = 1;

            foreach (var monitor in _monitorCatalog.GetMonitors())
            {
                if (monitor.IsPrimary || string.IsNullOrWhiteSpace(monitor.DeviceName))
                {
                    continue;
                }

                var label = _strings.FormatSecondaryMonitor(
                    index,
                    monitor.DeviceName,
                    monitor.Bounds.Width,
                    monitor.Bounds.Height);
                SecondaryMonitors.Add(new SecondaryMonitorItemViewModel(
                    monitor.DeviceName,
                    label,
                    enabled.Contains(monitor.DeviceName),
                    OnMonitorToggled));
                index++;
            }
        }

        OnPropertyChanged(nameof(HasSecondaryMonitors));
        OnPropertyChanged(nameof(HasNoSecondaryMonitors));
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

    private void OnMonitorToggled(string deviceName, bool isEnabled)
    {
        if (_disposed || _isUpdatingMonitors)
        {
            return;
        }

        _isUpdatingMonitors = true;

        try
        {
            _configuration.Update(current =>
            {
                var updated = new List<string>(current.EnabledReplicaMonitors ?? []);
                updated.RemoveAll(name => string.Equals(name, deviceName, StringComparison.OrdinalIgnoreCase));

                if (isEnabled)
                {
                    updated.Add(deviceName);
                }

                return current with { EnabledReplicaMonitors = updated };
            });

            _logger.Information($"Replica monitor '{deviceName}' {(isEnabled ? "enabled" : "disabled")}.");
        }
        finally
        {
            _isUpdatingMonitors = false;
        }
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        var normalized = GracePeriod.Normalize(e.Document.GracePeriodMilliseconds);
        SetProperty(ref _gracePeriodMilliseconds, normalized, nameof(GracePeriodMilliseconds));

        var normalizedLanguage = SupportedLanguages.Normalize(e.Document.Language);
        if (SetProperty(ref _selectedLanguage, normalizedLanguage, nameof(SelectedLanguage)))
        {
            _strings.SetLanguage(normalizedLanguage);
            RefreshMonitors();
        }

        DockedEdgeLabel = _strings.FormatDockEdge(e.Document.DockEdge);

        if (_isUpdatingMonitors)
        {
            return;
        }

        var enabled = new HashSet<string>(
            e.Document.EnabledReplicaMonitors ?? [],
            StringComparer.OrdinalIgnoreCase);

        foreach (var monitor in SecondaryMonitors)
        {
            monitor.SetEnabledFromConfiguration(enabled.Contains(monitor.DeviceName));
        }
    }

    private void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
