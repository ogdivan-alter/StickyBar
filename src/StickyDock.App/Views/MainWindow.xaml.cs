using System.Windows;
using StickyDock.Core.ViewModels;

namespace StickyDock.App.Views;

/// <summary>
/// Shell window. It contains no code on purpose beyond <see cref="InitializeComponent"/> and the main view model: state,
/// docking, ribbons, timers and animations belong to view models and services (§5.2-01, E0-CA6, ADR-003 D5).
/// </summary>
/// <remarks>
/// The pointer regions, the drag gesture and the animations of E3 are attached declaratively in the XAML, so the window
/// has no event handler of its own.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the main view model.</summary>
    public MainViewModel? MainViewModel { get; set; }
}
