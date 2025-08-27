using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ReisingerIntelliApp_V4.Converters
{
    public class SavedDeviceOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isAlreadySaved)
            {
                // If already saved, make it semi-transparent (0.5), otherwise fully opaque (1.0)
                return isAlreadySaved ? 0.5 : 1.0;
            }
            return 1.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
