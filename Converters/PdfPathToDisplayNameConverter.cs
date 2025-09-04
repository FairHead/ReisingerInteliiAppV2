using Microsoft.Maui.Controls;
using System.Globalization;

namespace ReisingerIntelliApp_V4.Converters;

public class PdfPathToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string pdfPath || string.IsNullOrWhiteSpace(pdfPath))
        {
            return string.Empty;
        }

        try
        {
            // Extract filename from the full path
            var fileName = Path.GetFileName(pdfPath);
            
            // If it's just "plan.pdf" (default name), try to get a more descriptive name
            if (string.Equals(fileName, "plan.pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Try to extract a meaningful name from the path structure
                // Path structure: .../floorplans/[BuildingName]/[FloorName]/plan.pdf
                var directories = Path.GetDirectoryName(pdfPath)?.Split(Path.DirectorySeparatorChar);
                if (directories?.Length >= 2)
                {
                    var floorName = directories[^1]; // Last directory (FloorName)
                    return $"FloorPlan: {floorName}.pdf";
                }
            }
            
            return $"FloorPlan: {fileName}";
        }
        catch
        {
            // Fallback if path parsing fails
            return "FloorPlan: plan.pdf";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}