using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

#if ANDROID
using ReisingerIntelliApp_V4.Platforms.Android;
#endif

namespace ReisingerIntelliApp_V4.ViewModels
{
    public partial class WifiScanViewModel : ObservableObject
    {
#if ANDROID
        private readonly AndroidWifiService _androidWifiService;
#endif
        private readonly INavigationService _navigationService;
        private readonly IDeviceService _deviceService;

        [ObservableProperty]
        private ObservableCollection<NetworkDataModel> wifiNetworks = new();

        [ObservableProperty]
        private bool isScanning = false;

        [ObservableProperty]
        private bool hasNetworks = false;

        [ObservableProperty]
        private string scanStatusText = "Ready to scan";

        [ObservableProperty]
        private NetworkDataModel? currentNetwork;

        [ObservableProperty]
        private string title = "WiFi Scan";

        public WifiScanViewModel(
            INavigationService navigationService,
            IDeviceService deviceService)
        {
#if ANDROID
            _androidWifiService = new AndroidWifiService();
#endif
            _navigationService = navigationService;
            _deviceService = deviceService;
        }

        [RelayCommand]
        public async Task BackAsync()
        {
            await _navigationService.NavigateBackAsync();
        }

        [RelayCommand]
        public async Task ScanForNetworksAsync()
        {
            await PerformScanAsync();
        }

        [RelayCommand]
        public async Task RefreshNetworksAsync()
        {
            await PerformScanAsync();
        }

        [RelayCommand]
        public async Task ConnectToNetworkAsync(NetworkDataModel network)
        {
            if (network == null) return;

            try
            {
                ScanStatusText = $"Connecting to {network.SsidName}...";
                await Task.Delay(100); // Ensure async behavior
                
#if ANDROID
                if (_androidWifiService != null)
                {
                    // For now, return false since we need password
                    // In a real implementation, you'd show a password dialog
                    Debug.WriteLine("❌ Password required for connection");
                    ScanStatusText = $"Password required for {network.SsidName}";
                }
                else
                {
                    ScanStatusText = "WiFi service unavailable";
                }
#else
                ScanStatusText = "Connection not available on this platform";
#endif
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error connecting to network: {ex.Message}");
                ScanStatusText = "Connection failed";
            }
        }

        public async Task LoadNetworksOnAppearingAsync()
        {
            Debug.WriteLine("📱 WifiScanPage appearing - starting automatic scan");
            await PerformScanAsync();
        }

        private async Task PerformScanAsync()
        {
            try
            {
                IsScanning = true;
                ScanStatusText = "Scanning for WiFi networks...";
                HasNetworks = false;

                Debug.WriteLine("🔍 Starting WiFi network scan...");

#if ANDROID
                if (_androidWifiService != null)
                {
                    // Get current network SSID first
                    var currentNetworkSsid = _androidWifiService.GetCurrentNetworkSsid();
                    Debug.WriteLine($"🔗 Current connected network: '{currentNetworkSsid}'");

                    // Clear existing networks
                    WifiNetworks.Clear();

                    // Scan for networks
                    var scanResults = await _androidWifiService.ScanWifiNetworksAsync();
                    
                    foreach (var result in scanResults)
                    {
                        // Check if this is the currently connected network
                        var isConnected = !string.IsNullOrEmpty(currentNetworkSsid) && 
                                        string.Equals(result.Ssid?.Trim(), currentNetworkSsid?.Trim(), StringComparison.OrdinalIgnoreCase);

                        // Check if device is already saved
                        var isAlreadySaved = await _deviceService.DeviceExistsBySsidAsync(result.Ssid);

                        var network = new NetworkDataModel
                        {
                            Ssid = result.Ssid,
                            Name = result.Ssid,
                            Bssid = result.Bssid,
                            SignalStrength = result.SignalLevel, // Use SignalLevel from WifiScanResult
                            SecurityType = result.SecurityType,
                            IsConnected = isConnected,
                            IsAlreadySaved = isAlreadySaved
                        };

                        WifiNetworks.Add(network);
                        
                        // Set current network if this matches the current SSID
                        if (isConnected)
                        {
                            CurrentNetwork = network;
                        }
                        
                        Debug.WriteLine($"📶 Found network: {network.Ssid} (Signal: {network.SignalStrength}, Security: {network.SecurityType}, Connected: {network.IsConnected}, Saved: {network.IsAlreadySaved})");
                    }

                    HasNetworks = WifiNetworks.Count > 0;
                    ScanStatusText = WifiNetworks.Count > 0 ? $"Found {WifiNetworks.Count} networks" : "No networks found";
                    Debug.WriteLine($"✅ Scan completed. Found {WifiNetworks.Count} networks.");
                }
                else
                {
                    Debug.WriteLine("❌ AndroidWifiService is null");
                    ScanStatusText = "WiFi service unavailable";
                }
#else
                await Task.Delay(100); // Ensure async behavior on other platforms
                ScanStatusText = "WiFi scanning not available on this platform";
#endif
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error scanning networks: {ex.Message}");
                ScanStatusText = "Scan failed - Please try again";
                HasNetworks = false;
            }
            finally
            {
                IsScanning = false;
            }
        }

        [RelayCommand]
        public async Task NetworkSelectedAsync(NetworkDataModel network)
        {
            if (network == null) return;

            // Don't allow selection of already saved devices
            if (network.IsAlreadySaved)
            {
                Debug.WriteLine($"⚠️ Device {network.Ssid} is already saved - selection blocked");
                return;
            }

            try
            {
                Debug.WriteLine($"🔗 Network selected: {network.Ssid}");

                // Check if already connected to this network
                if (network.IsConnected)
                {
                    // Navigate directly to save device page
                    await NavigateToSaveDevicePage(network);
                }
                else
                {
                    // For demo purposes, we'll navigate to save device page directly
                    // In a real app, you'd first connect to the network
                    await NavigateToSaveDevicePage(network);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error selecting network: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddDeviceAsync(NetworkDataModel network)
        {
            try
            {
                // Check if device already exists
                if (await _deviceService.DeviceExistsBySsidAsync(network.Ssid))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Device Already Exists", 
                        $"A device with SSID '{network.Ssid}' is already saved.", 
                        "OK");
                    return;
                }

                // Navigate to save device page
                await NavigateToSaveDevicePage(network);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to add device: {ex.Message}", 
                    "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteDeviceAsync(NetworkDataModel network)
        {
            try
            {
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Delete Device",
                    $"Are you sure you want to delete the device '{network.SsidName}'?",
                    "Delete",
                    "Cancel");

                if (confirm)
                {
                    // Check if device exists and delete it
                    if (await _deviceService.DeviceExistsBySsidAsync(network.Ssid))
                    {
                        // Get the device by SSID and delete it
                        var devices = await _deviceService.GetSavedWifiDevicesAsync();
                        var deviceToDelete = devices.FirstOrDefault(d => d.Ssid == network.Ssid);
                        
                        if (deviceToDelete != null)
                        {
                            await _deviceService.DeleteDeviceAsync(deviceToDelete);
                            
                            // Update the network model to reflect that it's no longer saved
                            network.IsAlreadySaved = false;
                            
                            await Application.Current.MainPage.DisplayAlert(
                                "Success",
                                $"Device '{network.SsidName}' has been deleted.",
                                "OK");
                                
                            // Refresh the network list to update UI
                            await PerformScanAsync();
                        }
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Not Found",
                            $"No saved device found with SSID '{network.SsidName}'.",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting device: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to delete device: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task ShowOptionsAsync(NetworkDataModel network)
        {
            try
            {
                var options = new List<string>();
                
                if (network.IsAlreadySaved)
                {
                    options.Add("Edit Device");
                    options.Add("View Details");
                    options.Add("Delete Device");
                }
                else
                {
                    options.Add("Connect");
                    options.Add("View Network Info");
                    options.Add("Add as Device");
                }
                
                options.Add("Cancel");

                var action = await Application.Current.MainPage.DisplayActionSheet(
                    $"Options for '{network.SsidName}'",
                    "Cancel",
                    null,
                    options.ToArray());

                switch (action)
                {
                    case "Edit Device":
                        await NavigateToSaveDevicePage(network);
                        break;
                        
                    case "View Details":
                        await ShowNetworkDetailsAsync(network);
                        break;
                        
                    case "Delete Device":
                        await DeleteDeviceAsync(network);
                        break;
                        
                    case "Connect":
                        await ConnectToNetworkAsync(network);
                        break;
                        
                    case "View Network Info":
                        await ShowNetworkDetailsAsync(network);
                        break;
                        
                    case "Add as Device":
                        await AddDeviceAsync(network);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error showing options: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Failed to show options: {ex.Message}",
                    "OK");
            }
        }

        private async Task ShowNetworkDetailsAsync(NetworkDataModel network)
        {
            var details = $"Network Name: {network.SsidName}\n" +
                         $"BSSID: {network.Bssid}\n" +
                         $"Signal Strength: {network.SignalStrengthText}\n" +
                         $"Security: {network.SecurityTypeText}\n" +
                         $"Status: {network.ConnectedStatus}";

            if (network.IsAlreadySaved)
            {
                details += $"\nDevice ID: {network.DeviceId}\n" +
                          $"Serial Number: {network.SerialNumber}\n" +
                          $"Firmware: {network.FirmwareVersion}";
            }

            await Application.Current.MainPage.DisplayAlert(
                "Network Details",
                details,
                "OK");
        }

        private async Task NavigateToSaveDevicePage(NetworkDataModel network)
        {
            try
            {
                // Serialize network data for passing as parameter
                var networkJson = System.Text.Json.JsonSerializer.Serialize(network);
                var encodedData = Uri.EscapeDataString(networkJson);
                
                await Shell.Current.GoToAsync($"savedevice?networkData={encodedData}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error navigating to save device page: {ex.Message}");
            }
        }
    }
}
