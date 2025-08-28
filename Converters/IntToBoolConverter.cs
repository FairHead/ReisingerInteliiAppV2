using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString() == "true";
        
        if (value is int count)
        {
            bool result = count > 0;
            return invert ? !result : result;
        }
        return invert;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
