using System.Windows;

namespace StickyBar.App.Views;

/// <summary>
/// General settings window. Contains no state or persistence logic beyond <see cref="InitializeComponent"/> (§5.2-01).
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }
}
