using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Configuration;

/// <summary>
/// Single place where the configuration store of the product is built, with its migrations registered.
/// </summary>
/// <remarks>
/// The application and the tests share this factory so that the chain of migrations cannot drift between them
/// (ADR-003, D6).
/// </remarks>
public static class StickyDockSettings
{
    /// <summary>
    /// Creates the store of <see cref="StickyDockConfiguration"/> with every migration of the schema registered.
    /// </summary>
    /// <param name="filePath">Full or relative path of the configuration file.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>The store, ready to read any version up to <see cref="StickyDockConfiguration.CurrentSchemaVersion"/>.</returns>
    public static IConfigurationStore<StickyDockConfiguration> CreateStore(string filePath, ILogger? logger = null)
        => new JsonConfigurationStore<StickyDockConfiguration>(
            filePath,
            static () => new StickyDockConfiguration(),
            [new AddGracePeriodMigration(), new AddRibbonsMigration(), new AddBarItemsMigration()],
            logger: logger);
}
