using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4.ViewModels;

public partial class LocalDevicesScanPageViewModel : ObservableObject
{
    private readonly IDeviceService _deviceService;
    private readonly IntellidriveApiService _apiService;
    private CancellationTokenSource? _cancellationTokenSource;
    // Slightly higher timeout for on-device LAN scanning due to WiFi latency & Android scheduler
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMilliseconds(2500) };
    private const int MaxConcurrency = 20; // Optimiert für bessere Performance

    public LocalDevicesScanPageViewModel(IDeviceService deviceService, IntellidriveApiService apiService)
    {
        _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        
        LocalDevices = new ObservableCollection<LocalNetworkDeviceModel>();
        
        // Set default IP range for most common local networks
        StartIp = "192.168.0.1";
        EndIp = "192.168.0.254";

        // Füge beim Start 3 Dummy-Geräte hinzu, wenn keine echten vorhanden sind
        if (LocalDevices.Count == 0)
        {
            for (int i = 1; i <= 3; i++)
            {
                LocalDevices.Add(new LocalNetworkDeviceModel
                {
                    DeviceId = $"dummy_local_{i}",
                    Name = $"Dummy Local Device {i}", // Use Name property, not DisplayName
                    IpAddress = $"192.168.1.10{i}",
                    IsOnline = false,
                    IsAlreadySaved = false
                });
            }
        }

        // When a device is saved from SaveLocalDevicePage, mark it as saved here so user can continue adding others
    MessagingCenter.Subscribe<SaveLocalDevicePageViewModel, string>(this, "LocalDeviceAdded", (sender, savedDeviceId) =>
        {
            try
            {
        var match = LocalDevices.FirstOrDefault(d => d.DeviceId == savedDeviceId);
                if (match != null)
                {
                    match.IsAlreadySaved = true;
                }
            }
            catch { }
        });
    }

    [ObservableProperty]
    private ObservableCollection<LocalNetworkDeviceModel> localDevices;

    [ObservableProperty]
    private string startIp = "192.168.1.1";

    [ObservableProperty]
    private string endIp = "192.168.1.254";

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private string scanStatusMessage = "Bereit für lokalen Netzwerk-Scan";

    [ObservableProperty]
    private int scannedCount = 0;

    [ObservableProperty]
    private int totalCount = 0;

    [ObservableProperty]
    private double progressPercentage = 0;

    [ObservableProperty]
    private int foundDevicesCount = 0;

    [ObservableProperty]
    private LocalNetworkDeviceModel? selectedDevice;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool hasValidationError = false;

    [RelayCommand]
    private async Task StartLocalScanAsync()
    {
        if (IsScanning) return;

        try
        {
            // Cancel any existing scan
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            IsScanning = true;
            ScannedCount = 0;
            FoundDevicesCount = 0;
            ProgressPercentage = 0;
            LocalDevices.Clear();
            ScanStatusMessage = "🔍 Starte lokalen Netzwerk-Scan...";

            Debug.WriteLine($"🏠 Starting local network scan from {StartIp} to {EndIp}");

            // Validate IP addresses
            if (!IsValidIpAddress(StartIp) || !IsValidIpAddress(EndIp))
            {
                ScanStatusMessage = "❌ Ungültige IP-Adressen eingegeben. Bitte prüfen Sie das Format (z.B. 192.168.1.1)";
                ValidationMessage = "Ungültige IP-Adressen eingegeben";
                HasValidationError = true;
                return;
            }

            // Validate IP range
            if (!IsValidIpRange(StartIp, EndIp))
            {
                ScanStatusMessage = "❌ Start-IP muss kleiner oder gleich End-IP sein";
                ValidationMessage = "Start-IP muss kleiner oder gleich End-IP sein";
                HasValidationError = true;
                return;
            }

            // Clear validation errors
            ValidationMessage = string.Empty;
            HasValidationError = false;

            // Generate IP range
            var ipList = GenerateIpRange(StartIp, EndIp);
            if (ipList.Count == 0)
            {
                ScanStatusMessage = "❌ Fehler beim Generieren des IP-Bereichs";
                return;
            }

            if (ipList.Count > 1000)
            {
                ScanStatusMessage = "⚠️ IP-Bereich zu groß (max. 1000 IPs). Bitte verkleinern Sie den Bereich.";
                return;
            }

            TotalCount = ipList.Count;
            ScanStatusMessage = $"🔍 Scanne {TotalCount} IP-Adressen...";

            Debug.WriteLine($"🚀 Starting scan of {TotalCount} IPs");

            // Start parallel scan with live updates
            await ScanIpRangeWithLiveUpdatesAsync(ipList, _cancellationTokenSource.Token);

            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                ScanStatusMessage = LocalDevices.Count > 0 
                    ? $"✅ Scan abgeschlossen: {LocalDevices.Count} Intellidrive-Geräte gefunden"
                    : "⚠️ Keine Intellidrive-Geräte im angegebenen Bereich gefunden";
            }

            Debug.WriteLine($"🎯 Local scan completed: {LocalDevices.Count} devices found");
        }
        catch (OperationCanceledException)
        {
            ScanStatusMessage = "⏹️ Scan abgebrochen";
            Debug.WriteLine("🛑 Local scan was cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error during local scan: {ex.Message}");
            ScanStatusMessage = $"❌ Fehler beim Scannen: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task ScanIpRangeWithLiveUpdatesAsync(List<string> ipAddresses, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var scannedCounter = 0;

        var tasks = ipAddresses.Select(async ip =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Update current scanning IP immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ScanStatusMessage = $"🔍 Scanne gerade: {ip}";
                });

                var device = await TestSingleIpForIntellidriveAsync(ip, cancellationToken);
                if (device != null)
                {
                    // Ensure we mark already-saved devices by comparing with saved list
                    try
                    {
                        var saved = await _deviceService.GetSavedLocalDevicesAsync();
                        if (saved.Any(d => d.DeviceId == device.DeviceId))
                        {
                            device.IsAlreadySaved = true;
                        }
                    }
                    catch { }
                    // UI-Update auf Main Thread - Sofortiges Hinzufügen zur Liste
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LocalDevices.Add(device);
                        FoundDevicesCount = LocalDevices.Count;
                        ScanStatusMessage = $"✅ Gefunden: {device.DisplayName} ({device.IpAddress}) - Scan läuft weiter...";
                    });

                    Debug.WriteLine($"✅ Found Intellidrive device: {device.DisplayName} at {device.IpAddress}");
                }

                // Progress Update
                var completed = Interlocked.Increment(ref scannedCounter);
                var progress = (double)completed / TotalCount * 100;
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ScannedCount = completed;
                    ProgressPercentage = progress;
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, just return
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<LocalNetworkDeviceModel?> TestSingleIpForIntellidriveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ipAddress}/intellidrive/version");
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                Debug.WriteLine($"🔍 Response from {ipAddress}: {content}");
                
                // Prüfe ob es eine gültige Intellidrive JSON-Response ist
                if (IsValidIntellidriveResponse(content))
                {
                    return await CreateDeviceFromResponseAsync(ipAddress, content, stopwatch.ElapsedMilliseconds, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"⚠️ IP {ipAddress} HTTP error: {httpEx.Message} ({httpEx.StatusCode})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ IP {ipAddress} test failed: {ex.GetType().Name}: {ex.Message}");
        }

        return null;
    }

    private static bool IsValidIntellidriveResponse(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            // Prüfe auf typische Intellidrive JSON-Inhalte
            return jsonContent.Contains("\"Success\":true") || 
                   jsonContent.Contains("\"success\":true") ||
                   jsonContent.Contains("\"DeviceId\"") || 
                   jsonContent.Contains("\"deviceId\"") ||
                   jsonContent.Contains("\"version\"") ||
                   jsonContent.Contains("\"Version\"") ||
                   jsonContent.Contains("intellidrive") ||
                   jsonContent.Contains("Intellidrive");
        }
        catch
        {
            return false;
        }
    }

    private async Task<LocalNetworkDeviceModel> CreateDeviceFromResponseAsync(string ipAddress, string jsonResponse, long responseTimeMs, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Versuche zusätzliche Geräteinformationen zu holen
            string deviceId = ExtractValueFromJson(jsonResponse, "DeviceId") ?? 
                            ExtractValueFromJson(jsonResponse, "deviceId") ?? 
                            Guid.NewGuid().ToString();
            
            string firmwareVersion = ExtractValueFromJson(jsonResponse, "Version") ?? 
                                   ExtractValueFromJson(jsonResponse, "version") ?? 
                                   ExtractValueFromJson(jsonResponse, "firmware") ??
                                   "Unknown";

            // Versuche Serial Number aus der Version-Response (Content.DEVICE_SERIALNO) zu lesen,
            // und falle nur bei Bedarf auf den separaten API-Call zurück
            string serialNumber = ExtractValueFromJson(jsonResponse, "DEVICE_SERIALNO") ?? "Unknown";
            if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber == "Unknown")
            {
                try
                {
                    using var serialRequest = new HttpRequestMessage(HttpMethod.Get, $"http://{ipAddress}/intellidrive/device_id");
                    var serialResponse = await _httpClient.SendAsync(serialRequest, cancellationToken);
                    if (serialResponse.IsSuccessStatusCode)
                    {
                        var serialContent = await serialResponse.Content.ReadAsStringAsync(cancellationToken);
                        serialNumber = ExtractValueFromJson(serialContent, "DEVICE_ID") ?? 
                                     ExtractValueFromJson(serialContent, "device_id") ?? 
                                     ExtractValueFromJson(serialContent, "SerialNumber") ?? 
                                     "Unknown";
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw cancellation
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Could not get serial for {ipAddress}: {ex.Message}");
                }
            }

            var model = new LocalNetworkDeviceModel
            {
                Id = deviceId,
                DeviceId = deviceId,
                Name = $"Intellidrive {serialNumber}",
                IpAddress = ipAddress,
                LastSeen = DateTime.Now,
                IsOnline = true,
                DeviceType = "Intellidrive",
                FirmwareVersion = firmwareVersion,
                SerialNumber = serialNumber,
                ResponseTime = DateTime.Now,
                DiscoveredAt = DateTime.Now
            };
            try
            {
                var saved = await _deviceService.GetSavedLocalDevicesAsync();
                if (saved.Any(d => d.DeviceId == model.DeviceId))
                {
                    model.IsAlreadySaved = true;
                }
            }
            catch { }
            return model;
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Error creating device model for {ipAddress}: {ex.Message}");
            
            var fallback = new LocalNetworkDeviceModel
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = Guid.NewGuid().ToString(),
                Name = $"Intellidrive {ipAddress}",
                IpAddress = ipAddress,
                LastSeen = DateTime.Now,
                IsOnline = true,
                DeviceType = "Intellidrive",
                FirmwareVersion = "Unknown",
                SerialNumber = "Unknown",
                ResponseTime = DateTime.Now,
                DiscoveredAt = DateTime.Now
            };
            try
            {
                var saved = await _deviceService.GetSavedLocalDevicesAsync();
                if (saved.Any(d => d.DeviceId == fallback.DeviceId))
                {
                    fallback.IsAlreadySaved = true;
                }
            }
            catch { }
            return fallback;
        }
    }

    private static string? ExtractValueFromJson(string json, string key)
    {
        try
        {
            var pattern = $"\"{key}\":\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> GenerateIpRange(string startIp, string endIp)
    {
        var ips = new List<string>();
        try
        {
            var start = IPAddress.Parse(startIp).GetAddressBytes();
            var end = IPAddress.Parse(endIp).GetAddressBytes();

            uint startInt = BitConverter.ToUInt32(start.Reverse().ToArray(), 0);
            uint endInt = BitConverter.ToUInt32(end.Reverse().ToArray(), 0);

            for (uint i = startInt; i <= endInt; i++)
            {
                var bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                ips.Add(new IPAddress(bytes).ToString());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error generating IP range: {ex.Message}");
        }

        return ips;
    }

    [RelayCommand]
    private void StopScan()
    {
        if (IsScanning && _cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            ScanStatusMessage = "⏹️ Scan wird abgebrochen...";
            Debug.WriteLine("🛑 Local scan cancellation requested by user");
        }
    }

    [RelayCommand]
    private async Task SaveSelectedDeviceAsync()
    {
        if (SelectedDevice == null)
        {
            ScanStatusMessage = "⚠️ Kein Gerät zum Speichern ausgewählt";
            return;
        }

        try
        {
            // Navigiere zur Save Device Page mit dem ausgewählten Gerät
            var parameters = new Dictionary<string, object>
            {
                ["Device"] = SelectedDevice
            };
            
            await Shell.Current.GoToAsync("//SaveDevicePage", parameters);
            Debug.WriteLine($"🔗 Navigating to save device: {SelectedDevice.DisplayName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error navigating to save device: {ex.Message}");
            ScanStatusMessage = $"❌ Fehler beim Öffnen der Speichern-Seite: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveDeviceAsync(LocalNetworkDeviceModel device)
    {
        try
        {
            Debug.WriteLine($"🔗 Navigating to save device: {device.DisplayName}");

            // Create device model for saving
            var deviceModel = new DeviceModel
            {
                DeviceId = device.DeviceId,
                Name = device.DisplayName,
                IpAddress = device.IpAddress,
                SerialNumber = device.SerialNumber,
                FirmwareVersion = device.FirmwareVersion,
                Type = AppDeviceType.LocalDevice,
                ConnectionType = ConnectionType.Local,
                LastSeen = device.LastSeen,
                IsOnline = device.IsOnline,
                Ip = device.IpAddress
            };

            // Save device via service
            await _deviceService.SaveDeviceAsync(deviceModel);
            
            // Mark as saved in UI
            device.IsAlreadySaved = true;
            
            ScanStatusMessage = $"✅ Gerät '{device.DisplayName}' erfolgreich gespeichert";
            
            Debug.WriteLine($"✅ Device saved successfully: {device.DisplayName}");
            
            // Trigger a refresh of the MainPage Local Devices dropdown if it's open
            MessagingCenter.Send(this, "LocalDeviceAdded");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error saving device: {ex.Message}");
            ScanStatusMessage = $"❌ Fehler beim Speichern: {ex.Message}";
        }
    }

    // Mirrors WifiScanViewModel.AddDevice: navigate to save page with prefilled local device data
    [RelayCommand]
    private async Task AddDeviceAsync(LocalNetworkDeviceModel device)
    {
        try
        {
            if (device == null)
            {
                ScanStatusMessage = "⚠️ Kein Gerät ausgewählt";
                return;
            }

            // Serialize minimal local device data for navigation
            var payload = new
            {
                ip = device.IpAddress,
                name = device.DisplayName,
                serial = device.SerialNumber,
                firmware = device.FirmwareVersion,
                deviceId = device.DeviceId
            };

            var json = JsonSerializer.Serialize(payload);
            var encoded = Uri.EscapeDataString(json);

            await Shell.Current.GoToAsync($"savelocaldevice?deviceData={encoded}");
            Debug.WriteLine($"🔗 Navigating to SaveLocalDevicePage for {device.DisplayName} ({device.IpAddress})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error navigating to SaveLocalDevicePage: {ex.Message}");
            ScanStatusMessage = $"❌ Navigation fehlgeschlagen: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectDevice(LocalNetworkDeviceModel device)
    {
        SelectedDevice = device;
        Debug.WriteLine($"📱 Selected device: {device.DisplayName} at {device.IpAddress}");
    }

    [RelayCommand]
    private void SetCommonIpRange(string range)
    {
        switch (range.ToLower())
        {
            case "192.168.1.x":
                StartIp = "192.168.1.1";
                EndIp = "192.168.1.254";
                break;
            case "192.168.0.x":
                StartIp = "192.168.0.1";
                EndIp = "192.168.0.254";
                break;
            case "10.0.0.x":
                StartIp = "10.0.0.1";
                EndIp = "10.0.0.254";
                break;
            case "172.16.0.x":
                StartIp = "172.16.0.1";
                EndIp = "172.16.0.254";
                break;
            default:
                Debug.WriteLine($"⚠️ Unknown IP range preset: {range}");
                break;
        }
        
        ScanStatusMessage = $"📋 IP-Bereich gesetzt: {StartIp} - {EndIp}";
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    private static bool IsValidIpRange(string startIp, string endIp)
    {
        try
        {
            var start = IPAddress.Parse(startIp).GetAddressBytes();
            var end = IPAddress.Parse(endIp).GetAddressBytes();

            uint startInt = BitConverter.ToUInt32(start.Reverse().ToArray(), 0);
            uint endInt = BitConverter.ToUInt32(end.Reverse().ToArray(), 0);

            return startInt <= endInt;
        }
        catch
        {
            return false;
        }
    }
}
