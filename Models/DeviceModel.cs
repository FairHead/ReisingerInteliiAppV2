using CommunityToolkit.Mvvm.ComponentModel;

namespace ReisingerIntelliApp_V4.Models;

public partial class DeviceModel : ObservableObject
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Ssid { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Ip { get; set; } = "192.168.4.100"; // Standard WiFi Antrieb IP
    public AppDeviceType Type { get; set; }
    public ConnectionType ConnectionType { get; set; }

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private bool isDoorClosed;

    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public DateTime LastSeen { get; set; } = DateTime.Now;
    public Dictionary<string, string> Parameters { get; set; } = new();

    // Header Data
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public bool LatestFirmware { get; set; }
    public string SoftwareVersion { get; set; } = string.Empty;
    public string ModuleType { get; set; } = "Default";
    public string ModuleId { get; set; } = string.Empty;

    // WiFi specific properties
    public string SignalStrengthText => IsOnline ? "Online" : "Offline";
    public string ConnectionStatusText => $"{ConnectionType} - {(IsOnline ? "Verbunden" : "Getrennt")}";

    // Mapper from NetworkDataModel
    public static DeviceModel FromNetworkData(NetworkDataModel network)
    {
        return new DeviceModel
        {
            DeviceId = network.DeviceId ?? $"wifi_{DateTime.Now.Ticks}",
            Name = network.Name ?? network.SsidName,
            Ssid = network.Ssid ?? string.Empty,
            Username = network.Username,
            Password = network.Password,
            BearerToken = network.BearerToken ?? string.Empty,
            SerialNumber = network.SerialNumber,
            IsOnline = network.IsConnected,
            LastUpdated = DateTime.Now,
            Parameters = new Dictionary<string, string>(),
            FirmwareVersion = network.FirmwareVersion,
            LatestFirmware = network.LatestFirmware,
            SoftwareVersion = network.SoftwareVersion,
            ModuleType = network.ModuleType,
            ModuleId = network.ModuleId,
            Type = AppDeviceType.WifiDevice,
            ConnectionType = ConnectionType.Wifi,
            Ip = "192.168.4.100"
        };
    }
}

public enum AppDeviceType
{
    Unknown,
    WifiDevice,
    LocalDevice,
    SmartDevice,
    Sensor
}

public enum ConnectionType
{
    Wifi,
    Ethernet,
    Local
}
