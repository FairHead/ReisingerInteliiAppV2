using System;
namespace ReisingerIntelliApp_V4.Models;

public static class DeviceModelExtensions
{
    /// <summary>
    /// Creates a DeviceModel from a NetworkDataModel (replacement for removed DeviceModel.FromNetworkData).
    /// Kept as an extension method to avoid reintroducing duplicate model definitions.
    /// </summary>
    public static DeviceModel ToDeviceModel(this NetworkDataModel network)
    {
        if (network == null) throw new ArgumentNullException(nameof(network));
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
            Parameters = new(),
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
