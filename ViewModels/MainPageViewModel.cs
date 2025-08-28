using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Text.RegularExpressions;

namespace ReisingerIntelliApp_V4.ViewModels;

public class MainPageViewModel : BaseViewModel, IDisposable
{
    private readonly IDeviceService _deviceService;
    private readonly IAuthenticationService _authService;
    private readonly WiFiManagerService _wifiService;
    private readonly IntellidriveApiService _apiService;
    private string? _currentActiveTab;
    private DateTime _lastTabTapTime = DateTime.MinValue;
    private const int TAB_DEBOUNCE_MS = 300;
    private const string NO_DEVICES_ITEM_ID = "no_devices";
    private Timer? _wifiStatusTimer;
    private readonly object _timerLock = new object();
    
    private ObservableCollection<DropdownItemModel> _dropdownItems = new();
    private string _dropdownTitle = string.Empty;
    private bool _isDropdownVisible;
    private bool _showScanButton;
    private string _scanButtonText = string.Empty;

    public MainPageViewModel(IDeviceService deviceService, IAuthenticationService authService, WiFiManagerService wifiService, IntellidriveApiService apiService)
    {
        _deviceService = deviceService;
        _authService = authService;
        _wifiService = wifiService;
        _apiService = apiService;
        Title = "Reisinger App";
        TabTappedCommand = new Command<string>(OnTabTapped);
        LeftSectionTappedCommand = new Command(OnLeftSectionTapped);
        CenterButtonTappedCommand = new Command(OnCenterButtonTapped);
        RightSectionTappedCommand = new Command(OnRightSectionTapped);
        ScanButtonTappedCommand = new Command(OnScanButtonTapped);
        DeleteDeviceFromDropdownCommand = new Command<DropdownItemModel>(OnDeleteDeviceFromDropdown);
        ShowDeviceOptionsCommand = new Command<DropdownItemModel>(OnShowDeviceOptions);
        
        InitializeDropdownData();
        
        // Subscribe to device added messages
        MessagingCenter.Subscribe<LocalDevicesScanPageViewModel>(this, "LocalDeviceAdded", async (sender) =>
        {
            if (CurrentActiveTab == "LocalDev")
            {
                await LoadLocalDevicesAsync();
            }
        });
    }

    public ICommand TabTappedCommand { get; }
    public ICommand LeftSectionTappedCommand { get; }
    public ICommand CenterButtonTappedCommand { get; }
    public ICommand RightSectionTappedCommand { get; }
    public ICommand ScanButtonTappedCommand { get; }
    public ICommand DeleteDeviceFromDropdownCommand { get; }
    public ICommand ShowDeviceOptionsCommand { get; }

    public string? CurrentActiveTab
    {
        get => _currentActiveTab;
        set => SetProperty(ref _currentActiveTab, value);
    }

    public ObservableCollection<DropdownItemModel> DropdownItems
    {
        get => _dropdownItems;
        set => SetProperty(ref _dropdownItems, value);
    }

    public string DropdownTitle
    {
        get => _dropdownTitle;
        set => SetProperty(ref _dropdownTitle, value);
    }

    public bool IsDropdownVisible
    {
        get => _isDropdownVisible;
        set => SetProperty(ref _isDropdownVisible, value);
    }

    public bool ShowScanButton
    {
        get => _showScanButton;
        set => SetProperty(ref _showScanButton, value);
    }

    public string ScanButtonText
    {
        get => _scanButtonText;
        set => SetProperty(ref _scanButtonText, value);
    }

    // Event to notify the View about tab changes
    public event EventHandler<string>? TabActivated;
    public event EventHandler? TabDeactivated;

    // Cache for dropdown data
    private readonly Dictionary<string, (string Title, List<DropdownItemModel> Items)> _dropdownCache = new();

    private void InitializeDropdownData()
    {
        // Pre-cache all dropdown data for instant access
        _dropdownCache["Structures"] = ("Structures", new List<DropdownItemModel>
        {
            new() { Id = "struct_001", Icon = "home.svg", Text = "Main HQ", HasActions = false },
            new() { Id = "struct_002", Icon = "home.svg", Text = "Storage\nFacility", HasActions = false },
            new() { Id = "struct_003", Icon = "home.svg", Text = "Manufacturing\nPlant", HasActions = false },
            new() { Id = "struct_004", Icon = "home.svg", Text = "Data Center", HasActions = false },
            new() { Id = "struct_005", Icon = "home.svg", Text = "Data Center", HasActions = false },
            new() { Id = "struct_006", Icon = "home.svg", Text = "Data Center", HasActions = false }
        });

        _dropdownCache["Levels"] = ("Levels", new List<DropdownItemModel>
        {
            new() { Id = "level_001", Icon = "levels.svg", Text = "First Floor\nRight Section", HasActions = false },
            new() { Id = "level_002", Icon = "levels.svg", Text = "First Floor\nLeft Section", HasActions = false },
            new() { Id = "level_003", Icon = "levels.svg", Text = "Second Floor\nRight Section", HasActions = false },
            new() { Id = "level_004", Icon = "levels.svg", Text = "Second Floor\nLeft Section", HasActions = false },
            new() { Id = "level_005", Icon = "levels.svg", Text = "Third Floor\nRight Section", HasActions = false },
            new() { Id = "level_006", Icon = "levels.svg", Text = "Third Floor\nLeft Section", HasActions = false }
        });

        _dropdownCache["WifiDev"] = ("Wifi Devices", new List<DropdownItemModel>());

    _dropdownCache["LocalDev"] = ("Local Devices", new List<DropdownItemModel>());
    }

    private void OnTabTapped(string tabName)
    {
        // Debounce rapid taps
        var now = DateTime.Now;
        if ((now - _lastTabTapTime).TotalMilliseconds < TAB_DEBOUNCE_MS)
        {
            System.Diagnostics.Debug.WriteLine("Tab tap ignored (debounce)");
            return;
        }
        _lastTabTapTime = now;

        if (string.IsNullOrEmpty(tabName)) return;

        System.Diagnostics.Debug.WriteLine($"Tab tapped: {tabName}, Current active: {CurrentActiveTab}");

        if (string.Equals(CurrentActiveTab, tabName, StringComparison.Ordinal))
        {
            // If the same tab is tapped again, deactivate it
            CloseDropdown();
        }
        else
        {
            // Activate the new tab
            ShowDropdownForTab(tabName);
        }
    }

    private void ShowDropdownForTab(string tabName)
    {
        CurrentActiveTab = tabName;
        
        if (tabName == "WifiDev")
        {
            _ = LoadWifiDevicesAsync();
            // Ensure WiFi status monitoring is active for WifiDev tab
            if (DropdownItems.Any(item => item.HasActions))
            {
                StartWifiStatusMonitoring();
            }
        }
        else if (tabName == "LocalDev")
        {
            _ = LoadLocalDevicesAsync();
            // Ensure Local device status monitoring is active for LocalDev tab
            if (DropdownItems.Any(item => item.HasActions))
            {
                StartLocalDevStatusMonitoring();
            }
        }
        else if (_dropdownCache.TryGetValue(tabName, out var cachedData))
        {
            DropdownTitle = cachedData.Title;
            ShowScanButton = tabName == "LocalDev";
            
            // Set scan button text based on tab
            ScanButtonText = "Scan Local Network for Devices";
            
            // Clear and add items directly
            DropdownItems.Clear();
            foreach (var item in cachedData.Items)
            {
                DropdownItems.Add(item);
            }
            
            IsDropdownVisible = true;
            
            // Start status monitoring for LocalDev as well
            if (tabName == "LocalDev" && DropdownItems.Any(item => item.HasActions))
            {
                StartLocalDevStatusMonitoring();
            }
            else
            {
                // Stop monitoring when switching away from device tabs
                StopWifiStatusMonitoring();
            }
        }

        TabActivated?.Invoke(this, tabName);
    }

    private async Task LoadWifiDevicesAsync()
    {
        try
        {
            DropdownTitle = "WiFi Devices";
            ShowScanButton = true;
            ScanButtonText = "Scan for Devices in WiFi-Ap Mode";
            
            // Show loading state
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = "loading", 
                Icon = "loading.svg", 
                Text = "Loading saved devices...", 
                HasActions = false 
            });
            IsDropdownVisible = true;

            // Load saved WiFi devices
            var savedDevices = await _deviceService.GetSavedWifiDevicesAsync();
            
            // Clear loading and add real devices
            DropdownItems.Clear();
            
            if (savedDevices.Any())
            {
                // Ensure any previous default card is removed
                RemoveDefaultNoDeviceCard();

                foreach (var device in savedDevices)
                {
                    var lastSeenText = device.LastSeen != default(DateTime) 
                        ? $"Last seen: {device.LastSeen:HH:mm}"
                        : "Never connected";
                    
                    var dropdownItem = new DropdownItemModel
                    {
                        Id = device.DeviceId,
                        Icon = "wifi_icon.svg",
                        Text = $"{device.Name}\n{device.Ssid} • {lastSeenText}",
                        HasActions = true,
                        IsConnected = false // Start with disconnected, will be updated immediately by monitoring
                    };
                    
                    DropdownItems.Add(dropdownItem);
                }
                
                // Start monitoring WiFi device status IMMEDIATELY
                StartWifiStatusMonitoring();
                
                // Trigger an immediate status check to get current connectivity state
                _ = Task.Run(async () => 
                {
                    await Task.Delay(100); // Small delay to ensure UI is ready
                    await CheckWifiDeviceStatusAsync();
                });
            }
            else
            {
                // Only show the default card when there are no saved devices
                DropdownItems.Add(new DropdownItemModel
                {
                    Id = NO_DEVICES_ITEM_ID,
                    Icon = "info.svg",
                    Text = "No saved WiFi devices\nUse 'Scan' to add devices",
                    HasActions = false
                });
            }
        }
        catch (Exception ex)
        {
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "error",
                Icon = "error.svg",
                Text = $"Error loading devices:\n{ex.Message}",
                HasActions = false
            });
        }
    }

    private async Task LoadLocalDevicesAsync()
    {
        try
        {
            DropdownTitle = "Local Devices";
            ShowScanButton = true;
            ScanButtonText = "Scan Local Network for Devices";
            
            // Show loading state
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = "loading", 
                Icon = "loading.svg", 
                Text = "Loading saved local devices...", 
                HasActions = false 
            });
            IsDropdownVisible = true;

            // Load saved local devices
            var savedDevices = await _deviceService.GetSavedLocalDevicesAsync();
            
            // Clear loading and add real devices
            DropdownItems.Clear();
            
            if (savedDevices.Any())
            {
                // Ensure any previous default card is removed
                RemoveDefaultNoDeviceCard();

                foreach (var device in savedDevices)
                {
                    var lastSeenText = device.LastSeen != default(DateTime) 
                        ? $"Last seen: {device.LastSeen:HH:mm}"
                        : "Never connected";
                    
                    var dropdownItem = new DropdownItemModel
                    {
                        Id = device.DeviceId,
                        Icon = "local_icon.svg",
                        Text = $"{device.Name}\n{device.IpAddress} • {lastSeenText}",
                        HasActions = true,
                        IsConnected = false // Start with disconnected, will be updated by monitoring
                    };
                    
                    DropdownItems.Add(dropdownItem);
                }
                
                // Start monitoring local device status IMMEDIATELY
                StartLocalDevStatusMonitoring();
                
                // Trigger an immediate status check to get current connectivity state
                _ = Task.Run(async () => 
                {
                    await Task.Delay(100); // Small delay to ensure UI is ready
                    await CheckLocalDeviceStatusAsync();
                });
            }
            else
            {
                // Only show the default card when there are no saved devices
                DropdownItems.Add(new DropdownItemModel
                {
                    Id = NO_DEVICES_ITEM_ID,
                    Icon = "info.svg",
                    Text = "No saved local devices\nUse 'Scan' to add devices",
                    HasActions = false
                });
            }
        }
        catch (Exception ex)
        {
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "error",
                Icon = "error.svg",
                Text = $"Error loading local devices:\n{ex.Message}",
                HasActions = false
            });
        }
    }

    private void RemoveDefaultNoDeviceCard()
    {
        var noDeviceItem = DropdownItems.FirstOrDefault(i => i.Id == NO_DEVICES_ITEM_ID);
        if (noDeviceItem != null)
        {
            DropdownItems.Remove(noDeviceItem);
        }
    }

    public void CloseDropdown()
    {
        IsDropdownVisible = false;
        CurrentActiveTab = null;
        ShowScanButton = false;
        DropdownItems.Clear();
        
        // Stop WiFi monitoring when dropdown is closed
        StopWifiStatusMonitoring();
        
        TabDeactivated?.Invoke(this, EventArgs.Empty);
    }

    private async void OnLeftSectionTapped()
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            await page.DisplayAlert("Navigation", "My Place tapped", "OK");
    }

    private async void OnCenterButtonTapped()
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            await page.DisplayAlert("Action", "Add button tapped", "OK");
    }

    private async void OnRightSectionTapped()
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            await page.DisplayAlert("Settings", "Preferences tapped", "OK");
    }

    private async void OnScanButtonTapped()
    {
        if (CurrentActiveTab == "WifiDev")
        {
            await Shell.Current.GoToAsync("wifiscan");
        }
        else if (CurrentActiveTab == "LocalDev")
        {
            await Shell.Current.GoToAsync("localscan");
        }
        
        CloseDropdown();
    }

    private async void OnDeleteDeviceFromDropdown(DropdownItemModel device)
    {
        if (device == null) return;

        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Device",
                $"Are you sure you want to delete the device '{device.Text.Split('\n')[0]}'?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                // Remove from DeviceService if it's a WiFi device
                if (CurrentActiveTab == "WifiDev")
                {
                    // Create a DeviceModel to delete from service
                    var deviceToDelete = new DeviceModel
                    {
                        DeviceId = device.Id,
                        // Extract device name from the text (first line)
                        Name = device.Text.Split('\n')[0],
                        ConnectionType = ConnectionType.Wifi
                    };
                    
                    await _deviceService.DeleteDeviceAsync(deviceToDelete);
                }
                else if (CurrentActiveTab == "LocalDev")
                {
                    // Persist deletion for Local devices as well
                    var deviceToDelete = new DeviceModel
                    {
                        DeviceId = device.Id,
                        Name = device.Text.Split('\n')[0],
                        ConnectionType = ConnectionType.Local
                    };

                    await _deviceService.DeleteDeviceAsync(deviceToDelete);
                }

                // Remove from the current dropdown items
                DropdownItems.Remove(device);

                // Also remove from the cache
                if (CurrentActiveTab == "WifiDev" && _dropdownCache.ContainsKey("WifiDev"))
                {
                    var (title, items) = _dropdownCache["WifiDev"];
                    var itemToRemove = items.FirstOrDefault(i => i.Id == device.Id);
                    if (itemToRemove != null)
                    {
                        items.Remove(itemToRemove);
                        _dropdownCache["WifiDev"] = (title, items);
                    }
                }
                else if (CurrentActiveTab == "LocalDev" && _dropdownCache.ContainsKey("LocalDev"))
                {
                    var (title, items) = _dropdownCache["LocalDev"];
                    var itemToRemove = items.FirstOrDefault(i => i.Id == device.Id);
                    if (itemToRemove != null)
                    {
                        items.Remove(itemToRemove);
                        _dropdownCache["LocalDev"] = (title, items);
                    }
                }

                // If no actionable items remain, show the default "no devices" card
                if (CurrentActiveTab == "LocalDev" && !DropdownItems.Any(i => i.HasActions))
                {
                    DropdownItems.Add(new DropdownItemModel
                    {
                        Id = NO_DEVICES_ITEM_ID,
                        Icon = "info.svg",
                        Text = "No saved local devices\nUse 'Scan' to add devices",
                        HasActions = false
                    });
                }
                else if (CurrentActiveTab == "WifiDev" && !DropdownItems.Any(i => i.HasActions))
                {
                    DropdownItems.Add(new DropdownItemModel
                    {
                        Id = NO_DEVICES_ITEM_ID,
                        Icon = "info.svg",
                        Text = "No saved WiFi devices\nUse 'Scan' to add devices",
                        HasActions = false
                    });
                }

                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    "Device has been deleted successfully.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to delete device: {ex.Message}",
                "OK");
        }
    }

    private async void OnShowDeviceOptions(DropdownItemModel device)
    {
        if (device == null) return;

        try
        {
            var deviceName = device.Text.Split('\n')[0];
            var options = new List<string>
            {
                "Edit Device",
                "View Details",
                "Connection Settings",
                "Delete Device",
                "Cancel"
            };

            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"Options for '{deviceName}'",
                "Cancel",
                null,
                options.ToArray());

            switch (action)
            {
                case "Edit Device":
                    await Application.Current.MainPage.DisplayAlert(
                        "Edit Device",
                        "Edit functionality will be implemented later.",
                        "OK");
                    break;

                case "View Details":
                    var details = $"Device ID: {device.Id}\n" +
                                 $"Name: {deviceName}\n" +
                                 $"Info: {device.Text}\n" +
                                 $"Type: {(CurrentActiveTab == "WifiDev" ? "WiFi Device" : "Local Device")}";
                    
                    await Application.Current.MainPage.DisplayAlert(
                        "Device Details",
                        details,
                        "OK");
                    break;

                case "Connection Settings":
                    await Application.Current.MainPage.DisplayAlert(
                        "Connection Settings",
                        "Connection settings will be implemented later.",
                        "OK");
                    break;

                case "Delete Device":
                    OnDeleteDeviceFromDropdown(device);
                    break;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to show options: {ex.Message}",
                "OK");
        }
    }

    #region WiFi Status Monitoring

    private void StartWifiStatusMonitoring()
    {
        lock (_timerLock)
        {
            // Stop existing timer if any
            _wifiStatusTimer?.Dispose();
            
            // Use the SAME interval as SaveDevicePage (5 seconds) for consistency
            _wifiStatusTimer = new Timer(async _ => await CheckWifiDeviceStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            System.Diagnostics.Debug.WriteLine("🔄 WiFi status monitoring started for MainPage dropdown (5s interval - same as SaveDevicePage)");
        }
    }

    private void StartLocalDevStatusMonitoring()
    {
        lock (_timerLock)
        {
            // Stop existing timer if any
            _wifiStatusTimer?.Dispose();
            
            // Poll every 3 seconds via /intellidrive/version as requested
            _wifiStatusTimer = new Timer(async _ => await CheckLocalDeviceStatusAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            
            System.Diagnostics.Debug.WriteLine("🔄 Local device status monitoring started for MainPage dropdown (3s interval)");
        }
    }

    private void StopWifiStatusMonitoring()
    {
        lock (_timerLock)
        {
            _wifiStatusTimer?.Dispose();
            _wifiStatusTimer = null;
            System.Diagnostics.Debug.WriteLine("⏹️ WiFi status monitoring stopped for MainPage dropdown");
        }
    }

    private async Task CheckWifiDeviceStatusAsync()
    {
        if (CurrentActiveTab != "WifiDev" || !DropdownItems.Any())
        {
            System.Diagnostics.Debug.WriteLine($"❌ Skipping WiFi status check - CurrentActiveTab: {CurrentActiveTab}, DropdownItems count: {DropdownItems.Count}");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 === Starting WiFi Device Status Check ===");
            
            // Get all saved devices to get their connection details
            var savedDevices = await _deviceService.GetSavedWifiDevicesAsync();
            System.Diagnostics.Debug.WriteLine($"🔄 Found {savedDevices.Count()} saved WiFi devices");
            
            var devicesWithActions = DropdownItems.Where(item => item.HasActions).ToList();
            System.Diagnostics.Debug.WriteLine($"🔄 Found {devicesWithActions.Count} dropdown items with actions");
            
            foreach (var dropdownItem in devicesWithActions)
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Processing dropdown item: {dropdownItem.Id} - {dropdownItem.Text}");
                
                var device = savedDevices.FirstOrDefault(d => d.DeviceId == dropdownItem.Id);
                if (device == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"❌ No matching device found for dropdown item {dropdownItem.Id}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"🔄 Testing connectivity for device: {device.Name} (SSID: {device.Ssid})");
                
                // Test connectivity to the device
                var isConnected = await TestDeviceConnectivity(device);
                
                System.Diagnostics.Debug.WriteLine($"🔄 Previous status: {dropdownItem.IsConnected}, New status: {isConnected}");
                
                // Update the UI if status changed
                if (dropdownItem.IsConnected != isConnected)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        dropdownItem.IsConnected = isConnected;
                        
                        System.Diagnostics.Debug.WriteLine($"📱 ✅ Updated {device.Name} status: {(isConnected ? "Connected" : "Disconnected")}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"📱 ➡️ No change for {device.Name} - status remains: {(isConnected ? "Connected" : "Disconnected")}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"🔄 === WiFi Device Status Check Complete ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking WiFi device status: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    private async Task CheckLocalDeviceStatusAsync()
    {
        if (CurrentActiveTab != "LocalDev" || !DropdownItems.Any())
            return;

        try
        {
            foreach (var dropdownItem in DropdownItems.Where(item => item.HasActions).ToList())
            {
                // Extract IP address from the text (format: "Device Name\n192.168.x.x")
                var lines = dropdownItem.Text.Split('\n');
                if (lines.Length < 2) continue;

                // lines[1] can contain "<ip> • Last seen: HH:mm" – extract just the IPv4 portion
                var secondLine = lines[1];
                // Quick cut before separator if present
                var beforeSeparator = secondLine.Split('•')[0];
                // Use regex to extract IPv4
                var match = Regex.Match(beforeSeparator, "\\b((25[0-5]|2[0-4]\\d|[0-1]?\\d?\\d)(\\.)){3}(25[0-5]|2[0-4]\\d|[0-1]?\\d?\\d)\\b");
                if (!match.Success) continue;
                var ipAddress = match.Value;
                
                // Query /intellidrive/version and mark online on successful JSON response
                var (success, _, _) = await _apiService.TestIntellidriveConnectionAsync(ipAddress);
                var isConnected = success;
                
                // Update the UI if status changed
                if (dropdownItem.IsConnected != isConnected)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        dropdownItem.IsConnected = isConnected;
                        
                        System.Diagnostics.Debug.WriteLine($"🏠 Updated local device {lines[0]} ({ipAddress}) status: {(isConnected ? "Connected" : "Disconnected")} (via /intellidrive/version)");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking local device status: {ex.Message}");
        }
    }

    private async Task<bool> TestDeviceConnectivity(DeviceModel device)
    {
        try
        {
            // Use the EXACT SAME logic as SaveDevicePage for consistency
            // Get current connected network
            var currentSsid = await _wifiService.GetCurrentNetworkSsidAsync();
            var isCurrentlyConnected = !string.IsNullOrEmpty(currentSsid) && 
                                     !string.IsNullOrEmpty(device.Ssid) &&
                                     string.Equals(currentSsid.Trim(), device.Ssid.Trim(), StringComparison.OrdinalIgnoreCase);
            
            System.Diagnostics.Debug.WriteLine($"🔍 === WiFi Connectivity Test (SAME AS SaveDevicePage) ===");
            System.Diagnostics.Debug.WriteLine($"🔍 Current WiFi SSID: '{currentSsid}'");
            System.Diagnostics.Debug.WriteLine($"🔍 Device SSID: '{device.Ssid}'");
            System.Diagnostics.Debug.WriteLine($"🔍 Device Name: '{device.Name}'");
            System.Diagnostics.Debug.WriteLine($"🔍 Final isConnected: {isCurrentlyConnected}");
            
            if (isCurrentlyConnected)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Connected to WiFi device '{device.Name}' network (SSID: '{device.Ssid}')");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Not connected to WiFi device '{device.Name}' network. Current: '{currentSsid}', Expected: '{device.Ssid}'");
            }
            
            return isCurrentlyConnected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error testing WiFi device connectivity for {device.Name}: {ex.Message}");
            return false;
        }
    }

    // Dispose method to clean up timer
    public void Dispose()
    {
        StopWifiStatusMonitoring();
        MessagingCenter.Unsubscribe<LocalDevicesScanPageViewModel>(this, "LocalDeviceAdded");
    }

    #endregion
}
