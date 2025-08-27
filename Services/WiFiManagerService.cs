using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ReisingerIntelliApp_V4.Models;
using Plugin.MauiWifiManager;
#if ANDROID
using ReisingerIntelliApp_V4.Platforms.Android;
#endif

namespace ReisingerIntelliApp_V4.Services
{
    public class WiFiManagerService
    {
        #if ANDROID
        private readonly AndroidWifiService _androidWifi;
        #endif
        public WiFiManagerService()
        {
            #if ANDROID
            _androidWifi = new AndroidWifiService();
            #endif
        }

        public async Task<ObservableCollection<NetworkDataModel>> ScanNetworksAsync()
        {
            var networks = new ObservableCollection<NetworkDataModel>();

            try
            {
                Debug.WriteLine("🔍 Starting WiFi network scan...");

                var currentNetworkSsid = await GetCurrentNetworkSsidAsync();
                Debug.WriteLine($"🔗 Current connected network: '{currentNetworkSsid}'");

                var scanResults = await CrossWifiManager.Current.ScanWifiNetworks();
                
                foreach (var result in scanResults)
                {
                    // Check if this is the currently connected network
                    var isConnected = !string.IsNullOrEmpty(currentNetworkSsid) && 
                                    string.Equals(result.Ssid?.Trim(), currentNetworkSsid?.Trim(), StringComparison.OrdinalIgnoreCase);

                    var network = new NetworkDataModel
                    {
                        Ssid = result.Ssid,
                        Name = result.Ssid,
                        Bssid = result.Bssid,
                        SignalStrength = result.SignalStrength,
                        SecurityType = result.SecurityType?.ToString(),
                        IsConnected = isConnected,
                        IsAlreadySaved = false
                    };

                    networks.Add(network);
                    Debug.WriteLine($"📶 Found network: {network.Ssid} (Signal: {network.SignalStrength}, Security: {network.SecurityType}, Connected: {network.IsConnected})");
                }

                Debug.WriteLine($"✅ WiFi scan completed. Found {networks.Count} networks.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error scanning WiFi networks: {ex.Message}");
            }

            return networks;
        }

        public async Task<string> GetCurrentNetworkSsidAsync()
        {
            try
            {
                #if ANDROID
                // Prefer platform service on Android for reliability
                var ssid = _androidWifi?.GetCurrentNetworkSsid();
                if (!string.IsNullOrEmpty(ssid)) return ssid;
                #endif

                var networkInfo = await CrossWifiManager.Current.GetNetworkInfo();
                return networkInfo?.Ssid ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting current network: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<(bool success, string message)> ConnectToWifiNetworkAsync(string ssid, string password)
        {
            try
            {
                Debug.WriteLine($"🔄 Attempting to connect to WiFi network: {ssid}");
                #if ANDROID
                // Prefer platform API on Android (works across OS versions and avoids plugin NRE)
                var (ok, msg) = await _androidWifi.ConnectToWifiNetworkAsync(ssid, password);
                if (!ok)
                {
                    Debug.WriteLine($"❌ AndroidWifiService connect failed: {msg}");
                }
                return (ok, msg);
                #else
                // Guard: ensure plugin is available
                if (CrossWifiManager.Current == null)
                {
                    Debug.WriteLine("❌ CrossWifiManager.Current is null - plugin may not be initialized");
                    return (false, "Connection Error: WiFi plugin not initialized (CrossWifiManager.Current is null)");
                }

                var connectResult = await CrossWifiManager.Current.ConnectWifi(ssid, password);

                if (connectResult?.NativeObject != null)
                {
                    Debug.WriteLine($"✅ Successfully connected to WiFi network: {ssid}");
                    return (true, "Successfully connected");
                }
                else
                {
                    Debug.WriteLine($"❌ Failed to connect to WiFi network: {ssid}");
                    return (false, "Connection failed: plugin returned null result");
                }
                #endif
            }
            catch (Exception ex)
            {
                // Log full exception to help identify NullReferenceException source
                Debug.WriteLine($"❌ Error connecting to WiFi network {ssid}: {ex}");
                return (false, $"Connection Error: {ex.Message}");
            }
        }

        public async Task<bool> ReconnectToPreviousNetworkAsync(string previousSsid)
        {
            try
            {
                if (string.IsNullOrEmpty(previousSsid))
                {
                    Debug.WriteLine("⚠️ No previous network SSID provided for reconnection");
                    return false;
                }

                Debug.WriteLine($"🔄 Attempting to reconnect to previous network: {previousSsid}");

                #if ANDROID
                var ok = await _androidWifi.ReconnectToPreviousNetworkAsync(previousSsid);
                return ok;
                #else
                // Guard: ensure plugin is available
                if (CrossWifiManager.Current == null)
                {
                    Debug.WriteLine("❌ CrossWifiManager.Current is null - plugin may not be initialized");
                    return false;
                }

                // Try to connect to the previous network (assuming it's saved)
                var reconnectResult = await CrossWifiManager.Current.ConnectWifi(previousSsid, "");
                if (reconnectResult?.NativeObject != null)
                {
                    Debug.WriteLine($"✅ Successfully reconnected to previous network: {previousSsid}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ Failed to reconnect to previous network: {previousSsid}");
                    return false;
                }
                #endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error reconnecting to previous network {previousSsid}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsNetworkAvailableAsync(string ssid)
        {
            try
            {
                var networks = await ScanNetworksAsync();
                return networks.Any(n => string.Equals(n.Ssid, ssid, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error checking network availability: {ex.Message}");
                return false;
            }
        }

        public async Task OpenWifiSettingsAsync()
        {
            try
            {
                Debug.WriteLine("🔧 Opening WiFi settings...");
                #if ANDROID
                await _androidWifi.OpenWifiSettingsAsync();
                #else
                // On other platforms, this would need platform-specific implementation
                Debug.WriteLine("⚠️ OpenWifiSettings not implemented for this platform");
                #endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error opening WiFi settings: {ex.Message}");
            }
        }
    }
}
