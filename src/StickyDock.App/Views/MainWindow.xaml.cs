using System.Windows;
using StickyDock.Core.ViewModels;

namespace StickyDock.App.Views;

/// <summary>
/// Shell window. It contains no code on purpose beyond <see cref="InitializeComponent"/> and the main view model: the
/// state of the bar, the docking, the ribbons, the timing and the animations belong to view models and services
/// (§5.2-01, E0-CA6).
/// </summary>
/// <remarks>
/// Animation is the one visual concern the view owns, and only as a declaration: ADR-003 D5 keeps it declarative and out
/// of the code behind, while ADR-004 D4 governs when a storyboard releases the property it animates. The pointer
/// regions, the drag gesture and the transitions of E3 are attached in the XAML, so the window has no event handler of
/// its own.
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
