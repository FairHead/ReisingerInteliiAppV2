using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReisingerIntelliApp_V4.Models;

public class Floor
{
    public string FloorName { get; set; } = string.Empty;
    public string? PdfPath { get; set; } = string.Empty;
    public string? PngPath { get; set; }
    public ObservableCollection<PlacedDeviceModel> PlacedDevices { get; set; } = new();
}

/// <summary>
/// Represents a device placed on a floor plan with position and display properties
/// </summary>
public partial class PlacedDeviceModel : ObservableObject
{
    [ObservableProperty]
    private string deviceId = string.Empty;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double size = 32.0; // Default pin size in logical pixels

    [ObservableProperty]
    private PlacedDeviceType deviceType = PlacedDeviceType.SavedDevice;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private string deviceInfo = string.Empty; // IP/SSID or other identifying info

    [ObservableProperty]
    private bool isOnline;

    // Reference to the actual device data (not persisted)
    public DeviceModel? SavedDevice { get; set; }
    public LocalNetworkDeviceModel? LocalDevice { get; set; }

    /// <summary>
    /// Gets the device for API calls based on type
    /// </summary>
    public DeviceModel? GetApiDevice()
    {
        if (DeviceType == PlacedDeviceType.SavedDevice) 
            return SavedDevice;
        
        // For local devices, create a DeviceModel for API calls
        if (LocalDevice != null)
        {
            return new DeviceModel
            {
                DeviceId = LocalDevice.DeviceId,
                Name = LocalDevice.DisplayName,
                Ip = LocalDevice.IpAddress,
                IpAddress = LocalDevice.IpAddress,
                IsOnline = LocalDevice.IsOnline,
                Type = AppDeviceType.LocalDevice,
                ConnectionType = ConnectionType.Local
            };
        }
        
        return null;
    }

    /// <summary>
    /// Creates a PlacedDeviceModel from a SavedDevice
    /// </summary>
    public static PlacedDeviceModel FromSavedDevice(DeviceModel device, double x, double y)
    {
        return new PlacedDeviceModel
        {
            DeviceId = device.DeviceId,
            X = x,
            Y = y,
            DeviceType = PlacedDeviceType.SavedDevice,
            DeviceName = device.Name,
            DeviceInfo = device.Ssid,
            IsOnline = device.IsOnline,
            SavedDevice = device
        };
    }

    /// <summary>
    /// Creates a PlacedDeviceModel from a LocalDevice
    /// </summary>
    public static PlacedDeviceModel FromLocalDevice(LocalNetworkDeviceModel device, double x, double y)
    {
        return new PlacedDeviceModel
        {
            DeviceId = device.DeviceId,
            X = x,
            Y = y,
            DeviceType = PlacedDeviceType.LocalDevice,
            DeviceName = device.DisplayName,
            DeviceInfo = device.IpAddress,
            IsOnline = device.IsOnline,
            LocalDevice = device
        };
    }
}

public enum PlacedDeviceType
{
    SavedDevice,
    LocalDevice
}
