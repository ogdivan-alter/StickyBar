using Microsoft.Win32;
using StickyDock.Core.Services;

namespace StickyDock.Interop.Theme;

/// <summary>
/// Detects the Windows light/dark theme. Uses the AppsUseLightTheme registry value with live updates via
/// <see cref="SystemEvents.UserPreferenceChanged"/> and falls back safely.
/// </summary>
public sealed class WindowsThemeService : IWindowsThemeService, IThemeService
{
    private readonly object _sync = new();
    private bool _isLightTheme;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsThemeService"/> class.
    /// </summary>
    public WindowsThemeService()
    {
        _isLightTheme = QueryIsLightTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <inheritdoc />
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public bool IsLightTheme
    {
        get
        {
            lock (_sync)
            {
                return _isLightTheme;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
        {
            return;
        }

        var newValue = QueryIsLightTheme();
        bool changed;

        lock (_sync)
        {
            changed = _isLightTheme != newValue;
            if (changed)
            {
                _isLightTheme = newValue;
            }
        }

        if (changed)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(newValue));
        }
    }

    private static bool QueryIsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value != 0;
            }
        }
        catch
        {
            // Fall back to light theme if detection fails.
        }

        return true;
    }
}

/// <summary>
/// Provides Windows theme information.
/// </summary>
public interface IWindowsThemeService : IThemeService
{
}
