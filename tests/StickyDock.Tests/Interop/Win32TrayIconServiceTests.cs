using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// ADR-006, E6-CA1, E6-CA2, A2: verifies <see cref="Win32TrayIconService"/> visibility lifecycle,
/// callback message handling, menu commands, and idempotent disposal without leaving a ghost icon.
/// </summary>
public sealed class Win32TrayIconServiceTests
{
    [Fact]
    public void ShowAndDispose_TogglesVisibilityCleanly_AndDisposeIsIdempotent()
    {
        var service = new Win32TrayIconService();
        Assert.False(service.IsVisible);
        Assert.False(service.IsDisposed);

        service.Show();
        Assert.True(service.IsVisible);

        // Calling Show again while already visible should remain a safe no-op.
        service.Show();
        Assert.True(service.IsVisible);

        service.Dispose();
        Assert.False(service.IsVisible);
        Assert.True(service.IsDisposed);

        // Idempotent second Dispose must not throw.
        service.Dispose();
        Assert.True(service.IsDisposed);

        Assert.Throws<ObjectDisposedException>(() => service.Show());
    }

    [Fact]
    public void ContextMenu_ContainsSettingsAndExit()
    {
        using var service = new Win32TrayIconService();

        Assert.Equal(new[] { "Settings", "Exit" }, service.MenuItemTexts);
    }

    [Fact]
    public void LeftButtonUp_RaisesSettingsRequested_AndRightButtonUpDoesNotDirectly()
    {
        using var service = new Win32TrayIconService();
        var settingsCount = 0;
        var exitCount = 0;
        service.SettingsRequested += (_, _) => settingsCount++;
        service.ExitRequested += (_, _) => exitCount++;

        // Simulate WM_RBUTTONUP where the user cancels the popup menu (returns 0).
        service.SimulateTrayMessage(Win32TrayIconService.WmRButtonUp, () => 0);
        Assert.Equal(0, settingsCount);
        Assert.Equal(0, exitCount);

        // Simulate WM_LBUTTONUP: raises SettingsRequested directly.
        service.SimulateTrayMessage(Win32TrayIconService.WmLButtonUp);
        Assert.Equal(1, settingsCount);
        Assert.Equal(0, exitCount);
    }

    [Fact]
    public void MenuCommands_RaiseSettingsRequestedAndExitRequested()
    {
        using var service = new Win32TrayIconService();
        var settingsCount = 0;
        var exitCount = 0;
        service.SettingsRequested += (_, _) => settingsCount++;
        service.ExitRequested += (_, _) => exitCount++;

        service.SimulateMenuCommand(Win32TrayIconService.SettingsMenuCommandId);
        Assert.Equal(1, settingsCount);
        Assert.Equal(0, exitCount);

        service.SimulateMenuCommand(Win32TrayIconService.ExitMenuCommandId);
        Assert.Equal(1, settingsCount);
        Assert.Equal(1, exitCount);

        // Unknown/cancelled command ID (0) raises neither event.
        service.SimulateMenuCommand(0);
        Assert.Equal(1, settingsCount);
        Assert.Equal(1, exitCount);
    }
}
