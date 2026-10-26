using System.Windows;

namespace StickyDock.App.Views;

/// <summary>
/// Shell window. Beyond <see cref="InitializeComponent"/> it contains no code on purpose: state, docking, ribbons,
/// timers and animations belong to view models and services (§5.2-01, E0-CA6).
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() => InitializeComponent();
}
