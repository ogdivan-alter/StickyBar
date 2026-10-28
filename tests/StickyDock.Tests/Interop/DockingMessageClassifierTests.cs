using StickyDock.Core.Docking;
using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// Only four window messages concern the docking, and <c>WM_SETTINGCHANGE</c> only when its <c>wParam</c> announces a
/// work area change. Classifying them here keeps the decision out of the <c>HwndSource</c> (ADR-002, H-E2-04).
/// </summary>
public sealed class DockingMessageClassifierTests
{
    [Fact]
    public void SettingChangeWithTheWorkAreaParameter_IsAWorkAreaChange()
    {
        var recognised = DockingMessageClassifier.TryClassify(
            DockingMessageClassifier.WindowSettingChange,
            DockingMessageClassifier.SetWorkArea,
            out var kind);

        Assert.True(recognised);
        Assert.Equal(DockingEventKind.WorkAreaChanged, kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x001A)]
    [InlineData(0x002A)]
    public void SettingChangeWithAnotherParameter_IsIgnored(int wParam)
    {
        // Windows broadcasts WM_SETTINGCHANGE for many preferences; only SPI_SETWORKAREA moves the taskbar.
        var recognised = DockingMessageClassifier.TryClassify(
            DockingMessageClassifier.WindowSettingChange,
            wParam,
            out _);

        Assert.False(recognised);
    }

    [Fact]
    public void DisplayChange_IsADisplayChange()
    {
        Assert.Equal(
            DockingEventKind.DisplayChanged,
            Classify(DockingMessageClassifier.DisplayChange));
    }

    [Fact]
    public void DpiChange_IsADisplayChange()
    {
        Assert.Equal(
            DockingEventKind.DisplayChanged,
            Classify(DockingMessageClassifier.DpiChanged));
    }

    [Fact]
    public void ExitSizeMove_IsTheEndOfTheDrag()
    {
        Assert.Equal(
            DockingEventKind.WindowDragCompleted,
            Classify(DockingMessageClassifier.ExitSizeMove));
    }

    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x0001)]
    [InlineData(0x0047)]
    [InlineData(0x0200)]
    public void AnyOtherMessage_IsIgnored(int message)
    {
        // WM_CREATE, WM_WINDOWPOSCHANGED and WM_MOUSEMOVE are noise for the docking.
        var recognised = DockingMessageClassifier.TryClassify(message, 0, out _);

        Assert.False(recognised);
    }

    private static DockingEventKind Classify(int message)
    {
        var recognised = DockingMessageClassifier.TryClassify(message, 0, out var kind);

        Assert.True(recognised);

        return kind;
    }
}
