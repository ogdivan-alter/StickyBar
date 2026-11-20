using System.Text.Json.Nodes;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Moves a stored document from schema version 2 to 3 by adding the empty ribbons collection (E4, §5.2-02).
/// </summary>
public sealed class AddRibbonsMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 2;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.ContainsKey(StickyBarConfiguration.RibbonsPropertyName))
        {
            return;
        }

        document[StickyBarConfiguration.RibbonsPropertyName] = new JsonArray();
    }
}
