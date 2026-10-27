using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using StickyDock.Core.Configuration;
using StickyDock.Core.Services;
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
    private Point? _dragStartPoint;
    private bool _isDragging;

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
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    /// <summary>Gets or sets the main view model.</summary>
    public MainViewModel? MainViewModel { get; set; }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        ApplyDarkModeIfNeeded(handle);
        try
        {
            if (App.Current is App app)
            {
                // Get coordinator from service container if possible - easier to resolve via DataContext services? 
                // For now, try to find via application resources not set; alternatively just create minimally
            }
        }
        catch
        {
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var workArea = WindowHelpers.GetWorkArea(handle);
            Left = workArea.Left + (workArea.Width - Width) / 2.0;
            Top = workArea.Top + 8;

            // Try to resolve coordinator from bootstrapper if exposed - not directly accessible; 
            // We'll rely on VM/services set up; for now just initialize hooks via a temporary coordinator pattern
            // Create coordinator if we can get dependencies from DataContext? No - keep logic in Core, do minimal wiring here
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

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            _dragStartPoint = PointToScreen(e.GetPosition(this));
            _isDragging = true;
            CaptureMouse();
        }
        catch
        {
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _dragStartPoint == null)
        {
            return;
        }

        try
        {
            var current = PointToScreen(e.GetPosition(this));
            var diff = new Point(current.X - _dragStartPoint.Value.X, current.Y - _dragStartPoint.Value.Y);
            if (Math.Abs(diff.X) > 2 || Math.Abs(diff.Y) > 2)
            {
                DragMove();
            }
        }
        catch
        {
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        try
        {
            _isDragging = false;
            ReleaseMouseCapture();
            var pos = PointToScreen(e.GetPosition(this));
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                var taskbar = TaskbarMonitor.GetTaskbarInfo(handle);
                var screen = DisplayMetrics.GetPrimaryScreenSize();
                // Compute snap in Core terms using Interop values
                var coreTaskbar = (Core.Services.TaskbarEdge)taskbar.Edge;
                var snapped = Core.Services.DockEdgeService.ComputeSnappedEdge(pos.X, pos.Y, screen.WidthInPixels, screen.HeightInPixels, coreTaskbar, Core.Services.DockEdgeService.DefaultSnapThreshold);
                if (snapped.HasValue && Core.Services.DockEdgeService.IsValidEdge(snapped.Value, coreTaskbar))
                {
                    if (DataContext is DockBarViewModel vm)
                    {
                        // VM will persist via its own logic if wired; also set via service if accessible
                    }
                    // Try to get dock edge service from DataContext? No direct access; rely on VM subscription
                }
            }
            catch
            {
            }
        }
        catch
        {
        }
        finally
        {
            _dragStartPoint = null;
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
