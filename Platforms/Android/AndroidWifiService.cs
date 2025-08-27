using Android.Content;
using Android.Net.Wifi;
using AndroidX.Core.App;
using System.Collections.Generic;
using System.Diagnostics;
using Java.Util;
using Android.Content.PM;
using Android.Net;
using Android.Provider;
using System.Reflection;

namespace ReisingerIntelliApp_V4.Platforms.Android
{
    public class AndroidWifiService
    {
        private WifiManager? _wifiManager;
        private Context? _context;
        private ConnectivityManager? _connectivityManager;

        public AndroidWifiService()
        {
            _context = Platform.CurrentActivity ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? global::Android.App.Application.Context;
            _wifiManager = _context?.GetSystemService(Context.WifiService) as WifiManager;
            _connectivityManager = _context?.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
        }

        public async Task<List<WifiScanResult>> ScanWifiNetworksAsync()
        {
            var results = new List<WifiScanResult>();

            try
            {
                if (_wifiManager == null || _context == null)
                {
                    Debug.WriteLine("❌ WiFi Manager or Context is null");
                    return results;
                }

                // Check if WiFi is enabled
                if (!_wifiManager.IsWifiEnabled)
                {
                    Debug.WriteLine("⚠️ WiFi is disabled");
                    return results;
                }

                // Check permissions
                if (!HasLocationPermission())
                {
                    Debug.WriteLine("⚠️ Location permission not granted - required for WiFi scanning");
                    await RequestLocationPermissionAsync();
                    return results;
                }

                Debug.WriteLine("🔍 Starting WiFi scan...");

                // Start WiFi scan
                bool scanStarted = _wifiManager.StartScan();
                
                if (!scanStarted)
                {
                    Debug.WriteLine("❌ Failed to start WiFi scan");
                    return results;
                }

                // Wait for scan to complete
                await Task.Delay(3000);

                // Get scan results
                var scanResults = _wifiManager.ScanResults;
                
                if (scanResults != null)
                {
                    Debug.WriteLine($"✅ Found {scanResults.Count} WiFi networks");

                    foreach (var scanResult in scanResults)
                    {
                        if (!string.IsNullOrEmpty(scanResult.Ssid))
                        {
                            var wifiResult = new WifiScanResult
                            {
                                Ssid = scanResult.Ssid,
                                Bssid = scanResult.Bssid,
                                SignalLevel = scanResult.Level,
                                Frequency = scanResult.Frequency,
                                Capabilities = scanResult.Capabilities
                            };

                            results.Add(wifiResult);
                            Debug.WriteLine($"📶 {scanResult.Ssid} | Signal: {scanResult.Level} dBm | Security: {scanResult.Capabilities}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("⚠️ No scan results available");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error scanning WiFi networks: {ex.Message}");
            }

            return results;
        }

        public string? GetCurrentNetworkSsid()
        {
            try
            {
                // Try newer API first (Android 10+)
                var currentSsid = GetCurrentSsidViaConnectivityManager();
                if (!string.IsNullOrEmpty(currentSsid))
                {
                    Debug.WriteLine($"📶 Found current SSID via ConnectivityManager: '{currentSsid}'");
                    return currentSsid;
                }

                // Fallback to older WifiManager approach
                if (_wifiManager?.ConnectionInfo != null)
                {
                    var wifiInfo = _wifiManager.ConnectionInfo;
                    
                    // Try to get SSID from NetworkId lookup
                    var ssid = GetSsidFromNetworkId(wifiInfo.NetworkId);
                    
                    // Fallback: try reflection to get SSID property (might work on some versions)
                    if (string.IsNullOrEmpty(ssid))
                    {
                        ssid = GetSsidViaReflection(wifiInfo);
                    }
                    
                    // Final fallback: check if we can get any network name
                    if (string.IsNullOrEmpty(ssid) && wifiInfo.NetworkId >= 0)
                    {
                        ssid = $"Network_{wifiInfo.NetworkId}";
                        Debug.WriteLine($"⚠️ Using NetworkId as fallback: {ssid}");
                    }
                    
                    var cleanSsid = ssid?.Replace("\"", "").Trim();
                    Debug.WriteLine($"📶 Found current network SSID: '{cleanSsid}' (NetworkId: {wifiInfo.NetworkId})");
                    return !string.IsNullOrEmpty(cleanSsid) ? cleanSsid : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting current network: {ex.Message}");
            }

            return null;
        }

        public async Task<(bool Success, string Message)> ConnectToWifiNetworkAsync(string ssid, string password)
        {
            try
            {
                Debug.WriteLine($"🔄 Attempting to connect to WiFi network: {ssid}");
                
                if (_wifiManager == null || _context == null)
                {
                    return (false, "WiFi Manager nicht verfügbar");
                }

                if (!_wifiManager.IsWifiEnabled)
                {
                    return (false, "WiFi ist nicht aktiviert");
                }

                // Store current network for reconnection
                var currentNetwork = GetCurrentNetworkSsid();
                Debug.WriteLine($"📶 Current network before connection attempt: {currentNetwork}");

                // Create WiFi configuration
                var wifiConfig = new WifiConfiguration
                {
                    Ssid = $"\"{ssid}\""
                };
                // If password is empty, treat as open network; else use WPA/WPA2 pre-shared key
                if (string.IsNullOrWhiteSpace(password))
                {
                    wifiConfig.AllowedKeyManagement.Clear();
                    wifiConfig.AllowedKeyManagement.Set((int)WifiConfiguration.KeyMgmt.None);
                }
                else
                {
                    wifiConfig.PreSharedKey = $"\"{password}\"";
                }

                // Add network configuration
                var networkId = _wifiManager.AddNetwork(wifiConfig);
                if (networkId == -1)
                {
                    Debug.WriteLine($"❌ Failed to add network configuration for {ssid}");
                    return (false, "Netzwerkkonfiguration konnte nicht hinzugefügt werden");
                }

                Debug.WriteLine($"✅ Network configuration added with ID: {networkId}");

                // Enable and connect to the network
                bool connectResult = _wifiManager.EnableNetwork(networkId, true);
                if (!connectResult)
                {
                    _wifiManager.RemoveNetwork(networkId);
                    Debug.WriteLine($"❌ Failed to enable network {ssid}");
                    return (false, "Verbindung zum Netzwerk fehlgeschlagen");
                }

                Debug.WriteLine($"🔄 Connection initiated to {ssid}, waiting for connection...");

                // Wait for connection to establish
                for (int i = 0; i < 30; i++) // Wait up to 30 seconds
                {
                    await Task.Delay(1000);
                    
                    var connectedSsid = GetCurrentNetworkSsid();
                    if (!string.IsNullOrEmpty(connectedSsid) && 
                        (connectedSsid.Equals(ssid, StringComparison.OrdinalIgnoreCase) ||
                         connectedSsid.Replace("\"", "").Equals(ssid, StringComparison.OrdinalIgnoreCase)))
                    {
                        Debug.WriteLine($"✅ Successfully connected to {ssid}");
                        
                        // Clean up the added network configuration (best-effort)
                        try { _wifiManager.RemoveNetwork(networkId); } catch { }
                        
                        return (true, $"Erfolgreich mit {ssid} verbunden");
                    }
                }

                // Connection timeout
                Debug.WriteLine($"⏰ Connection timeout for {ssid}");
                try { _wifiManager.RemoveNetwork(networkId); } catch { }
                return (false, "Verbindung Zeitüberschreitung - Gerät nicht erreichbar");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error connecting to WiFi: {ex.Message}");
                return (false, $"Verbindungsfehler: {ex.Message}");
            }
        }

        public async Task<bool> ReconnectToPreviousNetworkAsync(string? previousSsid)
        {
            try
            {
                if (string.IsNullOrEmpty(previousSsid))
                {
                    Debug.WriteLine("⚠️ No previous network to reconnect to");
                    return true; // Consider it successful if there was no previous network
                }

                Debug.WriteLine($"🔄 Attempting to reconnect to previous network: {previousSsid}");

                if (_wifiManager == null)
                {
                    return false;
                }

                // Try to find and reconnect to the previous network
                var configuredNetworks = _wifiManager.ConfiguredNetworks;
                if (configuredNetworks != null)
                {
                    foreach (var config in configuredNetworks)
                    {
                        var configSsid = config.Ssid?.Replace("\"", "").Trim();
                        if (!string.IsNullOrEmpty(configSsid) && 
                            configSsid.Equals(previousSsid, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"🔄 Found previous network config, attempting reconnection to {configSsid}");
                            
                            bool reconnectResult = _wifiManager.EnableNetwork(config.NetworkId, true);
                            if (reconnectResult)
                            {
                                // Wait for reconnection
                                for (int i = 0; i < 15; i++) // Wait up to 15 seconds
                                {
                                    await Task.Delay(1000);
                                    
                                    var currentSsid = GetCurrentNetworkSsid();
                                    if (!string.IsNullOrEmpty(currentSsid) && 
                                        currentSsid.Equals(previousSsid, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Debug.WriteLine($"✅ Successfully reconnected to {previousSsid}");
                                        return true;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }

                Debug.WriteLine($"⚠️ Could not reconnect to previous network: {previousSsid}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error reconnecting to previous network: {ex.Message}");
                return false;
            }
        }

        private string? GetCurrentSsidViaConnectivityManager()
        {
            try
            {
                if (_connectivityManager != null)
                {
                    var activeNetwork = _connectivityManager.ActiveNetwork;
                    if (activeNetwork != null)
                    {
                        var networkCapabilities = _connectivityManager.GetNetworkCapabilities(activeNetwork);
                        if (networkCapabilities?.HasTransport(TransportType.Wifi) == true)
                        {
                            // For newer Android versions, try to get WiFi SSID through different means
                            var wifiInfo = _wifiManager?.ConnectionInfo;
                            if (wifiInfo != null)
                            {
                                // Try multiple approaches to get the SSID
                                return GetSsidFromNetworkId(wifiInfo.NetworkId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting SSID via ConnectivityManager: {ex.Message}");
            }
            return null;
        }

        private string? GetSsidViaReflection(WifiInfo wifiInfo)
        {
            try
            {
                // Try to get SSID via reflection (might work on some Android versions)
                var type = wifiInfo.GetType();
                var ssidProperty = type.GetProperty("SSID");
                var ssidField = type.GetField("SSID");
                
                if (ssidProperty != null)
                {
                    var ssid = ssidProperty.GetValue(wifiInfo)?.ToString();
                    Debug.WriteLine($"📶 Found SSID via reflection (property): '{ssid}'");
                    return ssid;
                }
                
                if (ssidField != null)
                {
                    var ssid = ssidField.GetValue(wifiInfo)?.ToString();
                    Debug.WriteLine($"📶 Found SSID via reflection (field): '{ssid}'");
                    return ssid;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Reflection failed: {ex.Message}");
            }
            return null;
        }

        private string? GetSsidFromNetworkId(int networkId)
        {
            try
            {
                if (_wifiManager?.ConfiguredNetworks != null)
                {
                    foreach (var config in _wifiManager.ConfiguredNetworks)
                    {
                        if (config.NetworkId == networkId)
                        {
                            var ssid = config.Ssid?.Replace("\"", "").Trim();
                            Debug.WriteLine($"📶 Found SSID from configured networks: '{ssid}' for NetworkId: {networkId}");
                            return ssid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting SSID from NetworkId {networkId}: {ex.Message}");
            }

            return null;
        }

        private bool HasLocationPermission()
        {
            if (_context == null) return false;

            var fineLocationPermission = ActivityCompat.CheckSelfPermission(_context, global::Android.Manifest.Permission.AccessFineLocation);
            var coarseLocationPermission = ActivityCompat.CheckSelfPermission(_context, global::Android.Manifest.Permission.AccessCoarseLocation);

            return fineLocationPermission == Permission.Granted || coarseLocationPermission == Permission.Granted;
        }

        private async Task RequestLocationPermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                Debug.WriteLine($"📍 Location permission status: {status}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error requesting location permission: {ex.Message}");
            }
        }

        public async Task OpenWifiSettingsAsync()
        {
            try
            {
                Debug.WriteLine("🔧 Opening Android WiFi settings...");
                
                if (_context == null)
                {
                    Debug.WriteLine("❌ Context is null, cannot open WiFi settings");
                    return;
                }

                var intent = new Intent(Settings.ActionWifiSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                _context.StartActivity(intent);
                
                Debug.WriteLine("✅ WiFi settings opened successfully");
                await Task.Delay(100); // Small delay to ensure intent is processed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error opening WiFi settings: {ex.Message}");
            }
        }
    }

    public class WifiScanResult
    {
        public string Ssid { get; set; } = string.Empty;
        public string Bssid { get; set; } = string.Empty;
        public int SignalLevel { get; set; }
        public int Frequency { get; set; }
        public string Capabilities { get; set; } = string.Empty;

        public string SecurityType
        {
            get
            {
                if (string.IsNullOrEmpty(Capabilities)) return "Open";
                
                if (Capabilities.Contains("WPA3")) return "WPA3";
                if (Capabilities.Contains("WPA2")) return "WPA2";
                if (Capabilities.Contains("WPA")) return "WPA";
                if (Capabilities.Contains("WEP")) return "WEP";
                
                return "Open";
            }
        }

        public string SignalStrengthText
        {
            get
            {
                // Convert dBm to signal quality percentage
                var quality = Math.Max(0, Math.Min(100, 2 * (SignalLevel + 100)));
                return $"{quality}% ({SignalLevel} dBm)";
            }
        }
    }
}
