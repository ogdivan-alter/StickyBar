using System.Text.Json.Nodes;
using StickyBar.Core.Configuration;

namespace StickyBar.Tests.TestInfrastructure;

/// <summary>
/// Migration from schema version 0 to 1 used to prove the upgrade pipeline of the store.
/// </summary>
internal sealed class AddDefaultDisplayNameMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 0;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.ContainsKey("displayName"))
        {
            document["displayName"] = "default";
        }
    }
}
