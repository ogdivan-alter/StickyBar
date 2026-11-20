using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace StickyBar.App;

/// <summary>
/// Application entry point. It only composes the object graph and hosts the window; no shell state or behaviour is
/// implemented here (§5.2-01, E0-CA6).
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\StickyBar.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private Bootstrapper? _bootstrapper;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown(exitCode: 0);
            return;
        }

        // Force software rendering to avoid loading Direct3D driver modules (~26 MB) and keep private memory within budget (RNF-01, Q-12).
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        try
        {
            _bootstrapper = await Bootstrapper.InitializeAsync().ConfigureAwait(true);
            _bootstrapper.ShowMainWindow();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"StickyBar could not start.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "StickyBar",
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

        if (_singleInstanceMutex is not null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Ignored if the mutex was not owned by the exiting thread.
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        base.OnExit(e);
    }
}
