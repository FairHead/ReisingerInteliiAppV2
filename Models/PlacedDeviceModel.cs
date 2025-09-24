using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Represents a device placed on a floor plan
/// </summary>
public partial class PlacedDeviceModel : ObservableObject
{
    // Reference to the original device
    public DeviceModel DeviceInfo { get; set; }

    // Direct properties for easy access (mirror some properties from DeviceInfo)
    [ObservableProperty]
    private string placedDeviceId = string.Empty;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private string deviceIpAddress = string.Empty;

    // Compatibility properties for DevicePinOverlay
    public string DeviceId 
    { 
        get => PlacedDeviceId; 
        set => PlacedDeviceId = value; 
    }

    public string Name 
    { 
        get => DeviceName; 
        set => DeviceName = value; 
    }

    public string IpAddress 
    { 
        get => DeviceIpAddress; 
        set => DeviceIpAddress = value; 
    }

    public string DeviceType { get; set; } = "Intellidrive";

    // Display helper: SSID for Wifi devices, otherwise IP address
    public string NetworkInfo
        => DeviceInfo != null && DeviceInfo.Type == AppDeviceType.WifiDevice
            ? (!string.IsNullOrWhiteSpace(DeviceInfo.Ssid) ? DeviceInfo.Ssid : IpAddress)
            : IpAddress;

    // Position properties for DevicePinOverlay compatibility
    public double X 
    { 
        get => RelativeX; 
        set => RelativeX = value; 
    }

    public double Y 
    { 
        get => RelativeY; 
        set => RelativeY = value; 
    }

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private bool isVisible = true;

    // Normalized center coordinates in plan space [0..1]
    private double _relativeX; // Back-compat alias for XCenterNorm
    public double RelativeX
    {
        get => _relativeX;
        set
        {
            if (Math.Abs(_relativeX - value) > 0.0001)
            {
                var old = _relativeX; _relativeX = value;
                OnPropertyChanged(); OnPropertyChanged(nameof(XCenterNorm));
                Debug.WriteLine($"📍 X center: {old:F3} → {value:F3}");
            }
        }
    }

    private double _relativeY; // Back-compat alias for YCenterNorm
    public double RelativeY
    {
        get => _relativeY;
        set
        {
            if (Math.Abs(_relativeY - value) > 0.0001)
            {
                var old = _relativeY; _relativeY = value;
                OnPropertyChanged(); OnPropertyChanged(nameof(YCenterNorm));
                Debug.WriteLine($"📍 Y center: {old:F3} → {value:F3}");
            }
        }
    }

    // Preferred explicit names
    public double XCenterNorm { get => RelativeX; set => RelativeX = value; }
    public double YCenterNorm { get => RelativeY; set => RelativeY = value; }

    // Base size normalized to plan size
    private double _baseWidthNorm = 0.15; // default ~15% of plan width (increased for better scaling)
    public double BaseWidthNorm { get => _baseWidthNorm; set { if (Math.Abs(_baseWidthNorm - value) > 0.0001) { _baseWidthNorm = value; OnPropertyChanged(); } } }

    private double _baseHeightNorm = 0.18; // default ~18% of plan height (increased for better scaling)
    public double BaseHeightNorm { get => _baseHeightNorm; set { if (Math.Abs(_baseHeightNorm - value) > 0.0001) { _baseHeightNorm = value; OnPropertyChanged(); } } }

    private double _scale = 1.0; // Local scale multiplier (independent of plan P.scale)
    public double Scale 
    { 
        get => _scale; 
        set 
        { 
            if (Math.Abs(_scale - value) > 0.001) // Use consistent threshold
            {
                var oldValue = _scale;
                _scale = value;
                Debug.WriteLine($"📊 PlacedDeviceModel.Scale - Device: {Name}");
                Debug.WriteLine($"   Scale Change: {oldValue:F3} → {value:F3}");
                Debug.WriteLine($"   Difference: {Math.Abs(oldValue - value):F6}");
                Debug.WriteLine($"   Threshold: 0.001");
                OnPropertyChanged();
            }
            else
            {
                Debug.WriteLine($"📊 PlacedDeviceModel.Scale - Device: {Name} - Change too small: {Math.Abs(_scale - value):F6} (threshold: 0.001)");
            }
        } 
    }

    // Optional rotation in degrees
    private double _rotationDeg;
    public double RotationDeg { get => _rotationDeg; set { if (Math.Abs(_rotationDeg - value) > 0.01) { _rotationDeg = value; OnPropertyChanged(); } } }

    // ID of the building and floor where the device is placed
    public int BuildingId { get; set; }
    public int FloorId { get; set; }

    // Status for UI display
    [ObservableProperty]
    private bool isDoorOpen;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isBeingDragged;

    // Size properties for the placed device representation
    [ObservableProperty]
    private double width = 140;

    [ObservableProperty]
    private double height = 177;

    [ObservableProperty]
    private string deviceColor = "#4CAF50";

    // Mode toggles for dropdown UI
    [ObservableProperty]
    private bool dauerAuf; // Always open mode

    [ObservableProperty]
    private bool isLocked; // Locked/unlocked

    [ObservableProperty]
    private bool isOneWay; // One-way mode

    public enum AutoModeLevel { None, Half, Full }

    [ObservableProperty]
    private AutoModeLevel autoMode = AutoModeLevel.None; // Automatic mode selection

    [ObservableProperty]
    private bool isWinterMode; // Winter mode

    // Constructor
    public PlacedDeviceModel()
    {
        DeviceInfo = new DeviceModel();
    }

    public PlacedDeviceModel(DeviceModel deviceModel)
    {
        DeviceInfo = deviceModel;
        DeviceId = deviceModel.DeviceId;
        Name = deviceModel.Name;
        IpAddress = deviceModel.IpAddress;
        IsOnline = deviceModel.IsOnline;
        OnPropertyChanged(nameof(NetworkInfo));
        // Defaults for normalized sizing and scale
        if (double.IsNaN(BaseWidthNorm) || BaseWidthNorm <= 0) BaseWidthNorm = 0.15;
        if (double.IsNaN(BaseHeightNorm) || BaseHeightNorm <= 0) BaseHeightNorm = 0.18;
        if (Scale <= 0) Scale = 1.0; // initial visual scale 100%
    }

    /// <summary>
    /// Updates the placed device with current device information
    /// </summary>
    public void UpdateFromDevice(DeviceModel device)
    {
        DeviceInfo = device;
        DeviceId = device.DeviceId;
        Name = device.Name;
        IpAddress = device.IpAddress;
        IsOnline = device.IsOnline;
        IsDoorOpen = !device.IsDoorClosed;
        OnPropertyChanged(nameof(NetworkInfo));
    }
}
