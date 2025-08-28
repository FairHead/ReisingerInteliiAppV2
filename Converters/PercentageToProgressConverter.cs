using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

public class PercentageToProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return percentage / 100.0; // ProgressBar erwartet Werte zwischen 0.0 und 1.0
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return progress * 100.0;
        }
        return 0.0;
    }
}
