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
            [new AddGracePeriodMigration(), new AddRibbonsMigration(), new AddBarItemsMigration(), new AddMonitorReplicaMigration(), new AddLanguageMigration()],
            logger: logger);

    /// <summary>
    /// Loads the configuration from <paramref name="store"/>, recovering with default values and logging a warning
    /// if the persisted file is corrupt, truncated, or has an incompatible schema (E8-CA3).
    /// </summary>
    /// <param name="store">Store to read from.</param>
    /// <param name="logger">Optional logger that receives a warning when recovery occurs.</param>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The loaded configuration, or a fresh default <see cref="StickyDockConfiguration"/> on error.</returns>
    public static async Task<StickyDockConfiguration> LoadOrDefaultAsync(
        IConfigurationStore<StickyDockConfiguration> store,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        try
        {
            return await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConfigurationException exception)
        {
            logger?.Warning(
                $"The configuration at '{store.FilePath}' could not be loaded ({exception.Message}); defaults are used.",
                exception);
            return new StickyDockConfiguration();
        }
    }
}
