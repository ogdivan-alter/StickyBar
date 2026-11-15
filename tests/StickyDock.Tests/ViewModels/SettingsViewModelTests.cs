using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Services;
using StickyDock.Core.ViewModels;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.ViewModels;

/// <summary>
/// E6-CA1, E6-CA3, E6-CA4, Q-09: verifies <see cref="SettingsViewModel"/> behaviour, range normalization, immediate
/// application of grace period changes, and persistence.
/// </summary>
public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_InitializesPreferencesFromConfigurationAndAutoStartService()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(
            store,
            new StickyDockConfiguration { GracePeriodMilliseconds = 450, DockEdge = DockEdge.Left },
            logger);
        var autoStart = new FakeAutoStartService(initialEnabled: true);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);

        Assert.Equal(GracePeriod.MinimumMilliseconds, viewModel.GracePeriodMinimum);
        Assert.Equal(GracePeriod.MaximumMilliseconds, viewModel.GracePeriodMaximum);
        Assert.Equal(450, viewModel.GracePeriodMilliseconds);
        Assert.True(viewModel.AutoStartEnabled);
        Assert.Equal("Left", viewModel.DockedEdgeLabel);
    }

    [Fact]
    public async Task GracePeriodMilliseconds_WhenChanged_UpdatesDocumentAndPersistsToDisk()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var filePath = directory.GetFilePath("settings.json");
        var store = StickyDockSettings.CreateStore(filePath, logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);

        viewModel.GracePeriodMilliseconds = 480;
        await document.FlushAsync();

        Assert.Equal(480, document.Current.GracePeriodMilliseconds);

        var reloaded = await store.LoadAsync();
        Assert.Equal(480, reloaded.GracePeriodMilliseconds);
    }

    [Theory]
    [InlineData(100, GracePeriod.MinimumMilliseconds)]
    [InlineData(299, GracePeriod.MinimumMilliseconds)]
    [InlineData(300, 300)]
    [InlineData(425, 425)]
    [InlineData(500, 500)]
    [InlineData(501, GracePeriod.MaximumMilliseconds)]
    [InlineData(1200, GracePeriod.MaximumMilliseconds)]
    public void GracePeriodMilliseconds_NormalizesValuesToAllowedRange(int input, int expected)
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);

        viewModel.GracePeriodMilliseconds = input;

        Assert.Equal(expected, viewModel.GracePeriodMilliseconds);
        Assert.Equal(expected, document.Current.GracePeriodMilliseconds);
    }

    [Fact]
    public void GracePeriodMilliseconds_WhenChanged_TakesEffectImmediatelyOnNextCollapse()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);
        var scheduler = new ManualScheduler();
        using var controller = new DockInteractionController(scheduler, document, logger);
        using var viewModel = new SettingsViewModel(document, autoStart, logger);

        // Deploy the bar first while grace is at the default 400 ms.
        controller.EnterRegion(DockRegion.Tab);
        scheduler.Advance(TimeSpan.FromMilliseconds(InteractionTimings.ActivationMilliseconds));
        Assert.Equal(DockState.Expanded, controller.State);

        // Change the grace period to 500 ms via SettingsViewModel without restarting.
        viewModel.GracePeriodMilliseconds = 500;

        // Leave the bar: the grace timer must use the updated 500 ms immediately (E6-CA3).
        controller.LeaveRegion(DockRegion.Tab);
        scheduler.Advance(TimeSpan.FromMilliseconds(450));
        Assert.Equal(DockState.Expanded, controller.State);

        scheduler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(DockState.Collapsing, controller.State);
    }

    [Fact]
    public void AutoStartEnabled_WhenToggled_UpdatesAutoStartService()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);

        viewModel.AutoStartEnabled = true;
        Assert.True(autoStart.IsEnabled());
        Assert.True(viewModel.AutoStartEnabled);

        viewModel.AutoStartEnabled = false;
        Assert.False(autoStart.IsEnabled());
        Assert.False(viewModel.AutoStartEnabled);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.CloseCommand.Execute(null);

        Assert.True(closed);
    }

    [Fact]
    public void ExternalConfigurationChange_UpdatesDockedEdgeLabel()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);

        using var viewModel = new SettingsViewModel(document, autoStart, logger);
        Assert.Equal("Top", viewModel.DockedEdgeLabel);

        document.Update(current => current with { DockEdge = null });
        Assert.Equal("Floating", viewModel.DockedEdgeLabel);

        document.Update(current => current with { DockEdge = DockEdge.Right });
        Assert.Equal("Right", viewModel.DockedEdgeLabel);
    }

    [Fact]
    public void SecondaryMonitors_WithSinglePrimaryDisplay_ReportsHasNoSecondaryMonitors()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);
        var catalog = new FakeMonitorCatalog(
            new DisplayMonitorInfo(@"\\.\DISPLAY1", true, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96));

        using var viewModel = new SettingsViewModel(document, autoStart, logger, catalog);

        Assert.Empty(viewModel.SecondaryMonitors);
        Assert.False(viewModel.HasSecondaryMonitors);
        Assert.True(viewModel.HasNoSecondaryMonitors);
    }

    [Fact]
    public async Task SecondaryMonitors_WhenToggled_UpdatesDocumentImmediatelyAndPersists()
    {
        using var directory = new TemporaryDirectory();
        var logger = new RecordingLogger();
        var filePath = directory.GetFilePath("settings.json");
        var store = StickyDockSettings.CreateStore(filePath, logger);
        using var document = new ConfigurationDocument(
            store,
            new StickyDockConfiguration { EnabledReplicaMonitors = [@"\\.\DISPLAY3"] },
            logger);
        var autoStart = new FakeAutoStartService(initialEnabled: false);
        var catalog = new FakeMonitorCatalog(
            new DisplayMonitorInfo(@"\\.\DISPLAY1", true, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96),
            new DisplayMonitorInfo(@"\\.\DISPLAY2", false, new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1040), 96),
            new DisplayMonitorInfo(@"\\.\DISPLAY3", false, new PixelRect(3840, 0, 1920, 1080), new PixelRect(3840, 0, 1920, 1040), 120));

        using var viewModel = new SettingsViewModel(document, autoStart, logger, catalog);

        Assert.True(viewModel.HasSecondaryMonitors);
        Assert.False(viewModel.HasNoSecondaryMonitors);
        Assert.Equal(2, viewModel.SecondaryMonitors.Count);
        Assert.False(viewModel.SecondaryMonitors[0].IsEnabled);
        Assert.True(viewModel.SecondaryMonitors[1].IsEnabled);

        viewModel.SecondaryMonitors[0].IsEnabled = true;
        await document.FlushAsync();

        Assert.Contains(@"\\.\DISPLAY2", document.Current.EnabledReplicaMonitors);
        Assert.Contains(@"\\.\DISPLAY3", document.Current.EnabledReplicaMonitors);

        var reloaded = await store.LoadAsync();
        Assert.Contains(@"\\.\DISPLAY2", reloaded.EnabledReplicaMonitors);
    }

    private sealed class FakeMonitorCatalog : IMonitorCatalog
    {
        private readonly DisplayMonitorInfo[] _monitors;

        public FakeMonitorCatalog(params DisplayMonitorInfo[] monitors) => _monitors = monitors;

        public IReadOnlyList<DisplayMonitorInfo> GetMonitors() => _monitors;
    }

    private sealed class FakeAutoStartService : IAutoStartService
    {
        private bool _enabled;

        public FakeAutoStartService(bool initialEnabled) => _enabled = initialEnabled;

        public bool IsEnabled() => _enabled;

        public void SetEnabled(bool enabled) => _enabled = enabled;
    }
}
