namespace StickyDock.Interop.Win32;

/// <summary>
/// Size of a display area, in pixels.
/// </summary>
/// <param name="WidthInPixels">Width of the area.</param>
/// <param name="HeightInPixels">Height of the area.</param>
public readonly record struct ScreenSize(int WidthInPixels, int HeightInPixels);
