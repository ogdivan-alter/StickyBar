using System.Text.Json.Nodes;
using StickyBar.Core.Interaction;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Moves a stored document from schema version 1 to 2 by adding the grace period of the collapse timer (ADR-003, D6).
/// </summary>
/// <remarks>
/// The document of E0 to E2 does not know the preference, so it is added with the default value. An existing value is
/// never overwritten: the migration only fills what is missing.
/// </remarks>
public sealed class AddGracePeriodMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 1;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.ContainsKey(StickyBarConfiguration.GracePeriodPropertyName))
        {
            return;
        }

        document[StickyBarConfiguration.GracePeriodPropertyName] = GracePeriod.DefaultMilliseconds;
    }
}
