using Microsoft.Win32;
using StickyDock.Interop.Win32;

namespace StickyDock.Tests.Interop;

/// <summary>
/// E6-CA4 (§4.6-03): verifies <see cref="WindowsAutoStartService"/> against an isolated temporary HKCU subkey
/// that is deleted on disposal so the user's real startup registry is never touched.
/// </summary>
public sealed class WindowsAutoStartServiceTests : IDisposable
{
    private readonly string _testSubKeyPath = $@"Software\StickyDock\Tests\AutoStart_{Guid.NewGuid():N}";

    [Fact]
    public void IsEnabled_WhenRegistrySubKeyDoesNotExist_ReturnsFalse()
    {
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyDockTest",
            () => @"C:\Program Files\StickyDock\StickyDock.exe");

        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_TrueThenFalse_WritesAndRemovesRegistryValue()
    {
        const string exePath = @"C:\Program Files\StickyDock\StickyDock.exe";
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyDockTest",
            () => exePath);

        service.SetEnabled(true);

        Assert.True(service.IsEnabled());

        using (var key = Registry.CurrentUser.OpenSubKey(_testSubKeyPath, writable: false))
        {
            Assert.NotNull(key);
            Assert.Equal($"\"{exePath}\"", key.GetValue("StickyDockTest"));
        }

        service.SetEnabled(false);

        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_FalseWhenMissing_DoesNotThrow()
    {
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyDockTest",
            () => @"C:\Program Files\StickyDock\StickyDock.exe");

        service.SetEnabled(false);

        Assert.False(service.IsEnabled());
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_testSubKeyPath, throwOnMissingSubKey: false);
    }
}
