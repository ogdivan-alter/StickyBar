using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using StickyDock.Core.Shortcuts;
using StickyDock.Interop.Win32;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Interop;

/// <summary>
/// E5-CA1, E5-CA2, §4.5-03, §5.1-05, Q-06, R-03: verifies COM <c>IShellLinkW</c> reading, icon extraction to PNG
/// bytes without native handle leaks, and faithful process launch with arguments.
/// </summary>
public sealed class ShellLinkReaderTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Read_RealLnkWithArgumentsAndWorkingDirectory_PreservesMetadataAndExtractsPngIcon()
    {
        var lnkPath = _directory.GetFilePath("UbuntuWSL.lnk");
        var targetPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        const string arguments = "--distribution Ubuntu --exec bash -lc \"echo sticky\"";
        var workingDir = _directory.Path;
        var iconSource = Path.Combine(Environment.SystemDirectory, "shell32.dll");

        CreateShortcutFile(lnkPath, targetPath, arguments, workingDir, iconSource, iconIndex: 0);

        var reader = new ShellLinkReader();
        var resolved = reader.Read(lnkPath);

        Assert.Equal(lnkPath, resolved.ShortcutPath);
        Assert.Equal("UbuntuWSL", resolved.DisplayName);
        Assert.Equal(targetPath, resolved.TargetPath, ignoreCase: true);
        Assert.Equal(arguments, resolved.Arguments);
        Assert.Equal(workingDir, resolved.WorkingDirectory, ignoreCase: true);
        Assert.NotNull(resolved.IconPngBytes);
        Assert.True(resolved.IconPngBytes.Length > 8);
        // PNG magic bytes: 89 50 4E 47
        Assert.Equal(0x89, resolved.IconPngBytes[0]);
        Assert.Equal(0x50, resolved.IconPngBytes[1]);
        Assert.Equal(0x4E, resolved.IconPngBytes[2]);
        Assert.Equal(0x47, resolved.IconPngBytes[3]);
    }

    [Fact]
    public void Read_NonLnkExtension_ThrowsArgumentException()
    {
        var exePath = _directory.GetFilePath("tool.exe");
        File.WriteAllText(exePath, string.Empty);

        var reader = new ShellLinkReader();

        Assert.Throws<ArgumentException>(() => reader.Read(exePath));
    }

    [Fact]
    public void Read_MissingLnkFile_ThrowsFileNotFoundException()
    {
        var reader = new ShellLinkReader();

        Assert.Throws<FileNotFoundException>(() => reader.Read(_directory.GetFilePath("missing.lnk")));
    }

    [Fact]
    public void ShortcutLauncher_WithRealLnk_ConstructsExactProcessStartInfo()
    {
        var lnkPath = _directory.GetFilePath("WslSample.lnk");
        var targetPath = Path.Combine(Environment.SystemDirectory, "wsl.exe");
        const string arguments = "-d Debian -e ls -la";
        var workingDir = _directory.Path;

        CreateShortcutFile(lnkPath, targetPath, arguments, workingDir, iconLocation: null, iconIndex: 0);

        ProcessStartInfo? captured = null;
        var launcher = new ShortcutLauncher(
            new ShellLinkReader(),
            psi =>
            {
                captured = psi;
                return true;
            });

        var result = launcher.Launch(lnkPath);

        Assert.True(result.Succeeded);
        Assert.NotNull(captured);
        Assert.Equal(targetPath, captured.FileName, ignoreCase: true);
        Assert.Equal(arguments, captured.Arguments);
        Assert.Equal(workingDir, captured.WorkingDirectory, ignoreCase: true);
        Assert.True(captured.UseShellExecute);
    }

    [Fact]
    public void ShortcutLauncher_RealEndToEndExecution_PreservesCommandLineArguments()
    {
        var lnkPath = _directory.GetFilePath("EchoTest.lnk");
        var outputFile = _directory.GetFilePath("echo-output.txt");
        var cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var arguments = $"/c echo sticky-e5-ok> \"{outputFile}\"";

        CreateShortcutFile(lnkPath, cmdPath, arguments, _directory.Path, iconLocation: null, iconIndex: 0);

        var launcher = new ShortcutLauncher(new ShellLinkReader());
        var result = launcher.Launch(lnkPath);

        Assert.True(result.Succeeded);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!File.Exists(outputFile) && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(25);
        }

        Assert.True(File.Exists(outputFile), "Expected cmd.exe launched via .lnk to create the output file.");
        var content = File.ReadAllText(outputFile).Trim();
        Assert.Equal("sticky-e5-ok", content);
    }

    [Fact]
    public void ShortcutLauncher_WhenTargetFailsToStart_ReturnsFailureWithoutThrowing()
    {
        var lnkPath = _directory.GetFilePath("Broken.lnk");
        var missingExe = _directory.GetFilePath("nonexistent_app_12345.exe");

        CreateShortcutFile(lnkPath, missingExe, "--flag", _directory.Path, iconLocation: null, iconIndex: 0);

        var launcher = new ShortcutLauncher(new ShellLinkReader());
        var result = launcher.Launch(lnkPath);

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void ShortcutLauncher_WhenTargetPathIsEmptyAndShortcutExists_LaunchesShortcutPathWithShellExecute()
    {
        var lnkPath = _directory.GetFilePath("Ubuntu.lnk");
        File.WriteAllText(lnkPath, "uwp-shell-link");

        ProcessStartInfo? captured = null;
        var launcher = new ShortcutLauncher(
            new EmptyTargetShellLinkReader(),
            psi =>
            {
                captured = psi;
                return true;
            });

        var result = launcher.Launch(lnkPath);

        Assert.True(result.Succeeded);
        Assert.NotNull(captured);
        Assert.Equal(lnkPath, captured.FileName);
        Assert.True(captured.UseShellExecute);
    }

    private sealed class EmptyTargetShellLinkReader : IShellLinkReader
    {
        public ResolvedShortcut Read(string shortcutPath)
            => new(
                shortcutPath,
                Path.GetFileNameWithoutExtension(shortcutPath),
                string.Empty,
                string.Empty,
                string.Empty,
                null);
    }

    private static void CreateShortcutFile(
        string lnkPath,
        string targetPath,
        string arguments,
        string workingDirectory,
        string? iconLocation,
        int iconIndex)
    {
        var shellLink = (IShellLinkWTest)(object)new ShellLinkTestCoClass();

        try
        {
            shellLink.SetPath(targetPath);
            shellLink.SetArguments(arguments);
            shellLink.SetWorkingDirectory(workingDirectory);

            if (!string.IsNullOrWhiteSpace(iconLocation))
            {
                shellLink.SetIconLocation(iconLocation, iconIndex);
            }

            var persistFile = (IPersistFile)shellLink;
            persistFile.Save(lnkPath, fRemember: true);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shellLink);
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class ShellLinkTestCoClass
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkWTest
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, nint pfd, uint fFlags);
        void GetIDList(out nint ppidl);
        void SetIDList(nint pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(nint hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
