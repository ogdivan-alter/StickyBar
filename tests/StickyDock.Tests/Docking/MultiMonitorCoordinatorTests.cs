using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Docking;

/// <summary>
/// E7-CA4, E7-CA5: verifies <see cref="MultiMonitorCoordinator"/> lifecycle on monitor connect, disconnect, reconnect
/// and settings toggle.
/// </summary>
public sealed class MultiMonitorCoordinatorTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Start_WithOnlyPrimaryOrNoEnabledSecondaries_CreatesNoReplicas()
    {
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var catalog = new FakeMonitorCatalog(
            new DisplayMonitorInfo(@"\\.\DISPLAY1", true, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96),
            new DisplayMonitorInfo(@"\\.\DISPLAY2", false, new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1040), 96));
        var factory = new FakeBarHostFactory();

        using var coordinator = new MultiMonitorCoordinator(catalog, document, factory, logger);
        coordinator.Start();

        Assert.Empty(coordinator.ActiveReplicaDeviceNames);
        Assert.Empty(factory.CreatedHosts);
    }

    [Fact]
    public void DisconnectAndReconnectSecondaryMonitor_ClosesAndRestoresReplicaWithoutLosingConfig()
    {
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(
            store,
            new StickyDockConfiguration { EnabledReplicaMonitors = [@"\\.\DISPLAY2"] },
            logger);

        var primary = new DisplayMonitorInfo(@"\\.\DISPLAY1", true, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96);
        var secondary = new DisplayMonitorInfo(@"\\.\DISPLAY2", false, new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1040), 120);

        var catalog = new FakeMonitorCatalog(primary, secondary);
        var factory = new FakeBarHostFactory();

        using var coordinator = new MultiMonitorCoordinator(catalog, document, factory, logger);
        coordinator.Start();

        // Replica created initially.
        Assert.Equal([@"\\.\DISPLAY2"], coordinator.ActiveReplicaDeviceNames);
        var firstHost = Assert.Single(factory.CreatedHosts);
        Assert.False(firstHost.IsDisposed);

        // E7-CA4: monitor disconnected -> replica closed & disposed, config preserved.
        catalog.SetMonitors(primary);
        coordinator.NotifyDisplayChanged();

        Assert.Empty(coordinator.ActiveReplicaDeviceNames);
        Assert.True(firstHost.IsDisposed);
        Assert.Equal([@"\\.\DISPLAY2"], document.Current.EnabledReplicaMonitors);

        // E7-CA5: monitor reconnected -> replica automatically restored.
        catalog.SetMonitors(primary, secondary);
        coordinator.NotifyDisplayChanged();

        Assert.Equal([@"\\.\DISPLAY2"], coordinator.ActiveReplicaDeviceNames);
        Assert.Equal(2, factory.CreatedHosts.Count);
        Assert.False(factory.CreatedHosts[1].IsDisposed);
    }

    [Fact]
    public void ConfigurationToggle_OpensAndClosesReplicaImmediately()
    {
        var logger = new RecordingLogger();
        var store = StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);

        var primary = new DisplayMonitorInfo(@"\\.\DISPLAY1", true, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96);
        var secondary = new DisplayMonitorInfo(@"\\.\DISPLAY2", false, new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1040), 96);

        var catalog = new FakeMonitorCatalog(primary, secondary);
        var factory = new FakeBarHostFactory();

        using var coordinator = new MultiMonitorCoordinator(catalog, document, factory, logger);
        coordinator.Start();
        Assert.Empty(coordinator.ActiveReplicaDeviceNames);

        document.Update(doc => doc with { EnabledReplicaMonitors = [@"\\.\DISPLAY2"] });
        Assert.Equal([@"\\.\DISPLAY2"], coordinator.ActiveReplicaDeviceNames);
        var host = Assert.Single(factory.CreatedHosts);
        Assert.False(host.IsDisposed);

        document.Update(doc => doc with { EnabledReplicaMonitors = [] });
        Assert.Empty(coordinator.ActiveReplicaDeviceNames);
        Assert.True(host.IsDisposed);
    }

    private sealed class FakeMonitorCatalog : IMonitorCatalog
    {
        private DisplayMonitorInfo[] _monitors;

        public FakeMonitorCatalog(params DisplayMonitorInfo[] monitors) => _monitors = monitors;

        public void SetMonitors(params DisplayMonitorInfo[] monitors) => _monitors = monitors;

        public IReadOnlyList<DisplayMonitorInfo> GetMonitors() => _monitors;
    }

    private sealed class FakeBarHostFactory : IBarHostFactory
    {
        public List<FakeBarHost> CreatedHosts { get; } = [];

        public IBarHost CreateReplica(DisplayMonitorInfo monitor)
        {
            var host = new FakeBarHost(monitor.DeviceName);
            CreatedHosts.Add(host);
            return host;
        }
    }

    private sealed class FakeBarHost : IBarHost
    {
        public FakeBarHost(string deviceName) => DeviceName = deviceName;

        public string DeviceName { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
