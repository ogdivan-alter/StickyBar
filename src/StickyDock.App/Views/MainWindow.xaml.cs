using System.Windows;
using System.Windows.Input;
using StickyDock.Core.ViewModels;

namespace StickyDock.App.Views;

/// <summary>
/// Shell window. Beyond <see cref="InitializeComponent"/> and the two hover handlers carried over from E1 it contains no
/// code on purpose: state, docking, ribbons, timers and animations belong to view models and services (§5.2-01,
/// E0-CA6, H-E2-03).
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    /// <summary>Gets or sets the main view model.</summary>
    public MainViewModel? MainViewModel { get; set; }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is DockBarViewModel viewModel)
        {
            viewModel.IsHovered = true;
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is DockBarViewModel viewModel)
        {
            viewModel.IsHovered = false;
        }
    }
}
