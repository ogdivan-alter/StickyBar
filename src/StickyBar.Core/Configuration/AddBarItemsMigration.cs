using System.Text.Json.Nodes;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Moves a stored document from schema version 3 to 4 by adding the empty main-bar shortcut items collection (E5, §5.2-02).
/// </summary>
public sealed class AddBarItemsMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 3;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.ContainsKey(StickyBarConfiguration.BarItemsPropertyName))
        {
            return;
        }

        document[StickyBarConfiguration.BarItemsPropertyName] = new JsonArray();
    }
}
