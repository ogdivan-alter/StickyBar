using System.Diagnostics;
using Microsoft.Win32;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Installer;

/// <summary>
/// E8-CA5: verifies <c>installer/StickyDockInstaller.ps1</c> against an isolated temporary directory and isolated HKCU
/// registry subkeys so the user's real installation and startup registry are never touched.
/// </summary>
public sealed class InstallerScriptTests : IDisposable
{
    private readonly TemporaryDirectory _sandbox = new();
    private readonly string _registryRootRelative = $@"Software\StickyDock\Tests\Installer_{Guid.NewGuid():N}";

    [Fact]
    public void InstallAndUninstall_ManagesFilesShortcutUninstallKeyAndAutoStartCoherently()
    {
        var scriptPath = ResolveInstallerScriptPath();
        var sourceDir = Directory.CreateDirectory(_sandbox.GetFilePath("source")).FullName;
        var installDir = _sandbox.GetFilePath("install");
        var startMenuDir = _sandbox.GetFilePath("startmenu");
        var runRelative = $@"{_registryRootRelative}\Run";
        var uninstallRelative = $@"{_registryRootRelative}\Uninstall\StickyDock";

        File.WriteAllText(Path.Combine(sourceDir, "StickyDock.exe"), "stub-exe");
        File.WriteAllText(Path.Combine(sourceDir, "StickyDock.dll"), "stub-dll");

        // 1. Install with AutoStart enabled.
        RunInstaller(
            scriptPath,
            "Install",
            installDir,
            startMenuDir,
            sourceDir,
            $@"HKCU:\{runRelative}",
            $@"HKCU:\{uninstallRelative}",
            autoStart: true);

        Assert.True(File.Exists(Path.Combine(installDir, "StickyDock.exe")));
        Assert.True(File.Exists(Path.Combine(installDir, "StickyDock.dll")));
        Assert.True(File.Exists(Path.Combine(installDir, "StickyDockInstaller.ps1")));

        var shortcutPath = Path.Combine(startMenuDir, "Sticky Dock Utility.lnk");
        Assert.True(File.Exists(shortcutPath));

        using (var uninstallKey = Registry.CurrentUser.OpenSubKey(uninstallRelative, writable: false))
        {
            Assert.NotNull(uninstallKey);
            Assert.Equal("Sticky Dock Utility", uninstallKey.GetValue("DisplayName"));
            Assert.NotNull(uninstallKey.GetValue("UninstallString"));
        }

        using (var runKey = Registry.CurrentUser.OpenSubKey(runRelative, writable: false))
        {
            Assert.NotNull(runKey);
            var expectedValue = $"\"{Path.Combine(installDir, "StickyDock.exe")}\"";
            Assert.Equal(expectedValue, runKey.GetValue("StickyDock"));
        }

        // 2. Re-install with AutoStart disabled removes the Run entry while preserving the installation.
        RunInstaller(
            scriptPath,
            "Install",
            installDir,
            startMenuDir,
            sourceDir,
            $@"HKCU:\{runRelative}",
            $@"HKCU:\{uninstallRelative}",
            autoStart: false);

        using (var runKeyAfterDisable = Registry.CurrentUser.OpenSubKey(runRelative, writable: false))
        {
            Assert.Null(runKeyAfterDisable?.GetValue("StickyDock"));
        }

        Assert.True(File.Exists(Path.Combine(installDir, "StickyDock.exe")));

        // 3. Uninstall removes installed files, shortcut, Uninstall key and Run entry.
        RunInstaller(
            scriptPath,
            "Uninstall",
            installDir,
            startMenuDir,
            sourceDir,
            $@"HKCU:\{runRelative}",
            $@"HKCU:\{uninstallRelative}",
            autoStart: false);

        Assert.False(Directory.Exists(installDir));
        Assert.False(File.Exists(shortcutPath));
        Assert.Null(Registry.CurrentUser.OpenSubKey(uninstallRelative, writable: false));

        using (var runKeyAfterUninstall = Registry.CurrentUser.OpenSubKey(runRelative, writable: false))
        {
            Assert.Null(runKeyAfterUninstall?.GetValue("StickyDock"));
        }
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_registryRootRelative, throwOnMissingSubKey: false);
        _sandbox.Dispose();
    }

    private static string ResolveInstallerScriptPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "installer", "StickyDockInstaller.ps1");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate 'installer/StickyDockInstaller.ps1' from test base directory.");
    }

    private static void RunInstaller(
        string scriptPath,
        string action,
        string installDir,
        string startMenuDir,
        string sourceDir,
        string registryRunPath,
        string uninstallRegistryPath,
        bool autoStart)
    {
        var autoStartArg = autoStart ? " -AutoStart" : string.Empty;
        var arguments =
            $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
            $"-Action {action} " +
            $"-InstallDir \"{installDir}\" " +
            $"-StartMenuDir \"{startMenuDir}\" " +
            $"-SourceDir \"{sourceDir}\" " +
            $"-RegistryRunPath \"{registryRunPath}\" " +
            $"-UninstallRegistryPath \"{uninstallRegistryPath}\"" +
            autoStartArg;

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Failed to start powershell.exe.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"Installer script failed (exit code {process.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }
}
