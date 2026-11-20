using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Services;
using StickyDock.Core.Shortcuts;
using StickyDock.Core.ViewModels;
using StickyDock.Tests.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.ViewModels;

/// <summary>
/// E5-CA1..E5-CA6, HU04 CA #1/#2, Q-01, Q-06: drag &amp; drop validation, icon/metadata resolution, launch error
/// handling, context menu move/remove, internal reordering and persistence across reload.
/// </summary>
public sealed class DockBarViewModelShortcutsTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void DropFilesOnBar_WithLnkFile_AddsItemUpdatesBarItemCountAndPersists()
    {
        var lnkPath = CreateDummyFile("Terminal.lnk");
        var fixture = CreateFixture();
        var layoutChanges = 0;
        fixture.ViewModel.LayoutChanged += (_, _) => layoutChanges++;

        var accepted = fixture.ViewModel.DropFilesOnBar([lnkPath]);

        Assert.True(accepted);
        Assert.Equal(1, fixture.ViewModel.BarItemCount);
        var item = Assert.Single(fixture.ViewModel.BarItems);
        Assert.Equal("Terminal", item.Name);
        Assert.Equal(lnkPath, item.ShortcutPath);
        Assert.NotNull(item.IconPngBytes);
        Assert.Equal(1, layoutChanges);
        Assert.Single(fixture.Document.Current.BarItems);
        Assert.Empty(fixture.Alerts.Warnings);

        var dynamicSize = DockBarMetrics.Default.GetDynamicSize(
            expanded: true,
            DockEdge.Top,
            fixture.ViewModel.RibbonCount,
            fixture.ViewModel.BarItemCount,
            fixture.ViewModel.HasOpenRibbon);
        Assert.Equal(DockBarMetrics.BarActionPadding + DockBarMetrics.BarItemExtent, dynamicSize.WidthDips);
    }

    [Fact]
    public void DropFilesOnBar_WithMultipleLnkFiles_GrowsDynamicBarWidth()
    {
        var paths = Enumerable.Range(1, 3)
            .Select(i => CreateDummyFile($"App{i}.lnk"))
            .ToArray();
        var fixture = CreateFixture();

        fixture.ViewModel.DropFilesOnBar(paths);

        Assert.Equal(3, fixture.ViewModel.BarItemCount);
        var dynamicSize = DockBarMetrics.Default.GetDynamicSize(
            expanded: true,
            DockEdge.Top,
            fixture.ViewModel.RibbonCount,
            fixture.ViewModel.BarItemCount,
            fixture.ViewModel.HasOpenRibbon);
        Assert.Equal(DockBarMetrics.BarActionPadding + 3 * DockBarMetrics.BarItemExtent, dynamicSize.WidthDips);
    }

    [Fact]
    public void DropFilesOnBar_WithExeOrFolderOrTxt_RejectsAndNotifiesUser()
    {
        var exePath = CreateDummyFile("app.exe");
        var txtPath = CreateDummyFile("notes.txt");
        var folderPath = Directory.CreateDirectory(_directory.GetFilePath("MyFolder")).FullName;
        var fixture = CreateFixture();

        var accepted = fixture.ViewModel.DropFilesOnBar([exePath, txtPath, folderPath]);

        Assert.False(accepted);
        Assert.Empty(fixture.ViewModel.BarItems);
        Assert.Equal(0, fixture.ViewModel.BarItemCount);
        Assert.Equal(3, fixture.Alerts.Warnings.Count);
        Assert.All(fixture.Alerts.Warnings, w => Assert.Contains(".lnk", w.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DropFilesOnRibbon_WithMixedLnkAndExe_AddsOnlyLnkAndAlertsOnExe()
    {
        var lnkPath = CreateDummyFile("Wsl.lnk");
        var exePath = CreateDummyFile("setup.exe");
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Dev");

        var accepted = fixture.ViewModel.DropFilesOnRibbon(ribbon.Id, [lnkPath, exePath]);

        Assert.True(accepted);
        Assert.Equal(1, ribbon.ItemCount);
        Assert.Equal("Wsl", ribbon.ShortcutItems[0].Name);
        Assert.Single(fixture.Alerts.Warnings);
        Assert.Single(fixture.Document.Current.Ribbons[0].Items);
    }

    [Fact]
    public void DragEnterTab_ArmsAndExpandsBar_AndDropFilesOnBarAddsShortcut()
    {
        var lnkPath = CreateDummyFile("Ubuntu.lnk");
        var scheduler = new ManualScheduler();
        var fixture = CreateFixture(scheduler: scheduler);

        Assert.Equal(DockState.Tab, fixture.Controller.State);

        fixture.ViewModel.EnterRegionCommand.Execute(DockRegion.Tab);

        Assert.Equal(DockState.Arming, fixture.Controller.State);

        scheduler.Advance(InteractionTimings.ActivationMilliseconds);

        Assert.Equal(DockState.Expanded, fixture.Controller.State);

        fixture.ViewModel.DropFilesOnBarCommand.Execute(new[] { lnkPath });

        var item = Assert.Single(fixture.ViewModel.BarItems);
        Assert.Equal("Ubuntu", item.Name);
        Assert.Equal(lnkPath, item.ShortcutPath);
    }

    [Fact]
    public void LaunchShortcut_WhenLauncherFails_ShowsErrorAlertWithoutThrowing()
    {
        var lnkPath = CreateDummyFile("Broken.lnk");
        var fixture = CreateFixture(launchResult: new ShortcutLaunchResult(false, "Target executable missing."));
        fixture.ViewModel.DropFilesOnBar([lnkPath]);

        fixture.ViewModel.BarItems[0].LaunchCommand.Execute(null);

        var error = Assert.Single(fixture.Alerts.Errors);
        Assert.Contains("Target executable missing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveShortcutToRibbon_AndMoveShortcutToBar_UpdatesContainersAndPersists()
    {
        var lnkPath = CreateDummyFile("Code.lnk");
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Tools");
        fixture.ViewModel.DropFilesOnBar([lnkPath]);
        var item = fixture.ViewModel.BarItems[0];

        item.MoveToRibbonCommand.Execute(ribbon.Id);

        Assert.Empty(fixture.ViewModel.BarItems);
        Assert.Equal(0, fixture.ViewModel.BarItemCount);
        Assert.Single(ribbon.ShortcutItems);
        Assert.Equal(ribbon.Id, item.ContainerRibbonId);
        Assert.True(item.IsInRibbon);
        Assert.False(item.MoveToRibbonCommand.CanExecute(ribbon.Id));
        Assert.True(item.MoveToBarCommand.CanExecute(null));
        Assert.Empty(fixture.Document.Current.BarItems);
        Assert.Single(fixture.Document.Current.Ribbons[0].Items);

        item.MoveToBarCommand.Execute(null);

        Assert.Single(fixture.ViewModel.BarItems);
        Assert.Empty(ribbon.ShortcutItems);
        Assert.Null(item.ContainerRibbonId);
        Assert.False(item.IsInRibbon);
        Assert.Single(fixture.Document.Current.BarItems);
        Assert.Empty(fixture.Document.Current.Ribbons[0].Items);
    }

    [Fact]
    public void RemoveShortcut_FromBarAndRibbon_RemovesAndPersists()
    {
        var barLnk = CreateDummyFile("BarApp.lnk");
        var ribbonLnk = CreateDummyFile("RibApp.lnk");
        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Work");
        fixture.ViewModel.DropFilesOnBar([barLnk]);
        fixture.ViewModel.DropFilesOnRibbon(ribbon.Id, [ribbonLnk]);

        fixture.ViewModel.BarItems[0].RemoveCommand.Execute(null);
        ribbon.ShortcutItems[0].RemoveCommand.Execute(null);

        Assert.Empty(fixture.ViewModel.BarItems);
        Assert.Empty(ribbon.ShortcutItems);
        Assert.Empty(fixture.Document.Current.BarItems);
        Assert.Empty(fixture.Document.Current.Ribbons[0].Items);
    }

    [Fact]
    public void ReorderBarItems_AndReorderRibbonItems_PersistsAcrossReload()
    {
        var b1 = CreateDummyFile("One.lnk");
        var b2 = CreateDummyFile("Two.lnk");
        var r1 = CreateDummyFile("Alpha.lnk");
        var r2 = CreateDummyFile("Beta.lnk");

        var fixture = CreateFixture();
        var ribbon = fixture.ViewModel.CreateRibbon("Dev");
        fixture.ViewModel.DropFilesOnBar([b1, b2]);
        fixture.ViewModel.DropFilesOnRibbon(ribbon.Id, [r1, r2]);

        var firstBarItem = fixture.ViewModel.BarItems[0];
        var secondBarItem = fixture.ViewModel.BarItems[1];
        fixture.ViewModel.ReorderBarItemsCommand.Execute(new ShortcutReorderRequest(firstBarItem.Id, secondBarItem.Id));

        var firstRibItem = ribbon.ShortcutItems[0];
        var secondRibItem = ribbon.ShortcutItems[1];
        ribbon.ReorderItemsCommand.Execute(new ShortcutReorderRequest(firstRibItem.Id, secondRibItem.Id));

        Assert.Equal(["Two", "One"], fixture.ViewModel.BarItems.Select(i => i.Name).ToArray());
        Assert.Equal([0, 1], fixture.ViewModel.BarItems.Select(i => i.Order).ToArray());
        Assert.Equal(["Beta", "Alpha"], ribbon.ShortcutItems.Select(i => i.Name).ToArray());
        Assert.Equal([0, 1], ribbon.ShortcutItems.Select(i => i.Order).ToArray());

        // Simulate app restart by creating a new DockBarViewModel from the same persisted document.
        var reloaded = fixture.CreateReloadedViewModel();
        Assert.Equal(["Two", "One"], reloaded.BarItems.Select(i => i.Name).ToArray());
        Assert.Equal(["Beta", "Alpha"], reloaded.Ribbons[0].ShortcutItems.Select(i => i.Name).ToArray());
    }

    private string CreateDummyFile(string fileName)
    {
        var path = _directory.GetFilePath(fileName);
        File.WriteAllText(path, "dummy");
        return path;
    }

    private TestFixture CreateFixture(ShortcutLaunchResult? launchResult = null, ManualScheduler? scheduler = null)
    {
        var logger = new RecordingLogger();
        var store = new RecordingConfigurationStore();
        var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);
        var controller = new DockInteractionController(scheduler ?? new ManualScheduler(), document, logger);
        var reader = new FakeShellLinkReader();
        var launcher = new FakeShortcutLauncher(launchResult ?? new ShortcutLaunchResult(true));
        var alerts = new RecordingUserAlertService();

        var vm = new DockBarViewModel(
            logger,
            controller,
            controller,
            themeService: null,
            document,
            deletionConfirmer: null,
            reader,
            launcher,
            alerts);

        return new TestFixture(vm, document, logger, controller, reader, launcher, alerts);
    }

    private sealed record TestFixture(
        DockBarViewModel ViewModel,
        ConfigurationDocument Document,
        RecordingLogger Logger,
        DockInteractionController Controller,
        FakeShellLinkReader Reader,
        FakeShortcutLauncher Launcher,
        RecordingUserAlertService Alerts)
    {
        public DockBarViewModel CreateReloadedViewModel()
            => new(
                Logger,
                Controller,
                Controller,
                themeService: null,
                Document,
                deletionConfirmer: null,
                Reader,
                Launcher,
                Alerts);
    }

    private sealed class FakeShellLinkReader : IShellLinkReader
    {
        public ResolvedShortcut Read(string shortcutPath)
            => new(
                shortcutPath,
                Path.GetFileNameWithoutExtension(shortcutPath),
                @"C:\Windows\System32\cmd.exe",
                "/c echo test",
                @"C:\Windows\System32",
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    }

    private sealed class FakeShortcutLauncher(ShortcutLaunchResult result) : IShortcutLauncher
    {
        public ShortcutLaunchResult Launch(string shortcutPath) => result;
    }

    private sealed class RecordingUserAlertService : IUserAlertService
    {
        public List<(string Title, string Message)> Warnings { get; } = [];
        public List<(string Title, string Message)> Errors { get; } = [];

        public void ShowWarning(string title, string message) => Warnings.Add((title, message));
        public void ShowError(string title, string message) => Errors.Add((title, message));
    }
}
