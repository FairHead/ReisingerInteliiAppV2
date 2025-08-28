using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Diagnostics;
using System.Text.Json;

namespace ReisingerIntelliApp_V4.ViewModels;

[QueryProperty(nameof(DeviceData), "deviceData")]
public partial class SaveLocalDevicePageViewModel : ObservableObject, IDisposable
{
    private readonly IDeviceService _deviceService;
    private readonly IAuthenticationService _authService;
    private readonly IntellidriveApiService _apiService;
    private Timer? _onlineStatusTimer;
    private readonly object _timerLock = new object();

    public SaveLocalDevicePageViewModel(IDeviceService deviceService, IAuthenticationService authService, IntellidriveApiService apiService)
    {
        _deviceService = deviceService;
        _authService = authService;
        _apiService = apiService;
    TestButtonText = "Verbindung testen";
    CanTestConnection = true;
    }

    [ObservableProperty]
    private string deviceData = string.Empty;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    private string firmwareVersion = string.Empty;

    [ObservableProperty]
    private string serialNumber = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool showStatusMessage;

    [ObservableProperty]
    private bool isReachable;

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private bool canSaveDevice;

    // Added to mirror SaveDevicePage patterns
    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private Color statusColor = Colors.Black;

    [ObservableProperty]
    private bool isTestingConnection = false;

    [ObservableProperty]
    private string testButtonText = string.Empty;

    [ObservableProperty]
    private bool canTestConnection = false;

    partial void OnDeviceDataChanged(string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var json = Uri.UnescapeDataString(value);
            var payload = JsonSerializer.Deserialize<Payload>(json);
            if (payload != null)
            {
                DeviceName = string.IsNullOrWhiteSpace(payload.name) ? "Local Device" : payload.name;
                IpAddress = payload.ip ?? string.Empty;
                FirmwareVersion = payload.firmware ?? string.Empty;
                SerialNumber = payload.serial ?? string.Empty;
                CanSaveDevice = !string.IsNullOrEmpty(IpAddress) && !string.IsNullOrWhiteSpace(DeviceName);
                UpdateCanTestConnection();
                // Start online status monitoring when IP is known
                if (!string.IsNullOrWhiteSpace(IpAddress))
                {
                    StartOnlineStatusMonitoring();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error parsing deviceData: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        // Mirror WiFi test: require credentials, authenticate against device at IpAddress
        if (IsTestingConnection) return;

        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            UpdateStatusMessage("❌ Keine IP-Adresse vorhanden", Colors.Red, true);
            return;
        }

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
            // Call the authenticated beep endpoint directly; some devices don't respond to /version quickly or without auth
            var authenticated = await _authService.TestUserAuthAsync(IpAddress, Username, Password);
            if (authenticated)
            {
                IsReachable = true;
                IsOnline = true;
                UpdateStatusMessage("✅ Gerät erfolgreich authentifiziert und erreichbar", Colors.Green, true);
            }
            else
            {
                IsReachable = false;
                IsOnline = false;
                UpdateStatusMessage("❌ Authentifizierung fehlgeschlagen - prüfen Sie Benutzername/Passwort", Colors.Red, true);
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"❌ Fehler beim Test: {ex.Message}", Colors.Red, true);
        }
        finally
        {
            IsTestingConnection = false;
            TestButtonText = "Verbindung testen";
            UpdateCanTestConnection();
        }
    }

    [RelayCommand]
    private async Task SaveDeviceAsync()
    {
        try
        {
            var model = new DeviceModel
            {
                DeviceId = Guid.NewGuid().ToString(),
                Name = DeviceName,
                IpAddress = IpAddress,
                FirmwareVersion = FirmwareVersion,
                SerialNumber = SerialNumber,
                Type = AppDeviceType.LocalDevice,
                ConnectionType = ConnectionType.Local,
                LastSeen = DateTime.Now,
                IsOnline = true,
                Ip = IpAddress,
                Username = Username,
                Password = Password
            };

            await _deviceService.SaveDeviceAsync(model);
            // Notify listeners that a new local device was added
            MessagingCenter.Send(this, "LocalDeviceAdded");
            await Shell.Current.GoToAsync("../..");
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"❌ Speichern fehlgeschlagen: {ex.Message}", Colors.Red, true);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private record Payload(string ip, string name, string serial, string firmware, string deviceId);

    private void UpdateStatusMessage(string message, Color color, bool show)
    {
        StatusMessage = message;
        StatusColor = color;
        ShowStatusMessage = show;
    }

    partial void OnUsernameChanged(string value) => UpdateCanTestConnection();
    partial void OnPasswordChanged(string value) => UpdateCanTestConnection();
    partial void OnDeviceNameChanged(string value) => CanSaveDevice = !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(IpAddress);

    private void UpdateCanTestConnection()
    {
        // Enable only when we have the required inputs and we're not already testing
        CanTestConnection = !IsTestingConnection
                             && !string.IsNullOrWhiteSpace(IpAddress)
                             && !string.IsNullOrWhiteSpace(Username)
                             && !string.IsNullOrWhiteSpace(Password);
    }

    #region Online status monitoring (every 3s via /intellidrive/version)

    public void StartOnlineStatusMonitoring()
    {
        lock (_timerLock)
        {
            _onlineStatusTimer?.Dispose();
            // Poll every 3 seconds as requested
            _onlineStatusTimer = new Timer(async _ => await CheckOnlineStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            Debug.WriteLine("🔄 Local device online status monitoring started (3s interval)");
        }
    }

    public void StopOnlineStatusMonitoring()
    {
        lock (_timerLock)
        {
            _onlineStatusTimer?.Dispose();
            _onlineStatusTimer = null;
            Debug.WriteLine("⏹️ Local device online status monitoring stopped");
        }
    }

    private async Task CheckOnlineStatusAsync()
    {
        var ip = IpAddress;
        if (string.IsNullOrWhiteSpace(ip)) return;

        try
        {
            var (success, _, _) = await _apiService.TestIntellidriveConnectionAsync(ip);
            if (IsOnline != success)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsOnline = success;
                    // Optional: surface a lightweight status text when testing manually is not in progress
                    if (!IsTestingConnection)
                    {
                        StatusMessage = success ? "✅ Gerät online" : "❌ Gerät offline";
                        StatusColor = success ? Colors.Green : Colors.Red;
                        ShowStatusMessage = true;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error checking local device online status: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (IsOnline)
                {
                    IsOnline = false;
                    if (!IsTestingConnection)
                    {
                        StatusMessage = "❌ Gerät offline";
                        StatusColor = Colors.Red;
                        ShowStatusMessage = true;
                    }
                }
            });
        }
    }

    public void Dispose()
    {
        StopOnlineStatusMonitoring();
    }

    #endregion
}
