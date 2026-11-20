namespace StickyDock.Core.Localization;

/// <summary>
/// Supported UI language codes and normalization helper.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>Spanish (default).</summary>
    public const string Spanish = "es";

    /// <summary>English.</summary>
    public const string English = "en";

    /// <summary>
    /// Normalizes a persisted or user-supplied language code to a supported language (<c>"es"</c> or <c>"en"</c>).
    /// </summary>
    public static string Normalize(string? language)
        => string.Equals(language?.Trim(), English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Spanish;
}
