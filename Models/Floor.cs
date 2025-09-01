using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

public class Floor
{
    public string FloorName { get; set; } = string.Empty;
    public string? PdfPath { get; set; } = string.Empty;
    public string? PngPath { get; set; }
    public ObservableCollection<PlacedDeviceModel> PlacedDevices { get; set; } = new();
}

public class PlacedDeviceModel
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; } = 1.0;
    public DeviceType DeviceType { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.Now;
    
    // Device reference data for API calls
    public string DeviceIp { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public enum DeviceType
{
    WifiDevice,
    LocalDevice
}
