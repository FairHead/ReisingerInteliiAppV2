using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

public class Floor
{
    public string FloorName { get; set; } = string.Empty;
    public string? PdfPath { get; set; } = string.Empty;
    public string? PngPath { get; set; }
    public ObservableCollection<PlacedDeviceModel> PlacedDevices { get; set; } = new();
}

/// <summary>
/// Represents a device placed on a floor plan with position, scale, and device reference
/// </summary>
public class PlacedDeviceModel
{
    public string DeviceId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; } = 1.0;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIp { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // "WiFi" or "Local"
}
