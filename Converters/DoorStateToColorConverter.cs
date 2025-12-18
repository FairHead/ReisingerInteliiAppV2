using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

/// <summary>
/// Converts door state (IsDoorOpen) to appropriate button background color.
/// Green when door is OPEN, Red when door is CLOSED.
/// </summary>
public class DoorStateToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDoorOpen)
        {
            // Green when OPEN (door is currently open, button shows "CLOSE")
            // Red when CLOSED (door is currently closed, button shows "OPEN")
            return isDoorOpen ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        }
        return Color.FromArgb("#F44336"); // Default to red (CLOSED)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
