namespace StickyDock.Core.Services;

/// <summary>
/// Theme service abstraction for Core (MVVM without direct Win32 dependency).
/// </summary>
public interface IThemeService : IDisposable
{
    /// <summary>Raised when theme changes.</summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>Gets whether light theme is active.</summary>
    bool IsLightTheme { get; }
}

/// <summary>
/// Theme changed args.
/// </summary>
public sealed record ThemeChangedEventArgs(bool IsLightTheme);
