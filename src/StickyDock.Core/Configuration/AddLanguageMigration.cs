using System.Text.Json.Nodes;
using StickyDock.Core.Localization;

namespace StickyDock.Core.Configuration;

/// <summary>
/// Moves a stored document from schema version 5 to 6 by adding the UI language preference (default <c>"es"</c>).
/// </summary>
public sealed class AddLanguageMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 5;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.ContainsKey(StickyDockConfiguration.LanguagePropertyName))
        {
            return;
        }

        document[StickyDockConfiguration.LanguagePropertyName] = SupportedLanguages.Spanish;
    }
}
