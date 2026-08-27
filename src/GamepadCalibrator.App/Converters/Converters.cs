using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GamepadCalibrator.App.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true
            ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
            : new SolidColorBrush(Color.FromRgb(0x16, 0x20, 0x33));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class NormToOffsetConverter : IMultiValueConverter
{
    // values: normX or normY, size
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double norm || values[1] is not double size)
            return 0.0;
        var axis = parameter as string ?? "X";
        // map -1..1 to pixel offset inside square with padding
        var pad = 18.0;
        var usable = Math.Max(1, size - pad * 2);
        var mid = size / 2;
        if (axis == "Y")
            return mid + norm * (usable / 2) - 8; // 8 = half dot
        return mid + norm * (usable / 2) - 8;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
