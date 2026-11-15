using StickyDock.Core.Mvvm;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// Represents a secondary display monitor option in <see cref="SettingsViewModel"/> (§4.6-02, E7).
/// </summary>
public sealed class SecondaryMonitorItemViewModel : ViewModelBase
{
    private readonly Action<string, bool> _onToggled;
    private bool _isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecondaryMonitorItemViewModel"/> class.
    /// </summary>
    public SecondaryMonitorItemViewModel(
        string deviceName,
        string displayLabel,
        bool isEnabled,
        Action<string, bool> onToggled)
    {
        DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
        DisplayLabel = displayLabel ?? throw new ArgumentNullException(nameof(displayLabel));
        _isEnabled = isEnabled;
        _onToggled = onToggled ?? throw new ArgumentNullException(nameof(onToggled));
    }

    /// <summary>Gets the Win32 device name of the monitor.</summary>
    public string DeviceName { get; }

    /// <summary>Gets the human-readable label displayed in the settings UI.</summary>
    public string DisplayLabel { get; }

    /// <summary>Gets or sets whether mirrored replica is enabled on this monitor.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value))
            {
                return;
            }

            _onToggled(DeviceName, value);
        }
    }

    internal void SetEnabledFromConfiguration(bool isEnabled)
        => SetProperty(ref _isEnabled, isEnabled, nameof(IsEnabled));
}
