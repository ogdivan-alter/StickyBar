using System.Globalization;
using StickyDock.Core.Configuration;
using StickyDock.Core.ViewModels;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.ViewModels;

/// <summary>
/// The shell view model exposes configuration state without any WPF dependency (§5.2-01, E0-CA6).
/// </summary>
public sealed class MainViewModelTests
{
    [Fact]
    public void Constructor_ExposesTheLoadedSchemaVersion()
    {
        var viewModel = new MainViewModel(new StickyDockConfiguration(), new RecordingLogger());

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, viewModel.ConfigurationSchemaVersion);
        Assert.Contains(
            string.Create(CultureInfo.InvariantCulture, $"Configuration schema v{StickyDockConfiguration.CurrentSchemaVersion}"),
            viewModel.StatusText,
            StringComparison.Ordinal);
    }

    [Fact]
    public void StatusText_WhenChanged_RaisesPropertyChanged()
    {
        var viewModel = new MainViewModel(new StickyDockConfiguration(), new RecordingLogger());
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raisedProperties.Add(arguments.PropertyName);

        viewModel.StatusText = "Ready";

        Assert.Equal(new[] { nameof(MainViewModel.StatusText) }, raisedProperties);
        Assert.Equal("Ready", viewModel.StatusText);
    }

    [Fact]
    public void Constructor_LogsTheCreationOfTheShell()
    {
        var logger = new RecordingLogger();

        _ = new MainViewModel(new StickyDockConfiguration(), logger);

        Assert.Single(logger.Entries);
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, new RecordingLogger()));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(new StickyDockConfiguration(), null!));
    }
}
