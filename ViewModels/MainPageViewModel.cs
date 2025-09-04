using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace ReisingerIntelliApp_V4.ViewModels;

public class MainPageViewModel : BaseViewModel, IDisposable
{
    private readonly IDeviceService _deviceService;
    private readonly IAuthenticationService _authService;
    private readonly IBuildingStorageService _buildingStorage;
    private readonly PdfStorageService _pdfStorageService;
    private readonly WiFiManagerService _wifiService;
    private readonly IntellidriveApiService _apiService;
    private IPlanViewportService? _viewport; // optional, provided by view
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
    private string? _selectedBuildingName;
    private string? _selectedLevelName;
    public StructuresViewModel StructuresVM { get; }

    public MainPageViewModel(IDeviceService deviceService, IAuthenticationService authService, WiFiManagerService wifiService, IntellidriveApiService apiService, IBuildingStorageService buildingStorage, StructuresViewModel structuresVM, PdfStorageService pdfStorage, IPlanViewportService? viewport = null)
    {
        _deviceService = deviceService;
        _authService = authService;
        _wifiService = wifiService;
        _apiService = apiService;
    _buildingStorage = buildingStorage;
    _pdfStorageService = pdfStorage;
    StructuresVM = structuresVM;
    _viewport = viewport;
        Title = "Reisinger App";
        TabTappedCommand = new Command<string>(OnTabTapped);
        LeftSectionTappedCommand = new Command(OnLeftSectionTapped);
        CenterButtonTappedCommand = new Command(OnCenterButtonTapped);
        RightSectionTappedCommand = new Command(OnRightSectionTapped);
        ScanButtonTappedCommand = new Command(OnScanButtonTapped);
        DeleteDeviceFromDropdownCommand = new Command<DropdownItemModel>(OnDeleteDeviceFromDropdown);
    ShowDeviceOptionsCommand = new Command<DropdownItemModel>(OnShowDeviceOptions);
    AddDeviceToFloorPlanCommand = new AsyncRelayCommand<DropdownItemModel>(AddDeviceToCurrentFloorAsync);
    IncreaseDeviceScaleCommand = new AsyncRelayCommand<PlacedDeviceModel>(pd => ChangeDeviceScaleAsync(pd, +0.1));
    DecreaseDeviceScaleCommand = new AsyncRelayCommand<PlacedDeviceModel>(pd => ChangeDeviceScaleAsync(pd, -0.1));
        
        InitializeDropdownData();
        
        // Subscribe to device added messages
        MessagingCenter.Subscribe<LocalDevicesScanPageViewModel>(this, "LocalDeviceAdded", async (sender) =>
        {
            if (CurrentActiveTab == "LocalDev")
            {
                await LoadLocalDevicesAsync();
            }
        });
        // Also handle message sent from SaveLocalDevicePageViewModel (with payload deviceId)
        MessagingCenter.Subscribe<SaveLocalDevicePageViewModel, string>(this, "LocalDeviceAdded", async (sender, deviceId) =>
        {
            if (CurrentActiveTab == "LocalDev")
            {
                await LoadLocalDevicesAsync();
            }
        });

        // Listen for building saved to refresh and auto-open Levels
        MessagingCenter.Subscribe<StructureEditorViewModel, string>(this, "BuildingSaved", async (sender, buildingName) =>
        {
            await LoadStructuresAsync();
            SelectedBuildingName = buildingName;
            // Auto-open Levels and preselect first level if available
            ShowDropdownForTab("Levels");
            var structures = await _buildingStorage.LoadAsync();
            var selected = structures.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
            SelectedLevelName = selected?.Floors?.FirstOrDefault()?.FloorName;
            _ = ApplyStructureSelectionAsync(SelectedBuildingName, SelectedLevelName);
        });

        // Listen for floor plan changes to refresh current viewer if affected
        MessagingCenter.Subscribe<StructureEditorViewModel, (string building, string floor)>(this, "FloorPlanChanged", async (sender, payload) =>
        {
            if (string.Equals(SelectedBuildingName, payload.building, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(SelectedLevelName, payload.floor, StringComparison.OrdinalIgnoreCase))
            {
                await StructuresVM.RefreshCurrentFloorPlanAsync();
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
    public IAsyncRelayCommand<DropdownItemModel> AddDeviceToFloorPlanCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> IncreaseDeviceScaleCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> DecreaseDeviceScaleCommand { get; }

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

    // Tracks the currently selected building from the Structures tab
    public string? SelectedBuildingName
    {
        get => _selectedBuildingName;
        set
        {
            if (SetProperty(ref _selectedBuildingName, value))
            {
                _ = ApplyStructureSelectionAsync(_selectedBuildingName, _selectedLevelName);
            }
        }
    }

    // Tracks the currently selected level within the selected building
    public string? SelectedLevelName
    {
        get => _selectedLevelName;
        set
        {
            if (SetProperty(ref _selectedLevelName, value))
            {
                _ = ApplyStructureSelectionAsync(_selectedBuildingName, _selectedLevelName);
            }
        }
    }

    // Event to notify the View about tab changes
    public event EventHandler<string>? TabActivated;
    public event EventHandler? TabDeactivated;

    // Cache for dropdown data
    private readonly Dictionary<string, (string Title, List<DropdownItemModel> Items)> _dropdownCache = new();

    private void InitializeDropdownData()
    {
        // Pre-cache all dropdown data for instant access
    // Initial placeholders; will be replaced by persisted Buildings/Levels when tabs open
    _dropdownCache["Structures"] = ("Structures", new List<DropdownItemModel>());
    _dropdownCache["Levels"] = ("Levels", new List<DropdownItemModel>());

    _dropdownCache["WifiDev"] = ("Wifi Devices", new List<DropdownItemModel>());

    _dropdownCache["LocalDev"] = ("Local Devices", new List<DropdownItemModel>());
    }

    public async Task ApplyStructureSelectionAsync(string? buildingName, string? levelName)
    {
        try
        {
            await StructuresVM.LoadAsync(buildingName);
            if (!string.IsNullOrWhiteSpace(buildingName))
            {
                StructuresVM.SelectedBuilding = StructuresVM.Buildings.FirstOrDefault(b => b.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(levelName) && StructuresVM.SelectedBuilding != null)
            {
                StructuresVM.SelectedLevel = StructuresVM.Levels.FirstOrDefault(f => f.FloorName.Equals(levelName, StringComparison.OrdinalIgnoreCase));
            }
            await StructuresVM.RefreshCurrentFloorPlanAsync();
        }
        catch { }
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
        else if (tabName == "Structures")
        {
            _ = LoadStructuresAsync();
        }
        else if (tabName == "Levels")
        {
            _ = LoadLevelsAsync();
        }
        else if (_dropdownCache.TryGetValue(tabName, out var cachedData))
        {
            DropdownTitle = cachedData.Title;
            // Only show scan button for device tabs; Structures uses the main '+'
            ShowScanButton = tabName == "LocalDev";
            
            // Set scan button text based on tab
            ScanButtonText = tabName switch
            {
                "LocalDev" => "Scan Local Network for Devices",
                _ => string.Empty
            };
            
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

                // Determine which devices are already placed on the currently selected floor
                var placedIds = new HashSet<string>(StructuresVM.SelectedLevel?.PlacedDevices?.Select(pd => pd.DeviceInfo.DeviceId) ?? Enumerable.Empty<string>());

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
                        ShowStatus = true,
                        IsConnected = false, // Start with disconnected, will be updated immediately by monitoring
                        IsPlacedOnCurrentFloor = placedIds.Contains(device.DeviceId),
                        IsActionEnabled = !placedIds.Contains(device.DeviceId)
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

                // Determine which devices are already placed on the currently selected floor
                var placedIds = new HashSet<string>(StructuresVM.SelectedLevel?.PlacedDevices?.Select(pd => pd.DeviceInfo.DeviceId) ?? Enumerable.Empty<string>());

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
                        ShowStatus = true,
                        IsConnected = false, // Start with disconnected, will be updated immediately by monitoring
                        IsPlacedOnCurrentFloor = placedIds.Contains(device.DeviceId),
                        IsActionEnabled = !placedIds.Contains(device.DeviceId)
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

    private async Task LoadStructuresAsync()
    {
        DropdownTitle = "Structures";
        // Add Building moved to main '+'
        ShowScanButton = false;
        ScanButtonText = string.Empty;
        DropdownItems.Clear();
        DropdownItems.Add(new DropdownItemModel { Id = "loading", Icon = "loading.svg", Text = "Loading buildings..." });
        IsDropdownVisible = true;

        var buildings = await _buildingStorage.LoadAsync();
        DropdownItems.Clear();
        if (buildings.Count == 0)
        {
            DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "No buildings yet\nUse '+' to add", HasActions = false });
            return;
        }
        foreach (var b in buildings)
        {
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = b.BuildingName, 
                Icon = "home.svg", 
                Text = b.BuildingName, 
                HasActions = true, // Enable actions for buildings to show delete button
                ShowStatus = false, 
                IsSelected = string.Equals(SelectedBuildingName, b.BuildingName, StringComparison.OrdinalIgnoreCase) 
            });
        }
    }

    private async Task LoadLevelsAsync()
    {
        DropdownTitle = "Levels";
        ShowScanButton = false;
        DropdownItems.Clear();
        DropdownItems.Add(new DropdownItemModel { Id = "loading", Icon = "loading.svg", Text = "Loading floors..." });
        IsDropdownVisible = true;

        var structures = await _buildingStorage.LoadAsync();
        DropdownItems.Clear();

        if (string.IsNullOrWhiteSpace(SelectedBuildingName))
        {
            DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "Select a building in Structures first" });
            return;
        }

        var selected = structures.FirstOrDefault(b => b.BuildingName.Equals(SelectedBuildingName, StringComparison.OrdinalIgnoreCase));
        if (selected == null || selected.Floors.Count == 0)
        {
            DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "No floors to display" });
            return;
        }
        foreach (var f in selected.Floors)
        {
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = f.FloorName, 
                Icon = "levels.svg", 
                Text = f.FloorName, 
                HasActions = true, // Enable actions for floors to show delete button
                ShowStatus = false, 
                IsSelected = string.Equals(SelectedLevelName, f.FloorName, StringComparison.OrdinalIgnoreCase) 
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
        // Reset MainPage to default state - close all dropdowns and clear selections
        CloseDropdown();
        
        // Clear any selected building/level to reset the view completely
        SelectedBuildingName = null;
        SelectedLevelName = null;
        
        // Reset StructuresVM to clean state
        _ = Task.Run(async () =>
        {
            try
            {
                await StructuresVM.LoadAsync(null);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StructuresVM.SelectedBuilding = null;
                    StructuresVM.SelectedLevel = null;
                });
            }
            catch { }
        });
    }

    private async void OnCenterButtonTapped()
    {
        // Always navigate to structure editor for adding/editing buildings and floors
        await Shell.Current.GoToAsync("structureeditor");
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
        else if (CurrentActiveTab == "Structures")
        {
            await Shell.Current.GoToAsync("structureeditor");
        }
        
        CloseDropdown();
    }

    private async void OnDeleteDeviceFromDropdown(DropdownItemModel device)
    {
        if (device == null) return;

        try
        {
            // Branch by current tab
            if (CurrentActiveTab == "Structures")
            {
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Gebäude löschen",
                    $"Gebäude '{device.Text}' und alle zugehörigen Stockwerke löschen?",
                    "Löschen",
                    "Abbrechen");
                if (!confirm) return;

                var list = await _buildingStorage.LoadAsync();
                var toRemove = list.FirstOrDefault(b => b.BuildingName.Equals(device.Id, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    // Also clean up all floor assets (PDF/PNG files) for all floors in this building
                    foreach (var floor in toRemove.Floors)
                    {
                        await _pdfStorageService.DeleteFloorAssetsAsync(toRemove, floor);
                    }
                    list.Remove(toRemove);
                    await _buildingStorage.SaveAsync(list);
                    
                    // Clear selection if deleted building was selected
                    if (string.Equals(SelectedBuildingName, device.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedBuildingName = null;
                        SelectedLevelName = null;
                    }
                }
                // Refresh Structures view
                await LoadStructuresAsync();
                
                await Application.Current.MainPage.DisplayAlert(
                    "Erfolg", 
                    $"Gebäude '{device.Text}' wurde erfolgreich gelöscht.", 
                    "OK");
                return;
            }
            if (CurrentActiveTab == "Levels")
            {
                if (string.IsNullOrWhiteSpace(SelectedBuildingName))
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "Wählen Sie zuerst ein Gebäude aus.", "OK");
                    return;
                }
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Stockwerk löschen",
                    $"Stockwerk '{device.Text}' aus '{SelectedBuildingName}' löschen?",
                    "Löschen",
                    "Abbrechen");
                if (!confirm) return;

                var list = await _buildingStorage.LoadAsync();
                var building = list.FirstOrDefault(b => b.BuildingName.Equals(SelectedBuildingName, StringComparison.OrdinalIgnoreCase));
                if (building != null)
                {
                    var floor = building.Floors.FirstOrDefault(f => f.FloorName.Equals(device.Id, StringComparison.OrdinalIgnoreCase));
                    if (floor != null)
                    {
                        await _pdfStorageService.DeleteFloorAssetsAsync(building, floor);
                        building.Floors.Remove(floor);
                        if (string.Equals(SelectedLevelName, device.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            SelectedLevelName = null;
                        }
                    }
                    await _buildingStorage.SaveAsync(list);
                }
                // Refresh Levels view
                await LoadLevelsAsync();
                
                await Application.Current.MainPage.DisplayAlert(
                    "Erfolg", 
                    $"Stockwerk '{device.Text}' wurde erfolgreich gelöscht.", 
                    "OK");
                return;
            }

            // Default device deletion logic
            var confirmDelete = await Application.Current.MainPage.DisplayAlert(
                "Delete Device",
                $"Are you sure you want to delete the device '{device.Text.Split('\n')[0]}'?",
                "Delete",
                "Cancel");

            if (confirmDelete)
            {
                if (CurrentActiveTab == "WifiDev")
                {
                    var deviceToDelete = new DeviceModel
                    {
                        DeviceId = device.Id,
                        Name = device.Text.Split('\n')[0],
                        ConnectionType = ConnectionType.Wifi
                    };
                    await _deviceService.DeleteDeviceAsync(deviceToDelete);
                }
                else if (CurrentActiveTab == "LocalDev")
                {
                    var deviceToDelete = new DeviceModel
                    {
                        DeviceId = device.Id,
                        Name = device.Text.Split('\n')[0],
                        ConnectionType = ConnectionType.Local
                    };
                    await _deviceService.DeleteDeviceAsync(deviceToDelete);
                }

                DropdownItems.Remove(device);

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
                    "Deleted successfully.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to delete: {ex.Message}",
                "OK");
        }
    }

    private async void OnShowDeviceOptions(DropdownItemModel device)
    {
        if (device == null) return;

        try
        {
            if (CurrentActiveTab == "Structures")
            {
                // Open building editor
                var route = $"structureeditor?name={Uri.EscapeDataString(device.Id)}";
                await Shell.Current.GoToAsync(route);
                return;
            }
            if (CurrentActiveTab == "Levels")
            {
                // Open building editor for the selected building to manage floors
                if (string.IsNullOrWhiteSpace(SelectedBuildingName))
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "Select a building first.", "OK");
                    return;
                }
                var route = $"structureeditor?name={Uri.EscapeDataString(SelectedBuildingName)}";
                await Shell.Current.GoToAsync(route);
                return;
            }

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

    #region Device placement and scaling

    private async Task AddDeviceToCurrentFloorAsync(DropdownItemModel? item)
    {
        try
        {
            // Preconditions
            if (StructuresVM.SelectedBuilding is null || StructuresVM.SelectedLevel is null)
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Select building and level first.", "OK");
                return;
            }
            if (_viewport is null || !_viewport.IsPlanReady)
            {
                await Application.Current.MainPage.DisplayAlert("Info", "Floor plan not ready.", "OK");
                return;
            }

            // Resolve device info from saved lists by id
            if (item == null || string.IsNullOrWhiteSpace(item.Id)) return;
            DeviceModel? source = null;
            if (CurrentActiveTab == "WifiDev")
            {
                var saved = await _deviceService.GetSavedWifiDevicesAsync();
                source = saved.FirstOrDefault(d => d.DeviceId == item.Id);
            }
            else if (CurrentActiveTab == "LocalDev")
            {
                var saved = await _deviceService.GetSavedLocalDevicesAsync();
                source = saved.FirstOrDefault(d => d.DeviceId == item.Id);
            }
            if (source == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Device not found.", "OK");
                return;
            }

            // Prevent duplicates per floor: check if a device with same DeviceId already exists on this floor
            var alreadyPlaced = StructuresVM.SelectedLevel.PlacedDevices.Any(pd => pd.DeviceInfo?.DeviceId == source.DeviceId);
            if (alreadyPlaced)
            {
                await Application.Current.MainPage.DisplayAlert("Info", "This device is already placed on the selected floor.", "OK");
                // Update dropdown item to reflect disabled state
                var dropdownMatch = DropdownItems.FirstOrDefault(d => d.Id == source.DeviceId);
                if (dropdownMatch != null)
                {
                    dropdownMatch.IsPlacedOnCurrentFloor = true;
                    dropdownMatch.IsActionEnabled = false;
                }
                return;
            }

            // Place exactly at the center of the plan (normalized coordinates)
            // Using plan center ensures the device lands visually in the middle of the displayed floor plan
            // regardless of current zoom/pan state or letterboxing from AspectFit.
            var xNorm = 0.5;
            var yNorm = 0.5;

            var placed = new PlacedDeviceModel(source)
            {
                PlacedDeviceId = Guid.NewGuid().ToString("N"),
                XCenterNorm = xNorm,
                YCenterNorm = yNorm,
                BaseWidthNorm = 0.15, // Use new larger default
                BaseHeightNorm = 0.18, // Use new larger default
                Scale = 1.0,
                BuildingId = 0,
                FloorId = 0,
            };

            // Persist to selected floor
            StructuresVM.SelectedLevel.PlacedDevices.Add(placed);

            // Save all buildings
            await PersistBuildingsAsync();

            // Notify plan to refresh rendering if needed - IMMEDIATE refresh even with dropdown open
            await StructuresVM.RefreshCurrentFloorPlanAsync();
            
            // Force immediate layout refresh via MessagingCenter to ensure devices appear right away
            MessagingCenter.Send(this, "ForceDeviceLayoutRefresh");

            // Update dropdown item state after successful placement
            var placedItem = DropdownItems.FirstOrDefault(d => d.Id == source.DeviceId);
            if (placedItem != null)
            {
                placedItem.IsPlacedOnCurrentFloor = true;
                placedItem.IsActionEnabled = false;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ChangeDeviceScaleAsync(PlacedDeviceModel? device, double delta)
    {
        if (device == null) return;
        const double min = 0.05, max = 2.5; // Reduced min from 0.2 to 0.05 for much smaller scaling
        var newScale = Math.Clamp(device.Scale + delta, min, max);
        if (Math.Abs(newScale - device.Scale) < 0.0001) return;
        device.Scale = newScale;
        await PersistBuildingsAsync();
    }

    private async Task PersistBuildingsAsync()
    {
        // Save entire building list preserving floors and placed devices
        var list = await _buildingStorage.LoadAsync();
        var b = list.FirstOrDefault(x => x.BuildingName.Equals(StructuresVM.SelectedBuilding?.BuildingName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        if (b != null)
        {
            var f = b.Floors.FirstOrDefault(x => x.FloorName.Equals(StructuresVM.SelectedLevel?.FloorName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (f != null)
            {
                // Replace PlacedDevices with current instance
                f.PlacedDevices = StructuresVM.SelectedLevel!.PlacedDevices;
            }
        }
        await _buildingStorage.SaveAsync(list);
    }

    #endregion

    public void AttachViewport(IPlanViewportService viewport)
    {
        _viewport = viewport;
    }

    public async Task SaveCurrentFloorAsync()
    {
        await PersistBuildingsAsync();
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

    /// <summary>
    /// Saves changes to placed devices (scale, position, etc.) to persistent storage
    /// </summary>
    // Dispose method to clean up timer
    public void Dispose()
    {
        StopWifiStatusMonitoring();
    MessagingCenter.Unsubscribe<LocalDevicesScanPageViewModel>(this, "LocalDeviceAdded");
    MessagingCenter.Unsubscribe<SaveLocalDevicePageViewModel, string>(this, "LocalDeviceAdded");
    }

    #endregion
}
