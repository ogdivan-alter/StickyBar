using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using StickyDock.Core.ViewModels;
using StickyDock.Interop.Win32;

namespace StickyDock.App.Views;

/// <summary>
/// Shell window. Beyond <see cref="InitializeComponent"/> it contains no code on purpose: state, docking, ribbons,
/// timers and animations belong to view models and services (§5.2-01, E0-CA6).
/// </summary>
public partial class MainWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    /// <summary>Gets or sets the main view model.</summary>
    public MainViewModel? MainViewModel { get; set; }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        ApplyDarkModeIfNeeded(handle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Position in work area - default to top center area for now
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var workArea = WindowHelpers.GetWorkArea(handle);
            Left = workArea.Left + (workArea.Width - Width) / 2.0;
            Top = workArea.Top + 8;
        }
        catch
        {
            // Ignore positioning errors
        }
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is DockBarViewModel vm)
        {
            vm.IsHovered = true;
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is DockBarViewModel vm)
        {
            vm.IsHovered = false;
        }
    }

    private static void ApplyDarkModeIfNeeded(nint hwnd)
    {
        try
        {
            var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            int useDark = 0; // let system decide based on theme
            // Try to set; ignore failures
            _ = DwmSetWindowAttribute(hwnd, attribute, ref useDark, sizeof(int));
        }
        catch
        {
            // Ignore
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);
}
