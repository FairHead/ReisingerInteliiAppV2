using ReisingerIntelliApp_V4.Models;
using System.Text.Json;

namespace ReisingerIntelliApp_V4.Services;

public interface IDeviceService
{
    Task<List<DeviceModel>> ScanForWifiDevicesAsync();
    Task<List<DeviceModel>> ScanForLocalDevicesAsync(string startIp, string endIp);
    Task<DeviceModel?> GetDeviceDetailsAsync(string deviceId);
    Task<bool> ConnectToDeviceAsync(string deviceId);
    
    // New methods for saving/loading devices
    Task<List<DeviceModel>> GetSavedWifiDevicesAsync();
    Task<List<DeviceModel>> AddDeviceAndReturnUpdatedListAsync(DeviceModel device);
    Task SaveDeviceAsync(DeviceModel device);
    Task DeleteDeviceAsync(DeviceModel device);
    Task<bool> DeviceExistsAsync(string deviceId);
    Task<bool> DeviceExistsBySsidAsync(string ssid);
    Task<(bool IsSuccessful, bool IsEmpty, List<DeviceModel> Devices)> LoadDeviceListAsync();
}

public class DeviceService : IDeviceService
{
    private const string DevicesKey = "SavedDevices";

    public async Task<List<DeviceModel>> ScanForWifiDevicesAsync()
    {
        // Simulate scanning for WiFi devices
        await Task.Delay(2000); // Simulate network delay
        
        return new List<DeviceModel>
        {
            new() 
            { 
                DeviceId = "wifi_001", 
                Name = "IntellidriveS5000", 
                MacAddress = "35:FF:6G:00:03", 
                Type = AppDeviceType.WifiDevice, 
                IsOnline = true, 
                LastSeen = DateTime.Now 
            },
            new() 
            { 
                DeviceId = "wifi_002", 
                Name = "SmartSensor_A1", 
                MacAddress = "35:FF:6G:00:04", 
                Type = AppDeviceType.Sensor, 
                IsOnline = true, 
                LastSeen = DateTime.Now 
            }
        };
    }

    public async Task<List<DeviceModel>> ScanForLocalDevicesAsync(string startIp, string endIp)
    {
        // Simulate scanning for local devices
        await Task.Delay(3000); // Simulate network scanning delay
        
        return new List<DeviceModel>
        {
            new() 
            { 
                DeviceId = "local_001", 
                Name = "IT Door Right 1", 
                IpAddress = "192.168.0.45", 
                Type = AppDeviceType.LocalDevice, 
                IsOnline = true, 
                LastSeen = DateTime.Now 
            },
            new() 
            { 
                DeviceId = "local_002", 
                Name = "Security Camera 01", 
                IpAddress = "192.168.0.101", 
                Type = AppDeviceType.SmartDevice, 
                IsOnline = false, 
                LastSeen = DateTime.Now.AddMinutes(-15) 
            }
        };
    }

    public async Task<DeviceModel?> GetDeviceDetailsAsync(string deviceId)
    {
        await Task.Delay(500);
        // Return device details
        return new DeviceModel 
        { 
            DeviceId = deviceId, 
            Name = "Sample Device", 
            IsOnline = true, 
            LastSeen = DateTime.Now 
        };
    }

    public async Task<bool> ConnectToDeviceAsync(string deviceId)
    {
        await Task.Delay(1000);
        // Simulate connection attempt
        return true;
    }

    // Device Storage Methods
    public async Task<List<DeviceModel>> GetSavedWifiDevicesAsync()
    {
        var (success, isEmpty, devices) = await LoadDeviceListAsync();
        if (!success || isEmpty) return new List<DeviceModel>();
        
        return devices.Where(d => d.ConnectionType == ConnectionType.Wifi).ToList();
    }

    public async Task<List<DeviceModel>> AddDeviceAndReturnUpdatedListAsync(DeviceModel device)
    {
        var devices = (await LoadDeviceListAsync()).Devices;
        devices.Add(device);
        await SaveDeviceListToSecureStoreAsync(devices);
        return devices;
    }

    public async Task SaveDeviceAsync(DeviceModel device)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 DeviceService.SaveDeviceAsync called for device: {device?.Name ?? "NULL"}");
        if (device == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ Device is null, cannot save");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"📊 Device details: ID={device.DeviceId}, Name={device.Name}, SSID={device.Ssid}");
        await AddDeviceAndReturnUpdatedListAsync(device);
        System.Diagnostics.Debug.WriteLine("✅ Device saved successfully");
    }

    public async Task DeleteDeviceAsync(DeviceModel device)
    {
        var devices = (await LoadDeviceListAsync()).Devices;
        var deviceToRemove = devices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
        if (deviceToRemove != null)
        {
            devices.Remove(deviceToRemove);
            await SaveDeviceListToSecureStoreAsync(devices);
        }
    }

    public async Task<bool> DeviceExistsAsync(string deviceId)
    {
        var devices = (await LoadDeviceListAsync()).Devices;
        return devices.Any(d => d.DeviceId == deviceId);
    }

    public async Task<bool> DeviceExistsBySsidAsync(string ssid)
    {
        var devices = (await LoadDeviceListAsync()).Devices;
        return devices.Any(d => d.Ssid == ssid && d.ConnectionType == ConnectionType.Wifi);
    }

    public async Task<(bool IsSuccessful, bool IsEmpty, List<DeviceModel> Devices)> LoadDeviceListAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(DevicesKey);
            if (string.IsNullOrEmpty(json))
            {
                return (true, true, new List<DeviceModel>());
            }

            var devices = JsonSerializer.Deserialize<List<DeviceModel>>(json) ?? new List<DeviceModel>();
            return (true, false, devices);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading device list: {ex.Message}");
            return (false, true, new List<DeviceModel>());
        }
    }

    private async Task SaveDeviceListToSecureStoreAsync(List<DeviceModel> devices)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Saving {devices.Count} devices to SecureStorage");
            var json = JsonSerializer.Serialize(devices);
            System.Diagnostics.Debug.WriteLine($"📊 JSON length: {json.Length}");
            await SecureStorage.SetAsync(DevicesKey, json);
            System.Diagnostics.Debug.WriteLine("✅ Devices saved to SecureStorage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error saving device list: {ex.Message}");
        }
    }
}
