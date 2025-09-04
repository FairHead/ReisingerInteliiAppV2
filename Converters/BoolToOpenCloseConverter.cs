using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

public class BoolToOpenCloseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDoorOpen)
        {
            return isDoorOpen ? "OPEN" : "CLOSE";
        }
        return "CLOSE";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return string.Equals(text, "OPEN", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}