using Microsoft.Maui.Controls;
using ReisingerIntelliApp_V4.Models;
using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

/// <summary>
/// Converts PlacedDeviceType to appropriate icon resource
/// </summary>
public class DeviceTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlacedDeviceType deviceType)
        {
            return deviceType switch
            {
                PlacedDeviceType.SavedDevice => "wifi_icon.svg",
                PlacedDeviceType.LocalDevice => "local_icon.svg",
                _ => "device_icon.svg"
            };
        }
        return "device_icon.svg";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts device online status to background color
/// </summary>
public class BoolToDeviceStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? Color.FromArgb("#007AFF") : Color.FromArgb("#8E8E93");
        }
        return Color.FromArgb("#8E8E93");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts device online status to connection indicator color
/// </summary>
public class BoolToConnectionColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? Color.FromArgb("#34C759") : Color.FromArgb("#FF3B30");
        }
        return Color.FromArgb("#FF3B30");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}