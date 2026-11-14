using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using StickyDock.Core.Shortcuts;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Reads <c>.lnk</c> shortcut metadata and extracts icons via COM <c>IShellLinkW</c> and Shell32 (E5-CA1, §5.1-05, R-03).
/// </summary>
public sealed class ShellLinkReader : IShellLinkReader
{
    private const int MaxPathBuffer = 1024;
    private const int MaxArgumentsBuffer = 4096;
    private const uint SlgpRawPath = 0x4;
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const int StgmRead = 0;

    /// <inheritdoc />
    public ResolvedShortcut Read(string shortcutPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortcutPath);

        if (!string.Equals(Path.GetExtension(shortcutPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"File '{shortcutPath}' is not a .lnk shortcut.", nameof(shortcutPath));
        }

        if (!File.Exists(shortcutPath))
        {
            throw new FileNotFoundException($"Shortcut file not found: '{shortcutPath}'.", shortcutPath);
        }

        var shellLink = (IShellLinkW)(object)new ShellLinkCoClass();

        try
        {
            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(shortcutPath, StgmRead);

            var targetBuilder = new StringBuilder(MaxPathBuffer);
            shellLink.GetPath(targetBuilder, targetBuilder.Capacity, nint.Zero, 0);
            var targetPath = targetBuilder.ToString();

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                targetBuilder.Clear();
                shellLink.GetPath(targetBuilder, targetBuilder.Capacity, nint.Zero, SlgpRawPath);
                targetPath = targetBuilder.ToString();
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                targetPath = Environment.ExpandEnvironmentVariables(targetPath);
            }

            var argumentsBuilder = new StringBuilder(MaxArgumentsBuffer);
            shellLink.GetArguments(argumentsBuilder, argumentsBuilder.Capacity);
            var arguments = argumentsBuilder.ToString();

            var workingDirBuilder = new StringBuilder(MaxPathBuffer);
            shellLink.GetWorkingDirectory(workingDirBuilder, workingDirBuilder.Capacity);
            var workingDirectory = workingDirBuilder.ToString();

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Environment.ExpandEnvironmentVariables(workingDirectory);
            }

            var iconPathBuilder = new StringBuilder(MaxPathBuffer);
            shellLink.GetIconLocation(iconPathBuilder, iconPathBuilder.Capacity, out var iconIndex);
            var iconLocation = iconPathBuilder.ToString();

            var iconPngBytes = ExtractIconPngBytes(shortcutPath, targetPath, iconLocation, iconIndex);
            var displayName = Path.GetFileNameWithoutExtension(shortcutPath);

            return new ResolvedShortcut(
                shortcutPath,
                displayName,
                targetPath,
                arguments,
                workingDirectory,
                iconPngBytes);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shellLink);
        }
    }

    private static byte[]? ExtractIconPngBytes(
        string shortcutPath,
        string targetPath,
        string iconLocation,
        int iconIndex)
    {
        if (!string.IsNullOrWhiteSpace(iconLocation))
        {
            var expandedIconLocation = Environment.ExpandEnvironmentVariables(iconLocation);
            var fromLocation = TryExtractFromLocation(expandedIconLocation, iconIndex);

            if (fromLocation is not null)
            {
                return fromLocation;
            }
        }

        var fromShortcut = TryExtractViaShellGetFileInfo(shortcutPath);

        if (fromShortcut is not null)
        {
            return fromShortcut;
        }

        return !string.IsNullOrWhiteSpace(targetPath)
            ? TryExtractViaShellGetFileInfo(targetPath)
            : null;
    }

    private static unsafe byte[]? TryExtractFromLocation(string iconLocation, int iconIndex)
    {
        var hIcon = nint.Zero;

        try
        {
            var extracted = NativeMethods.ExtractIconExW(iconLocation, iconIndex, &hIcon, null, 1);

            if (extracted == 0 || hIcon == nint.Zero)
            {
                return null;
            }

            return ConvertHIconToPngBytes(hIcon);
        }
        finally
        {
            if (hIcon != nint.Zero)
            {
                NativeMethods.DestroyIcon(hIcon);
            }
        }
    }

    private static unsafe byte[]? TryExtractViaShellGetFileInfo(string path)
    {
        var shfi = default(SHFILEINFOW);

        try
        {
            var result = NativeMethods.SHGetFileInfoW(
                path,
                0,
                ref shfi,
                (uint)sizeof(SHFILEINFOW),
                ShgfiIcon | ShgfiLargeIcon);

            if (result == nint.Zero || shfi.hIcon == nint.Zero)
            {
                return null;
            }

            return ConvertHIconToPngBytes(shfi.hIcon);
        }
        finally
        {
            if (shfi.hIcon != nint.Zero)
            {
                NativeMethods.DestroyIcon(shfi.hIcon);
            }
        }
    }

    private static byte[] ConvertHIconToPngBytes(nint hIcon)
    {
        using var icon = Icon.FromHandle(hIcon);
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class ShellLinkCoClass
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
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
