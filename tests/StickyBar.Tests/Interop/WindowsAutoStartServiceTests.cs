using Microsoft.Win32;
using StickyBar.Interop.Win32;

namespace StickyBar.Tests.Interop;

/// <summary>
/// E6-CA4 (§4.6-03): verifies <see cref="WindowsAutoStartService"/> against an isolated temporary HKCU subkey
/// that is deleted on disposal so the user's real startup registry is never touched.
/// </summary>
public sealed class WindowsAutoStartServiceTests : IDisposable
{
    private readonly string _testSubKeyPath = $@"Software\StickyBar\Tests\AutoStart_{Guid.NewGuid():N}";

    [Fact]
    public void IsEnabled_WhenRegistrySubKeyDoesNotExist_ReturnsFalse()
    {
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyBarTest",
            () => @"C:\Program Files\StickyBar\StickyBar.exe");

        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_TrueThenFalse_WritesAndRemovesRegistryValue()
    {
        const string exePath = @"C:\Program Files\StickyBar\StickyBar.exe";
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyBarTest",
            () => exePath);

        service.SetEnabled(true);

        Assert.True(service.IsEnabled());

        using (var key = Registry.CurrentUser.OpenSubKey(_testSubKeyPath, writable: false))
        {
            Assert.NotNull(key);
            Assert.Equal($"\"{exePath}\"", key.GetValue("StickyBarTest"));
        }

        service.SetEnabled(false);

        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_FalseWhenMissing_DoesNotThrow()
    {
        var service = new WindowsAutoStartService(
            _testSubKeyPath,
            "StickyBarTest",
            () => @"C:\Program Files\StickyBar\StickyBar.exe");

        service.SetEnabled(false);

        Assert.False(service.IsEnabled());
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_testSubKeyPath, throwOnMissingSubKey: false);
    }
}
