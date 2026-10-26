using System.Windows;

namespace StickyDock.App;

/// <summary>
/// Application entry point. It only composes the object graph and hosts the window; no shell state or behaviour is
/// implemented here (§5.2-01, E0-CA6).
/// </summary>
public partial class App : Application
{
    private Bootstrapper? _bootstrapper;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _bootstrapper = await Bootstrapper.InitializeAsync().ConfigureAwait(true);
            _bootstrapper.ShowMainWindow();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Sticky Dock Utility could not start.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "Sticky Dock Utility",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(exitCode: 1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper?.Dispose();
        _bootstrapper = null;

        base.OnExit(e);
    }
}
