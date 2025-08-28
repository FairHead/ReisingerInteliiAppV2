using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

public class BoolToStrokeThicknessConverter : IValueConverter
{
    public double TrueThickness { get; set; } = 2.0;
    public double FalseThickness { get; set; } = 0.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? TrueThickness : FalseThickness;
        }
        return FalseThickness;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
