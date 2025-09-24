using ReisingerIntelliApp_V4.Models;
using System.Text.Json;
using System.Diagnostics;
using System.Net;

namespace ReisingerIntelliApp_V4.Services;

public interface IDeviceService
{
    Task<List<DeviceModel>> ScanForWifiDevicesAsync();
    Task<List<DeviceModel>> ScanForLocalDevicesAsync(string startIp, string endIp);
    Task<List<LocalNetworkDeviceModel>> ScanForLocalNetworkDevicesAsync(string startIp, string endIp);
    Task<DeviceModel?> GetDeviceDetailsAsync(string deviceId);
    Task<bool> ConnectToDeviceAsync(string deviceId);
    
    // New methods for saving/loading devices
    Task<List<DeviceModel>> GetSavedWifiDevicesAsync();
    Task<List<DeviceModel>> GetSavedLocalDevicesAsync();
    Task<List<DeviceModel>> AddDeviceAndReturnUpdatedListAsync(DeviceModel device);
    Task SaveDeviceAsync(DeviceModel device);
    Task DeleteDeviceAsync(DeviceModel device);
    Task<bool> DeviceExistsAsync(string deviceId);
    Task<bool> DeviceExistsBySsidAsync(string ssid);
    Task<(bool IsSuccessful, bool IsEmpty, List<DeviceModel> Devices)> LoadDeviceListAsync();

    // Neue Methoden für komplettes Speichern
    Task SaveLocalDevicesAsync(List<DeviceModel> devices);
    Task SaveWifiDevicesAsync(List<DeviceModel> devices);
}

public class DeviceService : IDeviceService
{
    private const string DevicesKey = "SavedDevices";
    private readonly IntellidriveApiService _intellidriveApiService;

    public DeviceService(IntellidriveApiService intellidriveApiService)
    {
        _intellidriveApiService = intellidriveApiService ?? throw new ArgumentNullException(nameof(intellidriveApiService));
    }

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
        try
        {
            var foundDevices = new List<DeviceModel>();
            var scannedDevices = await ScanForLocalNetworkDevicesAsync(startIp, endIp);
            
            foreach (var device in scannedDevices)
            {
                var deviceModel = new DeviceModel
                {
                    DeviceId = device.DeviceId ?? $"local_{DateTime.Now.Ticks}",
                    Name = device.DisplayName,
                    IpAddress = device.IpAddress,
                    SerialNumber = device.SerialNumber,
                    FirmwareVersion = device.FirmwareVersion,
                    SoftwareVersion = device.SoftwareVersion,
                    LatestFirmware = device.LatestFirmware,
                    Type = AppDeviceType.LocalDevice,
                    ConnectionType = ConnectionType.Local,
                    IsOnline = device.IsOnline,
                    LastUpdated = device.DiscoveredAt,
                    Ip = device.IpAddress
                };
                
                foundDevices.Add(deviceModel);
            }
            
            return foundDevices;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error in ScanForLocalDevicesAsync: {ex.Message}");
            return new List<DeviceModel>();
        }
    }

    /// <summary>
    /// Efficient parallel scanning of IP range for Intellidrive devices
    /// Based on sophisticated implementation from V3 with improvements
    /// </summary>
    public async Task<List<LocalNetworkDeviceModel>> ScanForLocalNetworkDevicesAsync(string startIp, string endIp)
    {
        var foundDevices = new List<LocalNetworkDeviceModel>();
        
        try
        {
            Debug.WriteLine($"🔍 Starting local network scan from {startIp} to {endIp}");
            
            // Get saved device IDs for comparison to ensure we can grey out already-saved devices
            var savedDevices = await GetSavedLocalDevicesAsync();
            var savedDeviceIds = savedDevices.Select(d => d.DeviceId ?? string.Empty).ToHashSet();
            
            // Parse IP range
            var startBytes = IPAddress.Parse(startIp).GetAddressBytes();
            var endBytes = IPAddress.Parse(endIp).GetAddressBytes();
            uint start = BitConverter.ToUInt32(startBytes.Reverse().ToArray(), 0);
            uint end = BitConverter.ToUInt32(endBytes.Reverse().ToArray(), 0);
            
            // Generate list of IP addresses to scan
            var ipsToScan = Enumerable
                .Range(0, (int)(end - start + 1))
                .Select(i => {
                    uint ip = start + (uint)i;
                    byte[] bytes = BitConverter.GetBytes(ip).Reverse().ToArray();
                    return new IPAddress(bytes).ToString();
                })
                .ToList();
            
            Debug.WriteLine($"📡 Scanning {ipsToScan.Count} IP addresses with parallel processing");
            
            // Use SemaphoreSlim for controlled parallelism (max 20 concurrent connections)
            using var semaphore = new SemaphoreSlim(20, 20);
        var scanTasks = ipsToScan.Select(async ip => {
                await semaphore.WaitAsync();
                try
                {
            return await ScanSingleDeviceAsync(ip, savedDeviceIds);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            var results = await Task.WhenAll(scanTasks);
            foundDevices = results.Where(d => d != null).Cast<LocalNetworkDeviceModel>().ToList();
            
            Debug.WriteLine($"✅ Network scan completed. Found {foundDevices.Count} devices");
            
            return foundDevices;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error during network scan: {ex.Message}");
            return foundDevices;
        }
    }
    
    private async Task<LocalNetworkDeviceModel?> ScanSingleDeviceAsync(string ipAddress, HashSet<string> savedDeviceIds)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Try to get version info from the device
            var versionResponse = await _intellidriveApiService.GetVersionAsync(ipAddress, timeoutSeconds: 3);
            
            stopwatch.Stop();
            
        if (versionResponse != null && !string.IsNullOrEmpty(versionResponse.Message))
            {
                var device = new LocalNetworkDeviceModel
                {
                    IpAddress = ipAddress,
                    DeviceId = versionResponse.DeviceId ?? $"local_{DateTime.Now.Ticks}",
                    SerialNumber = versionResponse.DeviceId ?? "Unknown",
                    FirmwareVersion = versionResponse.FirmwareVersion ?? "Unknown",
                    SoftwareVersion = versionResponse.Message ?? "Unknown", 
                    LatestFirmware = versionResponse.LatestFirmware,
            // Grey out immediately if we've already saved this DeviceId
            IsAlreadySaved = savedDeviceIds.Contains(versionResponse.DeviceId ?? string.Empty),
                    DiscoveredAt = DateTime.Now,
                    IsOnline = true,
                    ResponseTime = DateTime.Now // Fixed: Use DateTime instead of TimeSpan
                };
                
                Debug.WriteLine($"✅ Found device at {ipAddress}: {device.SerialNumber} (Response: {stopwatch.ElapsedMilliseconds}ms)");
                return device;
            }
        }
        catch (HttpRequestException)
        {
            // Device not reachable - this is expected for most IPs
        }
        catch (TaskCanceledException)
        {
            // Timeout - also expected
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Error scanning {ipAddress}: {ex.Message}");
        }
        
        return null;
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

        // Load existing list
        var (success, _, devices) = await LoadDeviceListAsync();
        if (!success)
        {
            devices = new List<DeviceModel>();
        }

        // Try match by DeviceId first (most reliable)
        var existing = devices.FirstOrDefault(d => !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId == device.DeviceId);

        // For WiFi devices where DeviceId might have changed earlier versions, fall back to SSID match
        if (existing == null && device.ConnectionType == ConnectionType.Wifi && !string.IsNullOrEmpty(device.Ssid))
        {
            existing = devices.FirstOrDefault(d => d.ConnectionType == ConnectionType.Wifi && d.Ssid == device.Ssid);
            if (existing != null && string.IsNullOrEmpty(device.DeviceId))
            {
                // Adopt existing DeviceId if incoming one is empty
                device.DeviceId = existing.DeviceId;
            }
        }

        if (existing != null)
        {
            System.Diagnostics.Debug.WriteLine($"📝 Updating existing device (DeviceId={existing.DeviceId})");
            // Preserve DeviceId and replace mutable fields
            var index = devices.IndexOf(existing);
            devices[index] = device;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("➕ Adding new device entry");
            devices.Add(device);
        }

        await SaveDeviceListToSecureStoreAsync(devices);
        System.Diagnostics.Debug.WriteLine("✅ Device list persisted (upsert)");
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

    public async Task<List<DeviceModel>> GetSavedLocalDevicesAsync()
    {
        try
        {
            var (isSuccessful, isEmpty, devices) = await LoadDeviceListAsync();
            
            if (!isSuccessful)
            {
                Debug.WriteLine("❌ Failed to load device list for local devices");
                return new List<DeviceModel>();
            }
            
            if (isEmpty)
            {
                Debug.WriteLine("📭 No devices found in storage");
                return new List<DeviceModel>();
            }
            
            // Filter for local devices only
            var localDevices = devices.Where(d => d.ConnectionType == ConnectionType.Local).ToList();
            
            Debug.WriteLine($"🏠 Found {localDevices.Count} saved local devices");
            
            return localDevices;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error getting saved local devices: {ex.Message}");
            return new List<DeviceModel>();
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

    // --- Neue Methoden für komplettes Speichern ---
    public async Task SaveLocalDevicesAsync(List<DeviceModel> devices)
    {
        var (success, _, allDevices) = await LoadDeviceListAsync();
        if (!success) allDevices = new List<DeviceModel>();
        allDevices.RemoveAll(d => d.ConnectionType == ConnectionType.Local);
        allDevices.AddRange(devices);
        await SaveDeviceListToSecureStoreAsync(allDevices);
    }

    public async Task SaveWifiDevicesAsync(List<DeviceModel> devices)
    {
        var (success, _, allDevices) = await LoadDeviceListAsync();
        if (!success) allDevices = new List<DeviceModel>();
        allDevices.RemoveAll(d => d.ConnectionType == ConnectionType.Wifi);
        allDevices.AddRange(devices);
        await SaveDeviceListToSecureStoreAsync(allDevices);
    }

}
