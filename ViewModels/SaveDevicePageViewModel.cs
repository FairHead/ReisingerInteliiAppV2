using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4.ViewModels;

[QueryProperty(nameof(NetworkData), "networkData")]
public partial class SaveDevicePageViewModel : ObservableObject, IDisposable
{
    private readonly IDeviceService _deviceService;
    private readonly IAuthenticationService _authService;
    private readonly WiFiManagerService _wifiService;
    private bool _isAuthenticated = false;
    private Timer? _networkStatusTimer;
    private readonly object _timerLock = new object();

    [ObservableProperty]
    private NetworkDataModel selectedNetwork = new();

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private bool canTestConnection = true;

    [ObservableProperty]
    private bool canSaveDevice = false;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Color statusColor = Colors.Black;

    [ObservableProperty]
    private bool showStatusMessage = false;

    [ObservableProperty]
    private bool isTestingConnection = false;

    [ObservableProperty]
    private string testButtonText = "Verbindung testen";

    public SaveDevicePageViewModel(
        IDeviceService deviceService, 
        IAuthenticationService authService,
        WiFiManagerService wifiService)
    {
        _deviceService = deviceService;
        _authService = authService;
        _wifiService = wifiService;
        
        // Initialize current network asynchronously
        _ = InitializeCurrentNetworkAsync();
        
        // Start network status monitoring
        StartNetworkStatusMonitoring();
    }

    private async Task InitializeCurrentNetworkAsync()
    {
        // Simplified - no longer tracking previous network for reconnection
        try
        {
            var currentSsid = await _wifiService.GetCurrentNetworkSsidAsync();
            System.Diagnostics.Debug.WriteLine($"📶 Current network at startup: {currentSsid}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting current network: {ex.Message}");
        }
    }

    public string NetworkData
    {
        set
        {
            System.Diagnostics.Debug.WriteLine($"🔄 NetworkData setter called with: {value ?? "NULL"}");
            
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var decodedValue = Uri.UnescapeDataString(value);
                    System.Diagnostics.Debug.WriteLine($"🔄 Decoded NetworkData: {decodedValue}");
                    
                    var network = System.Text.Json.JsonSerializer.Deserialize<NetworkDataModel>(decodedValue);
                    if (network != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Network deserialized: SSID={network.Ssid}");
                        SelectedNetwork = network;
                        DeviceName = network.Ssid ?? string.Empty; // Default device name to SSID
                        UpdateCanTestConnection();
                        UpdateCanSaveDevice();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Network deserialization returned null");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error deserializing network data: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ NetworkData is null or empty");
            }
        }
    }

    partial void OnUsernameChanged(string value) => UpdateCanTestConnection();
    partial void OnPasswordChanged(string value) => UpdateCanTestConnection();
    partial void OnDeviceNameChanged(string value) => UpdateCanSaveDevice();

    /// <summary>
    /// Navigates back to the previous page (WifiScanPage).
    /// </summary>
    [RelayCommand]
    private async Task BackAsync()
    {
        // Navigate back to WifiScanPage specifically
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTestingConnection) return;

        // Check if username and password are provided before starting the test
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            UpdateStatusMessage("❌ Bitte geben Sie Benutzername und Passwort ein", Colors.Red, true);
            return;
        }

        IsTestingConnection = true;
        TestButtonText = "Teste Verbindung...";
        UpdateStatusMessage("Verbindung wird getestet...", Colors.Orange, true);
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Starting WiFi connection test to: {SelectedNetwork?.Ssid}");
            
            if (SelectedNetwork == null || string.IsNullOrEmpty(SelectedNetwork.Ssid))
            {
                UpdateStatusMessage("❌ Kein Netzwerk ausgewählt", Colors.Red, true);
                return;
            }

            // Check if we're already connected to the target network
            var currentSsid = await _wifiService.GetCurrentNetworkSsidAsync();
            System.Diagnostics.Debug.WriteLine($"📶 Current network: '{currentSsid}', Target: '{SelectedNetwork.Ssid}'");
            
            if (!string.IsNullOrEmpty(currentSsid) && 
                string.Equals(currentSsid.Trim(), SelectedNetwork.Ssid?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Already connected to target network: {SelectedNetwork.Ssid}");
                UpdateStatusMessage("📡 Bereits mit Zielnetzwerk verbunden, teste Gerätezugang...", Colors.Green, true);
                
                // Test device authentication since we're already connected
                await TestDeviceAuthenticationAsync();
                return;
            }

            // Not connected to target network - need to connect
            var needsPassword = SelectedNetwork.SecurityType?.ToString() != "Open";
            if (needsPassword && string.IsNullOrEmpty(Password))
            {
                UpdateStatusMessage("❌ Passwort erforderlich für dieses Netzwerk", Colors.Red, true);
                return;
            }

            // Offer connection options
            var connectionChoice = await Shell.Current.DisplayAlert(
                "WiFi-Verbindung erforderlich",
                $"Sie sind nicht mit dem Netzwerk '{SelectedNetwork.Ssid}' verbunden.\n\nWie möchten Sie sich verbinden?",
                "WiFi-Einstellungen öffnen",
                "Automatisch versuchen");

            if (connectionChoice)
            {
                // User chose to open WiFi settings manually
                UpdateStatusMessage("⚙️ Öffne WiFi-Einstellungen...", Colors.Blue, true);
                await _wifiService.OpenWifiSettingsAsync();
                
                // Start background monitoring for connection
                UpdateStatusMessage($"📱 WiFi-Einstellungen geöffnet. Verbinden Sie sich mit '{SelectedNetwork.Ssid}' und die App überwacht automatisch die Verbindung.", Colors.Blue, true);
                StartBackgroundConnectionMonitoring(SelectedNetwork.Ssid ?? string.Empty);
            }
            else
            {
                // Try automatic connection
                await TryAutomaticConnectionAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error during connection test: {ex.Message}");
            UpdateStatusMessage($"❌ Fehler beim Test: {ex.Message}", Colors.Red, true);
        }
        finally
        {
            IsTestingConnection = false;
            TestButtonText = "Verbindung testen";
        }
    }

    private async Task TestDeviceAuthenticationAsync()
    {
        try
        {
            // For the connection test, username and password are now required
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                UpdateStatusMessage("❌ Benutzername und Passwort sind für den Test erforderlich", Colors.Red, true);
                return;
            }

            // Determine IP address based on device type
            // WiFi devices (from WiFi scan) always use the same IP: 192.168.4.100
            // Local devices have dynamic IP from network discovery
            string testIp;
            bool isWifiDevice = !string.IsNullOrEmpty(SelectedNetwork?.Ssid);
            
            if (isWifiDevice)
            {
                // WiFi devices always use the same IP
                testIp = "192.168.4.100";
                System.Diagnostics.Debug.WriteLine("🌐 Testing WiFi device at fixed IP: 192.168.4.100");
            }
            else
            {
                // Local devices have dynamic IP - use the IP from network data or fallback
                testIp = !string.IsNullOrEmpty(SelectedNetwork?.IpAddressString) 
                    ? SelectedNetwork.IpAddressString 
                    : "192.168.1.100"; // Fallback for local devices
                System.Diagnostics.Debug.WriteLine($"🏠 Testing local device at dynamic IP: {testIp}");
            }

            UpdateStatusMessage($"🔐 Teste Authentifizierung gegen {testIp}...", Colors.Orange, true);

            // Test device authentication with the intellidrive/serialnumber endpoint
            var isAuthenticated = await _authService.TestUserAuthAsync(testIp, Username, Password);
            
            if (isAuthenticated)
            {
                // Success - device responded with proper JSON
                UpdateStatusMessage("✅ Gerät erfolgreich authentifiziert und erreichbar", Colors.Green, true);
                _isAuthenticated = true;
                System.Diagnostics.Debug.WriteLine("🎉 Device authentication successful - JSON response received");
            }
            else
            {
                // Failed - either wrong credentials or device not reachable
                UpdateStatusMessage("❌ Authentifizierung fehlgeschlagen - prüfen Sie Benutzername/Passwort", Colors.Red, true);
                _isAuthenticated = false;
                System.Diagnostics.Debug.WriteLine("🔒 Device authentication failed");
            }

            // Wait a moment to show the result
            await Task.Delay(2000);

            // If test was successful, ask user if they want to switch back to another network
            if (isAuthenticated)
            {
                var switchNetwork = await Shell.Current.DisplayAlert(
                    "Test erfolgreich",
                    $"✅ Verbindung und Gerätezugang erfolgreich getestet!\n\nMöchten Sie zu einem anderen WiFi-Netzwerk wechseln oder mit '{SelectedNetwork?.Ssid ?? "diesem Netzwerk"}' verbunden bleiben?",
                    "Anderes Netzwerk wählen",
                    "Verbunden bleiben");

                if (switchNetwork)
                {
                    UpdateStatusMessage("⚙️ Öffne WiFi-Einstellungen zum Netzwerkwechsel...", Colors.Blue, true);
                    await _wifiService.OpenWifiSettingsAsync();
                    UpdateStatusMessage("📱 WiFi-Einstellungen geöffnet. Sie können jetzt zu einem anderen Netzwerk wechseln.", Colors.Blue, true);
                }
                else
                {
                    UpdateStatusMessage("✅ Test erfolgreich - mit Zielnetzwerk verbunden", Colors.Green, true);
                }
            }

            UpdateCanSaveDevice();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error during device authentication test: {ex.Message}");
            UpdateStatusMessage($"❌ Fehler beim Gerätetest: {ex.Message}", Colors.Red, true);
        }
    }

    [RelayCommand]
    private async Task SaveDeviceAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 SaveDeviceAsync started");
            System.Diagnostics.Debug.WriteLine($"📊 SelectedNetwork: {SelectedNetwork?.Ssid ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"📊 DeviceName: '{DeviceName}'");
            System.Diagnostics.Debug.WriteLine($"📊 Username: '{Username}'");
            System.Diagnostics.Debug.WriteLine($"📊 Password: '{Password}'");
            
            UpdateStatusMessage("Gerät wird gespeichert...", Colors.Orange, true);

            // Check if credentials are missing and ask for confirmation
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                bool continueWithoutCredentials = await Shell.Current.DisplayAlert(
                    "Credentials fehlen",
                    "Sie haben keinen Benutzernamen oder kein Passwort eingegeben. Möchten Sie das Gerät trotzdem ohne Anmeldedaten speichern?",
                    "Ja, ohne Credentials speichern",
                    "Nein, Credentials eingeben");

                if (!continueWithoutCredentials)
                {
                    UpdateStatusMessage("ℹ️ Speichern abgebrochen - bitte geben Sie Credentials ein", Colors.Blue, true);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("✅ User chose to save device without credentials");
            }

            if (SelectedNetwork == null)
            {
                UpdateStatusMessage("❌ Kein Netzwerk ausgewählt", Colors.Red, true);
                System.Diagnostics.Debug.WriteLine("❌ SelectedNetwork is null");
                return;
            }

            var device = DeviceModel.FromNetworkData(SelectedNetwork);
            
            // Set device properties
            device.DeviceId = Guid.NewGuid().ToString();
            device.Name = !string.IsNullOrEmpty(DeviceName) ? DeviceName : (SelectedNetwork.Ssid ?? "Unknown Device");
            device.Username = Username;
            device.Password = Password;
            device.ConnectionType = ConnectionType.Wifi;
            device.Type = AppDeviceType.WifiDevice;
            device.IsOnline = true;
            device.LastSeen = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"💾 Device created: ID={device.DeviceId}, Name={device.Name}");

            // Check if device already exists by SSID
            var exists = await _deviceService.DeviceExistsBySsidAsync(SelectedNetwork.Ssid ?? string.Empty);
            System.Diagnostics.Debug.WriteLine($"🔍 Device exists check: {exists}");
            
            if (exists)
            {
                UpdateStatusMessage("❌ Gerät mit dieser SSID bereits gespeichert", Colors.Red, true);
                return;
            }

            // Save device
            System.Diagnostics.Debug.WriteLine("💾 Calling SaveDeviceAsync...");
            await _deviceService.SaveDeviceAsync(device);
            System.Diagnostics.Debug.WriteLine("✅ SaveDeviceAsync completed");
            
            UpdateStatusMessage("✅ Gerät erfolgreich gespeichert", Colors.Green, true);
            
            // Navigate back to WifiScanPage after successful save
            await Task.Delay(1500);
            await Shell.Current.GoToAsync("../..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SaveDeviceAsync error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            UpdateStatusMessage($"❌ Speichern fehlgeschlagen: {ex.Message}", Colors.Red, true);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void UpdateCanTestConnection()
    {
        // Allow the button to be clickable always (except during testing)
        // Validation will happen inside TestConnectionAsync method
        CanTestConnection = !IsTestingConnection;
    }

    private void UpdateCanSaveDevice()
    {
        // Allow saving even without authentication test
        CanSaveDevice = !string.IsNullOrEmpty(DeviceName) && SelectedNetwork != null;
        System.Diagnostics.Debug.WriteLine($"🔄 UpdateCanSaveDevice: CanSave={CanSaveDevice}, DeviceName='{DeviceName}', SelectedNetwork={SelectedNetwork?.Ssid ?? "NULL"}");
    }

    private void UpdateStatusMessage(string message, Color color, bool show)
    {
        StatusMessage = message;
        StatusColor = color;
        ShowStatusMessage = show;
    }

    private async Task TryAutomaticConnectionAsync()
    {
        try
        {
            // Check if password is needed for automatic connection
            if (SelectedNetwork.SecurityType?.ToString() != "Open" && string.IsNullOrEmpty(Password))
            {
                UpdateStatusMessage("❌ Passwort erforderlich für automatische Verbindung", Colors.Red, true);
                return;
            }

            UpdateStatusMessage("📡 Versuche automatische Verbindung...", Colors.Orange, true);

            // Attempt to connect to the WiFi network
            var (connectSuccess, connectMessage) = await _wifiService.ConnectToWifiNetworkAsync(
                SelectedNetwork.Ssid ?? string.Empty, 
                Password ?? string.Empty);

            if (!connectSuccess)
            {
                UpdateStatusMessage($"❌ Automatische Verbindung fehlgeschlagen: {connectMessage}", Colors.Red, true);
                
                // Offer WiFi settings as fallback
                var openSettings = await Shell.Current.DisplayAlert(
                    "Automatische Verbindung fehlgeschlagen",
                    $"Die automatische Verbindung zu '{SelectedNetwork.Ssid}' ist fehlgeschlagen.\n\nMöchten Sie die WiFi-Einstellungen öffnen?",
                    "Einstellungen öffnen",
                    "Abbrechen");

                if (openSettings)
                {
                    await _wifiService.OpenWifiSettingsAsync();
                    UpdateStatusMessage($"📱 WiFi-Einstellungen geöffnet für '{SelectedNetwork.Ssid}'", Colors.Blue, true);
                    StartBackgroundConnectionMonitoring(SelectedNetwork.Ssid ?? string.Empty);
                }
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Successfully connected to WiFi: {SelectedNetwork.Ssid}");
            UpdateStatusMessage("📡 WiFi automatisch verbunden, teste Gerätezugang...", Colors.Green, true);

            // Give WiFi some time to fully establish
            await Task.Delay(3000);

            // Test device authentication
            await TestDeviceAuthenticationAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in automatic connection: {ex.Message}");
            UpdateStatusMessage($"❌ Automatische Verbindung fehlgeschlagen: {ex.Message}", Colors.Red, true);
        }
    }

    private void StartBackgroundConnectionMonitoring(string targetSsid)
    {
        System.Diagnostics.Debug.WriteLine($"🔄 Starting background monitoring for: {targetSsid}");
        
        Task.Run(async () =>
        {
            // Monitor for connection up to 5 minutes
            for (int i = 0; i < 60; i++) // Check every 5 seconds for 5 minutes
            {
                try
                {
                    await Task.Delay(5000);
                    
                    var currentSsid = await _wifiService.GetCurrentNetworkSsidAsync();
                    System.Diagnostics.Debug.WriteLine($"🔍 Background check #{i + 1}: Current SSID = '{currentSsid}'");
                    
                    if (!string.IsNullOrEmpty(currentSsid) && 
                        string.Equals(currentSsid.Trim(), targetSsid.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Connected to target network: {targetSsid}");
                        
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            UpdateStatusMessage($"✅ Verbunden mit '{targetSsid}', teste Gerätezugang...", Colors.Green, true);
                            await TestDeviceAuthenticationAsync();
                        });
                        
                        return; // Success - stop monitoring
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Background monitoring error: {ex.Message}");
                }
            }
            
            // If still not connected after 5 minutes
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateStatusMessage($"⏰ Zeitüberschreitung: Verbindung zu '{targetSsid}' nicht erkannt", Colors.Orange, true);
            });
        });
    }

    #region Network Status Monitoring

    private void StartNetworkStatusMonitoring()
    {
        lock (_timerLock)
        {
            // Stop existing timer if any
            _networkStatusTimer?.Dispose();
            
            // Start a timer that checks network status every 5 seconds
            _networkStatusTimer = new Timer(async _ => await CheckNetworkStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            System.Diagnostics.Debug.WriteLine("🔄 Network status monitoring started");
        }
    }

    private void StopNetworkStatusMonitoring()
    {
        lock (_timerLock)
        {
            _networkStatusTimer?.Dispose();
            _networkStatusTimer = null;
            System.Diagnostics.Debug.WriteLine("⏹️ Network status monitoring stopped");
        }
    }

    private async Task CheckNetworkStatusAsync()
    {
        if (SelectedNetwork == null || string.IsNullOrEmpty(SelectedNetwork.Ssid))
            return;

        try
        {
            // Get current connected network
            var currentSsid = await _wifiService.GetCurrentNetworkSsidAsync();
            var isCurrentlyConnected = !string.IsNullOrEmpty(currentSsid) && 
                                     string.Equals(currentSsid.Trim(), SelectedNetwork.Ssid.Trim(), StringComparison.OrdinalIgnoreCase);

            // If connection status has changed, update the UI
            if (SelectedNetwork.IsConnected != isCurrentlyConnected)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectedNetwork.IsConnected = isCurrentlyConnected;
                    OnPropertyChanged(nameof(SelectedNetwork));
                    
                    System.Diagnostics.Debug.WriteLine($"📶 Network status updated: {SelectedNetwork.Ssid} is now {(isCurrentlyConnected ? "Connected" : "Disconnected")}");
                });

                // If we're now connected to the target network, also check device reachability
                if (isCurrentlyConnected)
                {
                    await CheckDeviceReachabilityAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking network status: {ex.Message}");
        }
    }

    private async Task CheckDeviceReachabilityAsync()
    {
        if (SelectedNetwork == null || string.IsNullOrEmpty(SelectedNetwork.Ssid))
            return;

        try
        {
            // Try to reach the device on common WiFi AP IP addresses
            var commonIps = new[] { "192.168.4.1", "192.168.1.1", "10.0.0.1", "172.16.0.1" };
            
            foreach (var ip in commonIps)
            {
                var isReachable = await _authService.IsNetworkReachableAsync(ip);
                if (isReachable)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Device reachable at {ip} on network {SelectedNetwork.Ssid}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking device reachability: {ex.Message}");
        }
    }

    // Dispose method to clean up timer
    public void Dispose()
    {
        StopNetworkStatusMonitoring();
    }

    #endregion
}
