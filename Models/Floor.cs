using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

public class Floor
{
    public string FloorName { get; set; } = string.Empty;
    public string? PdfPath { get; set; } = string.Empty;
    public string? PngPath { get; set; }
    public ObservableCollection<PlacedDeviceModel> PlacedDevices { get; set; } = new();
}

// Minimal placeholder; can be expanded later to match V3
public class PlacedDeviceModel
{
    public string DeviceId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
