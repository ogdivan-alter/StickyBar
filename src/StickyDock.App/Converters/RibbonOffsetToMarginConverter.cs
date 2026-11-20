using System.Globalization;
using System.Windows;
using System.Windows.Data;
using StickyDock.Core.Docking;

namespace StickyDock.App.Converters;

/// <summary>
/// Converts (<see cref="double"/> offset, <see cref="DockEdge"/>?) into a <see cref="Thickness"/> margin so the narrow
/// ribbon flyout aligns directly under or beside its indicator button without interfering with <c>TranslateTransform</c>
/// slide animations.
/// </summary>
public sealed class RibbonOffsetToMarginConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double offset || double.IsNaN(offset) || double.IsInfinity(offset))
        {
            return new Thickness(0d);
        }

        var clamped = Math.Max(0d, offset);
        var edge = values[1] as DockEdge?;

        return edge is DockEdge.Left or DockEdge.Right
            ? new Thickness(0d, clamped, 0d, 0d)
            : new Thickness(clamped, 0d, 0d, 0d);
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
