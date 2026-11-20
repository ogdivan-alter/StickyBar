using System.Text.Json.Nodes;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Moves a stored document from schema version 4 to 5 by adding the enabled replica monitor identifiers collection
/// (E7, RF05, §4.6-02).
/// </summary>
public sealed class AddMonitorReplicaMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 4;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.ContainsKey(StickyBarConfiguration.EnabledReplicaMonitorsPropertyName))
        {
            return;
        }

        document[StickyBarConfiguration.EnabledReplicaMonitorsPropertyName] = new JsonArray();
    }
}
