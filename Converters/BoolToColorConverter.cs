using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

/// <summary>
/// Converts a boolean to a Color.
/// Use parameter format: "TrueColor|FalseColor" (e.g., "#FF0000|#00FF00")
/// Without parameter: true = Green, false = Red
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isTrue)
        {
            return Colors.Gray;
        }

        // Check if we have parameter with format "TrueColor|FalseColor"
        if (parameter is string paramStr && paramStr.Contains('|'))
        {
            var parts = paramStr.Split('|');
            if (parts.Length == 2)
            {
                var colorStr = isTrue ? parts[0] : parts[1];
                try
                {
                    return Color.FromArgb(colorStr);
                }
                catch
                {
                    // Fall through to default
                }
            }
        }

        // Default: Green for true, Red for false
        return isTrue ? Colors.Green : Colors.Red;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
