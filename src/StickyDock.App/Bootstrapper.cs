using System.Globalization;
using StickyDock.App.Views;
using StickyDock.Core.Configuration;
using StickyDock.Core.DependencyInjection;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Services;
using StickyDock.Core.ViewModels;
using StickyDock.Interop.Theme;
using StickyDock.Interop.Win32;

namespace StickyDock.App;

/// <summary>
/// Composition root of the application: builds the services, loads the configuration and creates the shell window.
/// </summary>
internal sealed class Bootstrapper : IDisposable
{
    private readonly ServiceContainer _services;
    private readonly RotatingFileLogger _logger;

    private Bootstrapper(ServiceContainer services, RotatingFileLogger logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Wires the application and creates the configuration file on first run.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels the startup I/O.</param>
    /// <returns>The initialized bootstrapper.</returns>
    public static async Task<Bootstrapper> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var paths = ApplicationPaths.ForCurrentUser();
        var logger = new RotatingFileLogger(new RotatingFileLoggerOptions { DirectoryPath = paths.LogDirectory });
        var services = new ServiceContainer();

        try
        {
            services.AddSingleton<ILogger>(logger);
            services.AddSingleton(paths);

            var store = new JsonConfigurationStore<StickyDockConfiguration>(
                paths.ConfigurationFilePath,
                static () => new StickyDockConfiguration(),
                logger: logger);

            services.AddSingleton<IConfigurationStore<StickyDockConfiguration>>(store);

            var configuration = await LoadConfigurationAsync(store, logger, cancellationToken).ConfigureAwait(false);

            if (!store.Exists)
            {
                await store.SaveAsync(configuration, cancellationToken).ConfigureAwait(false);
                logger.Information($"Initial configuration created at '{store.FilePath}'.");
            }

            services.AddSingleton(new MainViewModel(configuration, logger));

            // E1: theme service and dock state service
            var themeService = new WindowsThemeService();
            services.AddSingleton<IWindowsThemeService>(themeService);

            var dockStateService = new DockStateService(logger);
            services.AddSingleton<IDockStateService>(dockStateService);

            var dockBarViewModel = new DockBarViewModel(logger, dockStateService, themeService);
            services.AddSingleton(dockBarViewModel);

            var primaryScreen = DisplayMetrics.GetPrimaryScreenSize();
            logger.Information(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Sticky Dock Utility started. Primary screen: {primaryScreen.WidthInPixels}x{primaryScreen.HeightInPixels} px."));

            return new Bootstrapper(services, logger);
        }
        catch (Exception exception)
        {
            logger.Critical("Startup failed; the application cannot continue.", exception);
            services.Dispose();
            logger.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Shows the shell window bound to its view model.
    /// </summary>
    public void ShowMainWindow()
    {
        var viewModel = _services.GetRequiredService<MainViewModel>();
        var dockBarViewModel = _services.GetRequiredService<DockBarViewModel>();
        var window = new MainWindow
        {
            DataContext = dockBarViewModel,
            MainViewModel = viewModel
        };
        window.Show();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _services.Dispose();
        _logger.Dispose();
    }

    private static async Task<StickyDockConfiguration> LoadConfigurationAsync(
        IConfigurationStore<StickyDockConfiguration> store,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConfigurationException exception)
        {
            // Baseline recovery for E0: defaults are used and the invalid file is left untouched so no user data is
            // discarded. The definitive policy belongs to E8.
            logger.Error("The configuration could not be loaded; defaults are used.", exception);
            return new StickyDockConfiguration();
        }
    }
}
