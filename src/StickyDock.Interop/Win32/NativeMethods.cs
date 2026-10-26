using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Raw P/Invoke declarations. They are internal on purpose: the rest of the product uses the safe wrappers of this
/// assembly and never handles native types (AGENTS.md).
/// </summary>
internal static partial class NativeMethods
{
    private const string User32LibraryName = "user32.dll";

    [LibraryImport(User32LibraryName, EntryPoint = "GetSystemMetrics")]
    internal static partial int GetSystemMetrics(int index);
}
