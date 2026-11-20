using StickyBar.Core.Configuration;
using StickyBar.Core.Docking;
using StickyBar.Core.Interaction;
using StickyBar.Core.Ribbons;
using StickyBar.Core.Services;
using StickyBar.Core.Shortcuts;
using StickyBar.Core.ViewModels;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.ViewModels;

/// <summary>
/// E7-CA1, E7-CA2, R-04: multiple <see cref="DockBarViewModel"/> instances sharing one
/// <see cref="IConfigurationDocument"/> stay mirrored in real time without write-back loops.
/// </summary>
public sealed class DockBarViewModelMirrorSyncTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void RibbonCrudAndReorder_OnPrimaryBar_MirrorsImmediatelyToReplicaWithExactOrder()
    {
        var logger = new RecordingLogger();
        var store = StickyBarSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyBarConfiguration(), logger);

        using var primary = CreateViewModel(document, logger);
        using var replica = CreateViewModel(document, logger);

        // Create two ribbons on primary.
        primary.CreateRibbon("Dev");
        primary.CreateRibbon("Media");

        Assert.Equal(["Dev", "Media"], replica.Ribbons.Select(r => r.Name));

        // Rename second ribbon on primary.
        Assert.True(primary.RenameRibbon(primary.Ribbons[1].Id, "Games"));
        Assert.Equal(["Dev", "Games"], replica.Ribbons.Select(r => r.Name));

        // Reorder ribbons on primary (move Games before Dev).
        primary.Ribbons[1].MoveEarlierCommand.Execute(null);
        Assert.Equal(["Games", "Dev"], replica.Ribbons.Select(r => r.Name));

        // Delete Dev from primary.
        primary.Ribbons[1].DeleteCommand.Execute(null);
        Assert.Equal(["Games"], replica.Ribbons.Select(r => r.Name));
    }

    [Fact]
    public void ShortcutAddMoveReorderAndRemove_OnPrimaryBar_MirrorsImmediatelyToReplica()
    {
        var logger = new RecordingLogger();
        var store = StickyBarSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyBarConfiguration(), logger);

        var lnk1 = CreateDummyLnk("Alpha.lnk");
        var lnk2 = CreateDummyLnk("Beta.lnk");

        using var primary = CreateViewModel(document, logger);
        using var replica = CreateViewModel(document, logger);

        // Add two shortcuts to main bar on primary.
        primary.DropFilesOnBar([lnk1, lnk2]);
        Assert.Equal(["Alpha", "Beta"], replica.BarItems.Select(i => i.Name));

        // Reorder shortcuts on primary (move Beta before Alpha).
        primary.ReorderBarItems(primary.BarItems[1].Id, primary.BarItems[0].Id);
        Assert.Equal(["Beta", "Alpha"], replica.BarItems.Select(i => i.Name));

        // Create a ribbon and move Alpha into it from replica (bidirectional single source of truth).
        replica.CreateRibbon("Work");
        Assert.Equal(["Work"], primary.Ribbons.Select(r => r.Name));

        var alphaOnReplica = replica.BarItems[1];
        alphaOnReplica.MoveToRibbonCommand.Execute(replica.Ribbons[0].Id);

        Assert.Equal(["Beta"], primary.BarItems.Select(i => i.Name));
        Assert.Equal(["Alpha"], primary.Ribbons[0].ShortcutItems.Select(i => i.Name));

        // Remove Alpha from primary ribbon; replica reflects removal.
        primary.Ribbons[0].ShortcutItems[0].RemoveCommand.Execute(null);
        Assert.Empty(replica.Ribbons[0].ShortcutItems);
    }

    [Fact]
    public void NonContentConfigurationChange_DoesNotRebuildOrCloseOpenRibbon()
    {
        var logger = new RecordingLogger();
        var store = StickyBarSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(
            store,
            new StickyBarConfiguration
            {
                Ribbons = [new Ribbon { Id = "r1", Name = "Dev", Order = 0, Items = [] }],
            },
            logger);

        var stateService = new FakeDockStateService();
        stateService.SetState(DockState.Expanded);
        using var replica = new DockBarViewModel(
            logger,
            stateService,
            new FakeDockRegionSink(),
            themeService: null,
            document,
            deletionConfirmer: null);

        replica.Ribbons[0].ToggleCommand.Execute(null);
        Assert.True(replica.HasOpenRibbon);

        var layoutChangedCount = 0;
        replica.LayoutChanged += (_, _) => layoutChangedCount++;

        document.Update(doc => doc with { GracePeriodMilliseconds = 480, DockEdge = DockEdge.Left });

        Assert.Equal(0, layoutChangedCount);
        Assert.True(replica.HasOpenRibbon);
    }

    [Fact]
    public void Dispose_UnsubscribesFromConfigurationChanges()
    {
        var logger = new RecordingLogger();
        var store = StickyBarSettings.CreateStore(_directory.GetFilePath("settings.json"), logger);
        using var document = new ConfigurationDocument(store, new StickyBarConfiguration(), logger);

        using var primary = CreateViewModel(document, logger);
        var replica = CreateViewModel(document, logger);

        replica.Dispose();

        primary.CreateRibbon("AfterDispose");
        Assert.Empty(replica.Ribbons);
    }

    private string CreateDummyLnk(string fileName)
    {
        var path = _directory.GetFilePath(fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private static DockBarViewModel CreateViewModel(IConfigurationDocument document, RecordingLogger logger)
        => new(
            logger,
            new FakeDockStateService(),
            new FakeDockRegionSink(),
            themeService: null,
            document,
            new AlwaysConfirmRibbonDeletion(),
            new StubShellLinkReader(),
            new StubShortcutLauncher(),
            new StubUserAlertService());

    private sealed class AlwaysConfirmRibbonDeletion : IRibbonDeletionConfirmer
    {
        public bool ConfirmDeleteRibbon(string ribbonName, int itemCount) => true;
    }

    private sealed class StubShellLinkReader : IShellLinkReader
    {
        public ResolvedShortcut Read(string shortcutPath)
            => new(
                shortcutPath,
                Path.GetFileNameWithoutExtension(shortcutPath),
                @"C:\Windows\System32\cmd.exe",
                string.Empty,
                string.Empty,
                IconPngBytes: null);
    }

    private sealed class StubShortcutLauncher : IShortcutLauncher
    {
        public ShortcutLaunchResult Launch(string shortcutPath) => new(true);
    }

    private sealed class StubUserAlertService : IUserAlertService
    {
        public void ShowWarning(string title, string message) { }

        public void ShowError(string title, string message) { }
    }
}
