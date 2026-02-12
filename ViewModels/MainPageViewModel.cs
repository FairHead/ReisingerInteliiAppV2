using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.ComponentModel;
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
    // Remember the last selected level per building during app session
    private readonly Dictionary<string, string> _lastLevelByBuilding = new(StringComparer.OrdinalIgnoreCase);
    // Reentrancy guards to prevent feedback loops between VM <-> StructuresVM
    private bool _isApplyingSelection = false;
    private bool _isMirroringFromStructuresVM = false;
    
    // New fields for dropdown improvements
    private bool _showStructuresEmptyState;
    private bool _isLevelDropdownEnabled;
    private bool _isStructuresDropdownOpen;
    private bool _isLevelsDropdownOpen;
    private bool _isDevicesDropdownOpen;
    private bool _isDevicePanelOpen;
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
        // Beim App-Start alle Dropdown-Listen leeren
        DropdownItems.Clear();
        _dropdownCache["Structures"] = ("Structures", new List<DropdownItemModel>());
        _dropdownCache["Levels"] = ("Levels", new List<DropdownItemModel>());
        _dropdownCache["WifiDev"] = ("Wifi Devices", new List<DropdownItemModel>());
        _dropdownCache["LocalDev"] = ("Local Devices", new List<DropdownItemModel>());
        
        // Ensure Level dropdown starts as disabled (no structure selected initially)
        IsLevelDropdownEnabled = false;
        
        Title = "Reisinger App";
        TabTappedCommand = new Command<string>(OnTabTapped);
        LeftSectionTappedCommand = new Command(OnLeftSectionTapped);
        CenterButtonTappedCommand = new Command(OnCenterButtonTapped);
        RightSectionTappedCommand = new Command(OnRightSectionTapped);
        ScanButtonTappedCommand = new Command(OnScanButtonTapped);
        NavigateToStructureEditorCommand = new Command<DropdownItemModel>(OnNavigateToStructureEditor);
        DeleteDeviceFromDropdownCommand = new AsyncRelayCommand<DropdownItemModel>(
            async (item) =>
            {
                try
                {
                    bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                        "Gerät löschen",
                        $"Möchtest du '{item.Text}' wirklich entfernen?",
                        "Ja",
                        "Nein");

                    if (!confirm)
                        return;

                    OnDeleteDeviceFromDropdown(item);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Delete Confirm Exception: {ex}");
                }
            });

        ShowDeviceOptionsCommand = new Command<DropdownItemModel>(OnShowDeviceOptions);
        CardSettingsCommand = new AsyncRelayCommand<DropdownItemModel>(OnCardSettingsTapped,
            (item) => item != null);
        AddDeviceToFloorPlanCommand = new AsyncRelayCommand<DropdownItemModel>(AddDeviceToCurrentFloorAsync);
        IncreaseDeviceScaleCommand = new AsyncRelayCommand<PlacedDeviceModel>(pd => ChangeDeviceScaleAsync(pd, +0.1));
        DecreaseDeviceScaleCommand = new AsyncRelayCommand<PlacedDeviceModel>(pd => ChangeDeviceScaleAsync(pd, -0.1));
        DebugClearStorageCommand = new AsyncRelayCommand(DebugClearStorageAsync);
        
        InitializeDropdownData();

        // Removed dummy device seeding (previously added placeholder devices on empty storage)
        
        // Subscribe to device added messages
        #pragma warning disable CS0618 // MessagingCenter is obsolete; migration planned to WeakReferenceMessenger
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
        
        // Listen for device updates (name changes, etc.) to sync with PlacedDevices
        MessagingCenter.Subscribe<SaveLocalDevicePageViewModel, DeviceModel>(this, "DeviceUpdated", async (sender, updatedDevice) =>
        {
            await UpdatePlacedDevicesWithDeviceInfo(updatedDevice);
        });
        
        // Also listen for WiFi device updates
        MessagingCenter.Subscribe<SaveDevicePageViewModel, DeviceModel>(this, "DeviceUpdated", async (sender, updatedDevice) =>
        {
            await UpdatePlacedDevicesWithDeviceInfo(updatedDevice);
        });


        // Listen for building saved to refresh and open Levels without forcing a level selection
        MessagingCenter.Subscribe<StructureEditorViewModel, string>(this, "BuildingSaved", async (sender, buildingName) =>
        {
            await LoadStructuresAsync();
            // Select the building; ApplyStructureSelectionAsync will handle last-used/first level
            SelectedBuildingName = buildingName;
            // Open Levels dropdown so user can see/select floors; don't force a specific floor
            ShowDropdownForTab("Levels");
        });

        // Listen for floor plan changes to refresh current viewer if affected
        MessagingCenter.Subscribe<StructureEditorViewModel, (string building, string floor)>(this, "FloorPlanChanged", async (sender, payload) =>
        {
            if (string.Equals(SelectedBuildingName, payload.building, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(SelectedLevelName, payload.floor, StringComparison.OrdinalIgnoreCase))
            {
                await StructuresVM.RefreshCurrentFloorPlanAsync();
            }
            
            // Update level access whenever floor plans change (new levels might be available)
            UpdateLevelDropdownAccess();
        });
        #pragma warning restore CS0618
        
        // Subscribe to StructuresVM property changes for synchronization
        StructuresVM.PropertyChanged += OnStructuresVMPropertyChanged;
    }

    public ICommand TabTappedCommand { get; }
    public ICommand LeftSectionTappedCommand { get; }
    public ICommand CenterButtonTappedCommand { get; }
    public ICommand RightSectionTappedCommand { get; }
    public ICommand ScanButtonTappedCommand { get; }
    public IAsyncRelayCommand<DropdownItemModel> DeleteDeviceFromDropdownCommand { get; }
    public ICommand ShowDeviceOptionsCommand { get; }
    public IAsyncRelayCommand<DropdownItemModel> CardSettingsCommand { get; }
    public IAsyncRelayCommand<DropdownItemModel> AddDeviceToFloorPlanCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> IncreaseDeviceScaleCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> DecreaseDeviceScaleCommand { get; }
    public IAsyncRelayCommand DebugClearStorageCommand { get; }

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

    // New properties for dropdown improvements
    
    /// <summary>
    /// Shows empty state card when no structures are available
    /// </summary>
    public bool ShowStructuresEmptyState
    {
        get => _showStructuresEmptyState;
        set => SetProperty(ref _showStructuresEmptyState, value);
    }

    /// <summary>
    /// Controls whether Level dropdown is accessible (disabled when no structure selected)
    /// </summary>
    public bool IsLevelDropdownEnabled
    {
        get => _isLevelDropdownEnabled;
        set => SetProperty(ref _isLevelDropdownEnabled, value);
    }

    /// <summary>
    /// Indicates if Structures dropdown is currently open for background overlay
    /// </summary>
    public bool IsStructuresDropdownOpen
    {
        get => _isStructuresDropdownOpen;
        set => SetProperty(ref _isStructuresDropdownOpen, value);
    }

    /// <summary>
    /// Indicates if Levels dropdown is currently open for background overlay
    /// </summary>
    public bool IsLevelsDropdownOpen
    {
        get => _isLevelsDropdownOpen;
        set => SetProperty(ref _isLevelsDropdownOpen, value);
    }

    /// <summary>
    /// Indicates if Devices dropdown is currently open for background overlay
    /// </summary>
    public bool IsDevicesDropdownOpen
    {
        get => _isDevicesDropdownOpen;
        set => SetProperty(ref _isDevicesDropdownOpen, value);
    }

    /// <summary>
    /// Controls the collapsible placed-devices panel above the footer.
    /// </summary>
    public bool IsDevicePanelOpen
    {
        get => _isDevicePanelOpen;
        set => SetProperty(ref _isDevicePanelOpen, value);
    }

    /// <summary>
    /// True when the selected floor has at least one placed device.
    /// </summary>
    public bool HasPlacedDevices
        => StructuresVM?.SelectedLevel?.PlacedDevices?.Count > 0;

    /// <summary>
    /// Notifies the UI that HasPlacedDevices may have changed.
    /// </summary>
    public void RefreshHasPlacedDevices() => OnPropertyChanged(nameof(HasPlacedDevices));

    /// <summary>
    /// Command for navigating to StructureEditor (reused from center button)
    /// </summary>
    public ICommand NavigateToStructureEditorCommand { get; }

    // Tracks the currently selected building from the Structures tab
    public string? SelectedBuildingName
    {
        get => _selectedBuildingName;
        set
        {
            if (SetProperty(ref _selectedBuildingName, value))
            {
                // Optimistically enable Level dropdown when a building is selected,
                // so we can open Levels immediately without waiting for async load.
                IsLevelDropdownEnabled = !string.IsNullOrWhiteSpace(_selectedBuildingName);
                if (!_isMirroringFromStructuresVM)
                {
                    _ = ApplyStructureSelectionAsync(_selectedBuildingName, _selectedLevelName);
                }
                // Update Level dropdown access control after structure selection
                UpdateLevelDropdownAccess();
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
                if (!_isMirroringFromStructuresVM)
                {
                    _ = ApplyStructureSelectionAsync(_selectedBuildingName, _selectedLevelName);
                }
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
    System.Diagnostics.Debug.WriteLine("[InitializeDropdownData] Pre-caching dropdown data for all tabs");
    _dropdownCache["Structures"] = ("Structures", new List<DropdownItemModel>());
    _dropdownCache["Levels"] = ("Levels", new List<DropdownItemModel>());
    _dropdownCache["WifiDev"] = ("Wifi Devices", new List<DropdownItemModel>());
    _dropdownCache["LocalDev"] = ("Local Devices", new List<DropdownItemModel>());
    System.Diagnostics.Debug.WriteLine($"[InitializeDropdownData] Cache keys: {string.Join(", ", _dropdownCache.Keys)}");
    }

    public async Task ApplyStructureSelectionAsync(string? buildingName, string? levelName)
    {
        try
        {
            if (_isApplyingSelection) return;
            _isApplyingSelection = true;
            // If a building is ALREADY selected in the VM and the level is being de-selected (null),
            // avoid reloading or re-assigning SelectedBuilding (which would auto-select first level).
            // Only short-circuit when the incoming building matches the currently selected building.
            if (!string.IsNullOrWhiteSpace(buildingName)
                && string.IsNullOrWhiteSpace(levelName)
                && StructuresVM.SelectedBuilding != null
                && string.Equals(StructuresVM.SelectedBuilding.BuildingName, buildingName, StringComparison.OrdinalIgnoreCase))
            {
                // Keep the current building, just clear the level selection
                StructuresVM.SelectedLevel = null;
                // Update level access after deselection
                UpdateLevelDropdownAccess();
                return;
            }

            await StructuresVM.LoadAsync(buildingName);
            // Note: LoadAsync(selectBuilding) already sets SelectedBuilding when provided; avoid redundant assignment
            if (StructuresVM.SelectedBuilding != null)
            {
                if (!string.IsNullOrWhiteSpace(levelName))
                {
                    StructuresVM.SelectedLevel = StructuresVM.Levels.FirstOrDefault(f => f.FloorName.Equals(levelName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    var bName = StructuresVM.SelectedBuilding.BuildingName;
                    if (!string.IsNullOrWhiteSpace(bName) && _lastLevelByBuilding.TryGetValue(bName, out var lastLevel))
                    {
                        var found = StructuresVM.Levels.FirstOrDefault(f => f.FloorName.Equals(lastLevel, StringComparison.OrdinalIgnoreCase));
                        StructuresVM.SelectedLevel = found ?? StructuresVM.Levels.FirstOrDefault();
                    }
                    else
                    {
                        StructuresVM.SelectedLevel = StructuresVM.Levels.FirstOrDefault();
                    }
                }
            }
            await StructuresVM.RefreshCurrentFloorPlanAsync();
            
            // Update level access after loading structure data
            UpdateLevelDropdownAccess();
        }
        catch { }
        finally
        {
            _isApplyingSelection = false;
        }
    }
    /// <summary>
    /// Updates Level dropdown access control. Level stays disabled (red-transparent) 
    /// until levels are actually available in dropdown list.
    /// </summary>
    private void UpdateLevelDropdownAccess()
    {
        // Level dropdown is only enabled if:
        // 1. A building is selected AND
        // 2. That building has levels/floors available
        bool hasSelectedBuilding = !string.IsNullOrWhiteSpace(_selectedBuildingName);
        int levelsCount = StructuresVM?.Levels?.Count ?? 0;
        bool hasLevelsAvailable = hasSelectedBuilding && levelsCount > 0;
        
        IsLevelDropdownEnabled = hasLevelsAvailable;
        
        System.Diagnostics.Debug.WriteLine($"[UpdateLevelDropdownAccess] Building='{_selectedBuildingName}' HasSelectedBuilding={hasSelectedBuilding} LevelsCount={levelsCount} HasLevelsAvailable={hasLevelsAvailable} IsLevelDropdownEnabled={IsLevelDropdownEnabled}");
        
        // Debug: List all levels
        if (StructuresVM?.Levels != null)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateLevelDropdownAccess] Available levels: {string.Join(", ", StructuresVM.Levels.Select(l => $"'{l?.FloorName}'"))}");
        }
    }

    /// <summary>
    /// Checks if Level tab style can be modified. Returns false if Level should stay disabled (red-transparent).
    /// Only when levels are actually created/available should Level tab be styleable.
    /// </summary>
    public bool CanModifyLevelTabStyle => IsLevelDropdownEnabled;

    /// <summary>
    /// Synchronizes SelectedLevelName with StructuresVM.SelectedLevel changes
    /// to ensure dropdown selection matches current bauplan
    /// </summary>
    private void OnStructuresVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StructuresViewModel.SelectedLevel))
        {
            // Synchronize SelectedLevelName with StructuresVM.SelectedLevel
            var newLevelName = StructuresVM.SelectedLevel?.FloorName;
            if (SelectedLevelName != newLevelName)
            {
                System.Diagnostics.Debug.WriteLine($"[OnStructuresVMPropertyChanged] Synchronizing SelectedLevelName: '{SelectedLevelName}' -> '{newLevelName}'");
                _isMirroringFromStructuresVM = true;
                try { SelectedLevelName = newLevelName; }
                finally { _isMirroringFromStructuresVM = false; }
            }
            // Remember last-selected level per building at runtime
            var bName = StructuresVM.SelectedBuilding?.BuildingName;
            if (!string.IsNullOrWhiteSpace(bName) && !string.IsNullOrWhiteSpace(newLevelName))
            {
                _lastLevelByBuilding[bName] = newLevelName!;
                System.Diagnostics.Debug.WriteLine($"[OnStructuresVMPropertyChanged] Remembered last level '{newLevelName}' for building '{bName}'");
            }
        }
        else if (e.PropertyName == nameof(StructuresViewModel.SelectedBuilding))
        {
            // Synchronize SelectedBuildingName with StructuresVM.SelectedBuilding
            var newBuildingName = StructuresVM.SelectedBuilding?.BuildingName;
            if (SelectedBuildingName != newBuildingName)
            {
                System.Diagnostics.Debug.WriteLine($"[OnStructuresVMPropertyChanged] Synchronizing SelectedBuildingName: '{SelectedBuildingName}' -> '{newBuildingName}'");
                _isMirroringFromStructuresVM = true;
                try { SelectedBuildingName = newBuildingName; }
                finally { _isMirroringFromStructuresVM = false; }
            }
        }
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

    private async void ShowDropdownForTab(string tabName)
    {
    System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] ENTRY: tabName={tabName}");
    
    // Close all dropdowns first
    CloseAllDropdowns();
    
    // Check Level access control before opening Levels dropdown
    if (tabName == "Levels" && !IsLevelDropdownEnabled)
    {
        System.Diagnostics.Debug.WriteLine("[ShowDropdownForTab] Level dropdown access denied - no levels available");
        return; // Don't open Levels dropdown if no levels available
    }
    
    CurrentActiveTab = tabName;
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] Loading data for tab: {tabName}");
            if (tabName == "WifiDev")
            {
                await LoadWifiDevicesAsync();
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] WifiDev: DropdownItems count after load: {DropdownItems.Count}");
                if (DropdownItems.Any(item => item.HasActions))
                {
                    System.Diagnostics.Debug.WriteLine("[ShowDropdownForTab] Starting WiFi status monitoring");
                    StartWifiStatusMonitoring();
                }
            }
            else if (tabName == "LocalDev")
            {
                await LoadLocalDevicesAsync();
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] LocalDev: DropdownItems count after load: {DropdownItems.Count}");
                if (DropdownItems.Any(item => item.HasActions))
                {
                    System.Diagnostics.Debug.WriteLine("[ShowDropdownForTab] Starting LocalDev status monitoring");
                    StartLocalDevStatusMonitoring();
                }
            }
            else if (tabName == "Structures")
            {
                await LoadStructuresAsync();
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] Structures: DropdownItems count after load: {DropdownItems.Count}");
            }
            else if (tabName == "Levels")
            {
                await LoadLevelsAsync();
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] Levels: DropdownItems count after load: {DropdownItems.Count}");
            }
            else if (_dropdownCache.TryGetValue(tabName, out var cachedData))
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] Using cached data for tab: {tabName}");
                DropdownTitle = cachedData.Title;
                ShowScanButton = tabName == "LocalDev";
                ScanButtonText = tabName switch
                {
                    "LocalDev" => "Scan Local Network for Devices",
                    _ => string.Empty
                };
                DropdownItems.Clear();
                foreach (var item in cachedData.Items)
                {
                    DropdownItems.Add(item);
                }
                System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] CachedData.Items count: {cachedData.Items.Count}");
                IsDropdownVisible = true;
                
                // Update dropdown state for background overlays
                UpdateDropdownStates(tabName);
                if (tabName == "LocalDev" && DropdownItems.Any(item => item.HasActions))
                {
                    System.Diagnostics.Debug.WriteLine("[ShowDropdownForTab] Starting LocalDev status monitoring (cached)");
                    StartLocalDevStatusMonitoring();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ShowDropdownForTab] Stopping WiFi status monitoring (cached)");
                    StopWifiStatusMonitoring();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] ERROR loading tab data for {tabName}: {ex.Message}");
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = "error", 
                Icon = "info.svg", 
                Text = $"Error loading {tabName} data", 
                HasActions = false 
            });
        }

        System.Diagnostics.Debug.WriteLine($"[ShowDropdownForTab] TabActivated event fired for {tabName}");
        TabActivated?.Invoke(this, tabName);
    }

    private async Task LoadWifiDevicesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[LoadWifiDevicesAsync] ENTRY");
            DropdownTitle = "WiFi Devices";
            ShowScanButton = true;
            ScanButtonText = "Scan for Devices in WiFi-Ap Mode";
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "loading",
                Icon = "loading.svg",
                Text = "Loading saved devices...",
                HasActions = false
            });
            IsDropdownVisible = true;

            var savedDevices = await _deviceService.GetSavedWifiDevicesAsync();
            System.Diagnostics.Debug.WriteLine($"[LoadWifiDevicesAsync] Loaded {savedDevices.Count} devices from storage.");
            DropdownItems.Clear();

            if (savedDevices.Any())
            {
                RemoveDefaultNoDeviceCard();
                var placedIds = new HashSet<string>(StructuresVM.SelectedLevel?.PlacedDevices?.Select(pd => pd.DeviceInfo.DeviceId) ?? Enumerable.Empty<string>());
                foreach (var device in savedDevices)
                {
                    try
                    {
                        DropdownItems.Add(new DropdownItemModel
                        {
                            Id = device.DeviceId,
                            Icon = "wifi_icon.svg",
                            Text = device.Name,
                            SubText = device.Ssid,
                            NetworkInfo = device.Ssid,
                            LastSeen = device.LastSeen.ToString("HH:mm"),
                            HasActions = true,
                            ShowStatus = true,
                            IsConnected = false,
                            IsPlacedOnCurrentFloor = placedIds.Contains(device.DeviceId),
                            IsActionEnabled = !placedIds.Contains(device.DeviceId)
                        });
                    }
                    catch { }
                }
                StartWifiStatusMonitoring();
                _ = Task.Run(async () => { await Task.Delay(100); await CheckWifiDeviceStatusAsync(); });
            }
            else
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "Keine WiFi Geräte gespeichert", HasActions = false, IsActionEnabled = false });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadWifiDevicesAsync] ERROR: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("[LoadLocalDevicesAsync] ENTRY");
            DropdownTitle = "Local Devices";
            ShowScanButton = true;
            ScanButtonText = "Scan Local Network for Devices";
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "loading",
                Icon = "loading.svg",
                Text = "Loading saved local devices...",
                HasActions = false
            });
            IsDropdownVisible = true;

            var savedDevices = await _deviceService.GetSavedLocalDevicesAsync();
            System.Diagnostics.Debug.WriteLine($"[LoadLocalDevicesAsync] Loaded {savedDevices.Count} devices from storage.");

            var validDeviceIds = new HashSet<string>(savedDevices.Select(d => d.DeviceId));
            var staleItems = DropdownItems.Where(d => d.HasActions && !validDeviceIds.Contains(d.Id) && !d.Id.StartsWith("dummy_local_")).ToList();
            foreach (var stale in staleItems)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadLocalDevicesAsync] Removing stale device from Dropdown: {stale.Id}");
                DropdownItems.Remove(stale);
            }

            DropdownItems.Clear();

            if (savedDevices.Any())
            {
                RemoveDefaultNoDeviceCard();
                var placedIds = new HashSet<string>(StructuresVM.SelectedLevel?.PlacedDevices?.Select(pd => pd.DeviceInfo.DeviceId) ?? Enumerable.Empty<string>());
                foreach (var device in savedDevices)
                {
                    try
                    {
                        DropdownItems.Add(new DropdownItemModel
                        {
                            Id = device.DeviceId,
                            Icon = "local_icon.svg",
                            Text = device.Name,
                            SubText = device.IpAddress,
                            NetworkInfo = device.IpAddress,
                            LastSeen = device.LastSeen.ToString("HH:mm"),
                            HasActions = true,
                            ShowStatus = true,
                            IsConnected = false,
                            IsPlacedOnCurrentFloor = placedIds.Contains(device.DeviceId),
                            IsActionEnabled = !placedIds.Contains(device.DeviceId)
                        });
                    }
                    catch { }
                }
                StartLocalDevStatusMonitoring();
                _ = Task.Run(async () => { await Task.Delay(100); await CheckLocalDeviceStatusAsync(); });
            }
            else
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "Keine lokalen Geräte gespeichert", HasActions = false, IsActionEnabled = false });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadLocalDevicesAsync] ERROR: {ex.Message}");
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "error",
                Icon = "error",
                Text = $"Error loading local devices:\n{ex.Message}",
                HasActions = false
            });
        }
    }

    private async Task LoadStructuresAsync()
    {
        System.Diagnostics.Debug.WriteLine("[LoadStructuresAsync] === ENTRY ===");
        System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] Current DropdownItems.Count: {DropdownItems.Count}");
        
        // Log existing items BEFORE clearing
        if (DropdownItems.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine("[LoadStructuresAsync] Existing items in dropdown BEFORE clear:");
            foreach (var existingItem in DropdownItems)
            {
                System.Diagnostics.Debug.WriteLine($"   - Id: '{existingItem.Id}', Text: '{existingItem.Text}', Icon: '{existingItem.Icon}'");
            }
        }
        
        DropdownTitle = "Structures";
        // Add Building moved to main '+'
        ShowScanButton = false;
        ScanButtonText = string.Empty;
        
        // CRITICAL: Clear items FIRST
        DropdownItems.Clear();
        System.Diagnostics.Debug.WriteLine("[LoadStructuresAsync] DropdownItems cleared. Count now: {DropdownItems.Count}");
        
        DropdownItems.Add(new DropdownItemModel { Id = "loading", Icon = "loading.svg", Text = "Loading buildings..." });
        IsDropdownVisible = true;

        try
        {
            // Remove ConfigureAwait(false) to prevent deadlock when accessing UI elements
            var buildings = await _buildingStorage.LoadAsync();
            System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] Loaded {buildings.Count} buildings from storage");
            
            // Use synchronous UI update since we're already on the correct thread
            DropdownItems.Clear();
            System.Diagnostics.Debug.WriteLine("[LoadStructuresAsync] DropdownItems cleared again after load. Count: {DropdownItems.Count}");
            
            // Update empty state visibility
            ShowStructuresEmptyState = buildings.Count == 0;
            
            if (buildings.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LoadStructuresAsync] No buildings found - showing empty state");
                // Don't add the old no-buildings card since we now have a dedicated empty state
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] Adding {buildings.Count} buildings to dropdown:");
            foreach (var b in buildings)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync]   Adding building: '{b.BuildingName}'");
                    DropdownItems.Add(new DropdownItemModel 
                    { 
                        Id = b.BuildingName, 
                        Icon = "home.svg", 
                        Text = b.BuildingName,
                        SubText = string.Empty,
                        HasActions = true, // Enable actions for buildings to show delete button
                        ShowStatus = false, 
                        IsSelected = string.Equals(SelectedBuildingName, b.BuildingName, StringComparison.OrdinalIgnoreCase) 
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] Fehler beim Hinzufügen eines Buildings: {ex.Message}");
                    // Building nicht anzeigen, einfach überspringen
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] === EXIT === Final DropdownItems.Count: {DropdownItems.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LoadStructuresAsync] Stack trace: {ex.StackTrace}");
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel 
            { 
                Id = "error", 
                Icon = "info.svg", 
                Text = "Error loading buildings", 
                HasActions = false 
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

        try
        {
            // Remove ConfigureAwait(false) to prevent deadlock when accessing UI elements
            var structures = await _buildingStorage.LoadAsync();
            // Defensive: handle null/empty structures
            if (structures == null)
            {
                DropdownItems.Clear();
                DropdownItems.Add(new DropdownItemModel { Id = "error", Icon = "info.svg", Text = "No building data found.", HasActions = false });
                return;
            }

            DropdownItems.Clear();

            if (string.IsNullOrWhiteSpace(SelectedBuildingName))
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "Select a building in Structures first" });
                return;
            }

            var selected = structures.FirstOrDefault(b => b?.BuildingName != null && b.BuildingName.Equals(SelectedBuildingName, StringComparison.OrdinalIgnoreCase));
            if (selected == null)
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "Building not found or deleted." });
                return;
            }
            if (selected.Floors == null || selected.Floors.Count == 0)
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "No floors to display" });
                return;
            }

                foreach (var f in selected.Floors)
            {
                try
                {
                    if (f == null || string.IsNullOrWhiteSpace(f.FloorName))
                        continue;
                    DropdownItems.Add(new DropdownItemModel
                    {
                        Id = f.FloorName,
                        Icon = "levels.svg",
                        Text = f.FloorName,
                        SubText = string.Empty,
                        HasActions = true,
                        ShowStatus = false,
                        IsSelected = string.Equals(SelectedLevelName, f.FloorName, StringComparison.OrdinalIgnoreCase)
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadLevelsAsync] Fehler beim Hinzufügen eines Floors: {ex.Message}");
                    // Floor nicht anzeigen, einfach überspringen
                }
            }
            if (DropdownItems.Count == 0)
            {
                DropdownItems.Add(new DropdownItemModel { Id = NO_DEVICES_ITEM_ID, Icon = "info.svg", Text = "No valid floors found." });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadLevelsAsync: {ex.Message}");
            DropdownItems.Clear();
            DropdownItems.Add(new DropdownItemModel
            {
                Id = "error",
                Icon = "info.svg",
                Text = $"Error loading floors: {ex.Message}",
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
        
        // Close all dropdown states
        CloseAllDropdowns();
        
        // Stop WiFi monitoring when dropdown is closed
        StopWifiStatusMonitoring();
        
        TabDeactivated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes all dropdown overlays by setting their state to false
    /// </summary>
    public void CloseAllDropdowns()
    {
        IsStructuresDropdownOpen = false;
        IsLevelsDropdownOpen = false;
        IsDevicesDropdownOpen = false;
    }

    /// <summary>
    /// Updates dropdown state for background overlay visibility
    /// </summary>
    private void UpdateDropdownStates(string tabName)
    {
        // First close all
        CloseAllDropdowns();
        
        // Then set the active one
        switch (tabName)
        {
            case "Structures":
                IsStructuresDropdownOpen = true;
                break;
            case "Levels":
                IsLevelsDropdownOpen = true;
                break;
            case "WifiDev":
            case "LocalDev":
                IsDevicesDropdownOpen = true;
                break;
        }
    }

    private void OnLeftSectionTapped()
    {
        // Reset MainPage to default state - close all dropdowns and clear selections
        CloseDropdown();
        
        // Immediately notify view to clear plan image and overlay (ensure instant UI reset on devices)
        #pragma warning disable CS0618 // MessagingCenter obsolete suppression until migration
        MessagingCenter.Send(this, "ResetPlanAndOverlay");
        #pragma warning restore CS0618
        
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnLeftSectionTapped] Error resetting StructuresVM: {ex.Message}");
            }
        });
    }

    private async void OnCenterButtonTapped()
    {
        // Always navigate to structure editor for adding/editing buildings and floors
        await Shell.Current.GoToAsync("structureeditor");
    }

    private async void OnNavigateToStructureEditor(DropdownItemModel item)
    {
        if (item != null)
        {
            // Pass the building or level data as navigation parameters
            var parameters = new Dictionary<string, object>();
            
            if (CurrentActiveTab == "Structures")
            {
                // For structures, pass building info - use "name" to match QueryProperty
                parameters["name"] = item.Text;
                parameters["EditMode"] = "Building";
            }
            else if (CurrentActiveTab == "Levels")
            {
                // For levels, pass the building name to edit the building containing this level
                parameters["name"] = StructuresVM?.SelectedBuilding?.BuildingName ?? "";
                parameters["EditMode"] = "Level";
            }
            
            await Shell.Current.GoToAsync("structureeditor", parameters);
        }
        else
        {
            // Fallback to general structure editor
            await Shell.Current.GoToAsync("structureeditor");
        }
    }

    private async void OnRightSectionTapped()
    {
        await Shell.Current.GoToAsync("settings");
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
                var confirm = await ShowConfirmAsync(
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
                
                await ShowAlertAsync(
                    "Erfolg", 
                    $"Gebäude '{device.Text}' wurde erfolgreich gelöscht.", 
                    "OK");
                return;
            }
            if (CurrentActiveTab == "Levels")
            {
                if (string.IsNullOrWhiteSpace(SelectedBuildingName))
                {
                    await ShowAlertAsync("Info", "Wählen Sie zuerst ein Gebäude aus.", "OK");
                    return;
                }
                var confirm = await ShowConfirmAsync(
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
                        // Clear StructuresVM selection FIRST to immediately unbind UI from this floor
                        if (string.Equals(SelectedLevelName, device.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[OnDeleteDeviceFromDropdown] Clearing UI selection for floor '{floor.FloorName}' BEFORE deletion");
                            SelectedLevelName = null;
                            StructuresVM.SelectedLevel = null;
                        }
                        
                        // Clear all devices from the floor before deletion
                        Console.WriteLine($"[OnDeleteDeviceFromDropdown] Clearing {floor?.PlacedDevices?.Count ?? 0} devices from floor '{floor.FloorName}'");
                        floor.PlacedDevices?.Clear();
                        
                        // Delete PDF/PNG assets
                        await _pdfStorageService.DeleteFloorAssetsAsync(building, floor);
                        
                        // Remove floor from building
                        building.Floors.Remove(floor);
                        
                        Console.WriteLine($"[OnDeleteDeviceFromDropdown] Floor '{floor.FloorName}' and all its devices successfully deleted");
                    }
                    await _buildingStorage.SaveAsync(list);
                }
                
                // Force immediate UI refresh to hide deleted devices
                await StructuresVM.RefreshCurrentFloorPlanAsync();
                #pragma warning disable CS0618
                MessagingCenter.Send(this, "ForceDeviceLayoutRefresh");
                #pragma warning restore CS0618
                
                // Refresh Levels view
                await LoadLevelsAsync();
                
                await ShowAlertAsync(
                    "Erfolg", 
                    $"Stockwerk '{device.Text}' wurde erfolgreich gelöscht.", 
                    "OK");
                return;
            }

            // Default device deletion logic
            var confirmDelete = await ShowConfirmAsync(
                "Delete Device",
                $"Are you sure you want to delete the device '{device.Text.Split('\n')[0]}'?",
                "Delete",
                "Cancel");

            if (confirmDelete)
            {
                // NEW: Cascade remove any placed instances BEFORE removing from persistence/UI
                CascadeRemovePlacedDevice(device.Id);

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

                await ShowAlertAsync(
                    "Success",
                    "Deleted successfully.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync(
                "Error",
                $"Failed to delete: {ex.Message}",
                "OK");
        }
    }

    // Remove any PlacedDeviceModel referencing a deleted saved device
    private void CascadeRemovePlacedDevice(string deviceId)
    {
        try
        {
            if (StructuresVM?.SelectedLevel?.PlacedDevices == null || string.IsNullOrEmpty(deviceId))
                return;

            var toRemove = StructuresVM.SelectedLevel.PlacedDevices
                .Where(pd => pd.DeviceId == deviceId || (pd.DeviceInfo != null && pd.DeviceInfo.DeviceId == deviceId))
                .ToList();
            if (toRemove.Count == 0) return;

            foreach (var pd in toRemove)
            {
                StructuresVM.SelectedLevel.PlacedDevices.Remove(pd);
            }

#pragma warning disable CS0618
            MessagingCenter.Send(this, "ForceDeviceLayoutRefresh");
#pragma warning restore CS0618
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CascadeRemovePlacedDevice] Failed: {e.Message}");
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
                    await ShowAlertAsync("Info", "Select a building first.", "OK");
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

            var action = await ShowActionSheetAsync(
                $"Options for '{deviceName}'",
                "Cancel",
                null,
                options.ToArray());

            switch (action)
            {
                case "Edit Device":
                    await ShowAlertAsync(
                        "Edit Device",
                        "Edit functionality will be implemented later.",
                        "OK");
                    break;

                case "View Details":
                    var details = $"Device ID: {device.Id}\n" +
                                 $"Name: {deviceName}\n" +
                                 $"Info: {device.Text}\n" +
                                 $"Type: {(CurrentActiveTab == "WifiDev" ? "WiFi Device" : "Local Device")}";
                    
                    await ShowAlertAsync(
                        "Device Details",
                        details,
                        "OK");
                    break;

                case "Connection Settings":
                    await ShowAlertAsync(
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
            await ShowAlertAsync(
                "Error",
                $"Failed to show options: {ex.Message}",
                "OK");
        }
    }

    /// <summary>
    /// Handles direct tap on the settings (gear) button in a dropdown card.
    /// Structures / Levels: keep existing behavior (navigate to structure editor)
    /// WifiDev: navigate to SaveDevicePage ("savedevice") passing serialized network data (SSID only available)
    /// LocalDev: navigate to SaveLocalDevicePage ("savelocaldevice") passing local device info
    /// </summary>
    private async Task OnCardSettingsTapped(DropdownItemModel? item)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔧 [OnCardSettingsTapped] Called with item: {item?.Text}, CurrentActiveTab: {CurrentActiveTab}");
            
            if (item == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ [OnCardSettingsTapped] item is NULL!");
                return;
            }
            
            switch (CurrentActiveTab)
            {
                case "Structures":
                case "Levels":
                    System.Diagnostics.Debug.WriteLine($"🔧 [OnCardSettingsTapped] Navigating to structure editor");
                    OnNavigateToStructureEditor(item);
                    return;
                    
                case "WifiDev":
                    System.Diagnostics.Debug.WriteLine($"🔧 [OnCardSettingsTapped] Handling WifiDev case");
                    // Build minimal NetworkDataModel (mirrors WifiScan navigation)
                    var wifiNetwork = new NetworkDataModel
                    {
                        Ssid = item.NetworkInfo, // SSID stored in NetworkInfo
                        SsidName = item.Text,
                        IsAlreadySaved = true,
                        DeviceId = item.Id
                    };
                    var wifiJson = System.Text.Json.JsonSerializer.Serialize(wifiNetwork);
                    var encodedWifi = Uri.EscapeDataString(wifiJson);
                    System.Diagnostics.Debug.WriteLine($"📤 [OnCardSettingsTapped] WifiDev navigation: savedevice?networkData={encodedWifi}");
                    await Shell.Current.GoToAsync($"savedevice?networkData={encodedWifi}");
                    return;
                    
                case "LocalDev":
                    System.Diagnostics.Debug.WriteLine($"🔧 [OnCardSettingsTapped] Handling LocalDev case");
                    System.Diagnostics.Debug.WriteLine($"   Item.Id: '{item.Id}'");
                    System.Diagnostics.Debug.WriteLine($"   Item.Text: '{item.Text}'");
                    System.Diagnostics.Debug.WriteLine($"   Item.NetworkInfo: '{item.NetworkInfo}'");
                    
                    // SaveLocalDevicePageViewModel expects a query parameter named "deviceData" with
                    // a JSON payload matching record Payload(string ip, string name, string serial, string firmware, string deviceId)
                    var localPayload = new
                    {
                        ip = item.NetworkInfo ?? string.Empty,
                        name = item.Text ?? string.Empty,
                        serial = string.Empty,
                        firmware = string.Empty,
                        deviceId = item.Id ?? string.Empty
                    };
                    var localJson = System.Text.Json.JsonSerializer.Serialize(localPayload);
                    var encodedLocal = Uri.EscapeDataString(localJson);
                    
                    System.Diagnostics.Debug.WriteLine($"📦 [OnCardSettingsTapped] LocalDev payload: {localJson}");
                    System.Diagnostics.Debug.WriteLine($"📤 [OnCardSettingsTapped] LocalDev navigation: savelocaldevice?deviceData={encodedLocal}");
                    
                    await Shell.Current.GoToAsync($"savelocaldevice?deviceData={encodedLocal}");
                    System.Diagnostics.Debug.WriteLine($"✅ [OnCardSettingsTapped] Navigation completed");
                    return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [OnCardSettingsTapped] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            await ShowAlertAsync("Error", ex.Message, "OK");
        }
    }

    #region Device placement and scaling

    /// <summary>
    /// Updates all PlacedDevices across all buildings and floors when a device's info changes (e.g., name)
    /// </summary>
    private async Task UpdatePlacedDevicesWithDeviceInfo(DeviceModel updatedDevice)
    {
        try
        {
            Debug.WriteLine($"[UpdatePlacedDevicesWithDeviceInfo] Updating PlacedDevices for device: {updatedDevice.DeviceId} - {updatedDevice.Name}");
            
            // Load all buildings
            var buildings = await _buildingStorage.LoadAsync();
            bool anyChanges = false;
            
            foreach (var building in buildings)
            {
                foreach (var floor in building.Floors)
                {
                    if (floor.PlacedDevices == null) continue;
                    
                    foreach (var placedDevice in floor.PlacedDevices)
                    {
                        // Match by DeviceId
                        if (placedDevice.DeviceInfo?.DeviceId == updatedDevice.DeviceId ||
                            placedDevice.DeviceId == updatedDevice.DeviceId)
                        {
                            Debug.WriteLine($"[UpdatePlacedDevicesWithDeviceInfo] Found matching PlacedDevice on floor '{floor.FloorName}' - updating...");
                            
                            // Update the placed device with new info
                            placedDevice.UpdateFromDevice(updatedDevice);
                            anyChanges = true;
                            
                            Debug.WriteLine($"[UpdatePlacedDevicesWithDeviceInfo] Updated PlacedDevice name to: {placedDevice.Name}");
                        }
                    }
                }
            }
            
            if (anyChanges)
            {
                // Save changes back to storage
                await _buildingStorage.SaveAsync(buildings);
                Debug.WriteLine("[UpdatePlacedDevicesWithDeviceInfo] Changes saved to storage");
                
                // Refresh UI if currently viewing the affected floor
                if (StructuresVM?.SelectedLevel?.PlacedDevices != null)
                {
                    foreach (var pd in StructuresVM.SelectedLevel.PlacedDevices)
                    {
                        if (pd.DeviceInfo?.DeviceId == updatedDevice.DeviceId ||
                            pd.DeviceId == updatedDevice.DeviceId)
                        {
                            pd.UpdateFromDevice(updatedDevice);
                        }
                    }
                    
                    // Force UI refresh
                    #pragma warning disable CS0618
                    MessagingCenter.Send(this, "ForceDeviceLayoutRefresh");
                    #pragma warning restore CS0618
                }
            }
            else
            {
                Debug.WriteLine("[UpdatePlacedDevicesWithDeviceInfo] No matching PlacedDevices found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdatePlacedDevicesWithDeviceInfo] Error: {ex.Message}");
        }
    }

    private async Task AddDeviceToCurrentFloorAsync(DropdownItemModel? item)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] ENTRY: item.Id={item?.Id}, CurrentActiveTab={CurrentActiveTab}");
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] StructuresVM={StructuresVM != null}, SelectedBuilding={StructuresVM?.SelectedBuilding?.BuildingName}, SelectedLevel={StructuresVM?.SelectedLevel?.FloorName}");
            if (StructuresVM?.SelectedLevel?.PlacedDevices != null)
                System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] PlacedDevices count: {StructuresVM.SelectedLevel.PlacedDevices.Count}");

            if (StructuresVM == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: StructuresVM is null");
                await ShowAlertAsync("Error", "App state invalid (StructuresVM is null).", "OK");
                return;
            }
            if (StructuresVM.SelectedBuilding == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: SelectedBuilding is null");
                await ShowAlertAsync("Info", "Select a building first.", "OK");
                return;
            }
            if (StructuresVM.SelectedLevel == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: SelectedLevel is null");
                await ShowAlertAsync("Info", "Select a level first.", "OK");
                return;
            }
            var currentLevel = StructuresVM.SelectedLevel; // non-null here
            if (currentLevel.PlacedDevices == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] PlacedDevices is null! Initialisiere neu.");
                currentLevel.PlacedDevices = new ObservableCollection<PlacedDeviceModel>();
            }
            if (_viewport == null || !_viewport.IsPlanReady)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: Viewport not ready");
                await ShowAlertAsync("Info", "Floor plan not ready.", "OK");
                return;
            }

            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: No device selected");
                await ShowAlertAsync("Error", "No device selected.", "OK");
                return;
            }
            DeviceModel? source = null;

            // Load selected real device details based on current tab
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Loading device: {item.Id} from {CurrentActiveTab}");
            if (CurrentActiveTab == "WifiDev")
            {
                var saved = await _deviceService.GetSavedWifiDevicesAsync();
                System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Loaded {saved?.Count ?? 0} saved wifi devices");
                source = saved?.FirstOrDefault(d => d.DeviceId == item.Id);
            }
            else if (CurrentActiveTab == "LocalDev")
            {
                var saved = await _deviceService.GetSavedLocalDevicesAsync();
                System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Loaded {saved?.Count ?? 0} saved local devices");
                source = saved?.FirstOrDefault(d => d.DeviceId == item.Id);
            }

            if (source == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] ERROR: Device not found for id {item.Id}");
                await ShowAlertAsync("Error", "Device not found.", "OK");
                return;
            }

            var alreadyPlaced = currentLevel.PlacedDevices.Any(pd => pd.DeviceInfo?.DeviceId == source.DeviceId);
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] alreadyPlaced={alreadyPlaced}");
            if (alreadyPlaced)
            {
                System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Device already placed on floor: {source.DeviceId}");
                await ShowAlertAsync("Info", "This device is already placed on the selected floor.", "OK");
                var dropdownMatch = DropdownItems.FirstOrDefault(d => d.Id == source.DeviceId);
                if (dropdownMatch != null)
                {
                    dropdownMatch.IsPlacedOnCurrentFloor = true;
                    dropdownMatch.IsActionEnabled = false;
                }
                return;
            }

            var xNorm = 0.5;
            var yNorm = 0.5;
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Creating PlacedDeviceModel for {source.DeviceId}");
            var placed = new PlacedDeviceModel(source)
            {
                PlacedDeviceId = Guid.NewGuid().ToString("N"),
                XCenterNorm = xNorm,
                YCenterNorm = yNorm,
                BaseWidthNorm = 0.15,
                BaseHeightNorm = 0.18,
                Scale = 1.0,
                BuildingId = 0,
                FloorId = 0,
            };

            if (currentLevel == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] ERROR: SelectedLevel is null after device creation!");
                await ShowAlertAsync("Error", "SelectedLevel is null!", "OK");
                return;
            }
            if (currentLevel.PlacedDevices == null)
            {
                System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] PlacedDevices is null after device creation! Initialisiere neue Collection.");
                currentLevel.PlacedDevices = new System.Collections.ObjectModel.ObservableCollection<PlacedDeviceModel>();
            }

            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] >>> VOR Add: {placed?.DeviceId} zu Level: {currentLevel.FloorName}");
            currentLevel.PlacedDevices!.Add(placed!);
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] <<< NACH Add: {placed?.DeviceId} zu Level: {currentLevel.FloorName}");

            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] Calling PersistBuildingsAsync...");
            await PersistBuildingsAsync();
            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] PersistBuildingsAsync finished.");

            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] Calling RefreshCurrentFloorPlanAsync...");
            await StructuresVM.RefreshCurrentFloorPlanAsync();
            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] RefreshCurrentFloorPlanAsync finished.");

            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] Sending ForceDeviceLayoutRefresh via MessagingCenter...");
            #pragma warning disable CS0618
            MessagingCenter.Send(this, "ForceDeviceLayoutRefresh");
            #pragma warning restore CS0618

            var placedItem = DropdownItems.FirstOrDefault(d => d.Id == source.DeviceId);
            if (placedItem != null)
            {
                placedItem.IsPlacedOnCurrentFloor = true;
                placedItem.IsActionEnabled = false;
            }
            System.Diagnostics.Debug.WriteLine("[AddDeviceToCurrentFloorAsync] EXIT");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AddDeviceToCurrentFloorAsync] Exception: {ex}");
            await ShowAlertAsync("Error", ex.Message, "OK");
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
        System.Diagnostics.Debug.WriteLine("[PersistBuildingsAsync] ENTRY");
        var list = await _buildingStorage.LoadAsync();
        System.Diagnostics.Debug.WriteLine($"[PersistBuildingsAsync] Loaded {list.Count} buildings from storage");
        var b = list.FirstOrDefault(x => x.BuildingName.Equals(StructuresVM.SelectedBuilding?.BuildingName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        if (b != null)
        {
            System.Diagnostics.Debug.WriteLine($"[PersistBuildingsAsync] Found building: {b.BuildingName}");
            var f = b.Floors.FirstOrDefault(x => x.FloorName.Equals(StructuresVM.SelectedLevel?.FloorName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (f != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PersistBuildingsAsync] Found floor: {f.FloorName}, replacing PlacedDevices (count: {StructuresVM.SelectedLevel!.PlacedDevices.Count})");
                f.PlacedDevices = StructuresVM.SelectedLevel!.PlacedDevices;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PersistBuildingsAsync] Floor not found!");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PersistBuildingsAsync] Building not found!");
        }
        await _buildingStorage.SaveAsync(list);
        System.Diagnostics.Debug.WriteLine("[PersistBuildingsAsync] SaveAsync finished");
        System.Diagnostics.Debug.WriteLine("[PersistBuildingsAsync] EXIT");
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
            _wifiStatusTimer = null;
            
            // Use the SAME interval as SaveDevicePage (5 seconds) for consistency
            _wifiStatusTimer = new Timer(async _ => 
            {
                try 
                { 
                    await CheckWifiDeviceStatusAsync(); 
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error in WiFi status monitoring timer: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            System.Diagnostics.Debug.WriteLine("🔄 WiFi status monitoring started for MainPage dropdown (5s interval - same as SaveDevicePage)");
        }
    }

    private void StartLocalDevStatusMonitoring()
    {
        lock (_timerLock)
        {
            // Stop existing timer if any
            _wifiStatusTimer?.Dispose();
            _wifiStatusTimer = null;
            
            // Poll every 3 seconds via /intellidrive/version as requested
            _wifiStatusTimer = new Timer(async _ => 
            {
                try 
                { 
                    await CheckLocalDeviceStatusAsync(); 
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error in local device status monitoring timer: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            
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
                
                // Update the UI if status changed - use simpler approach to avoid deadlocks
                if (dropdownItem.IsConnected != isConnected)
                {
                    dropdownItem.IsConnected = isConnected;
                    System.Diagnostics.Debug.WriteLine($"📱 ✅ Updated {device.Name} status: {(isConnected ? "Connected" : "Disconnected")}");
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
                var (successStatus, _, _) = await _apiService.TestIntellidriveConnectionAsync(ipAddress);
                var isConnected = successStatus;
                
                // Update the UI if status changed - use simpler approach to avoid deadlocks
                if (dropdownItem.IsConnected != isConnected)
                {
                    dropdownItem.IsConnected = isConnected;
                    System.Diagnostics.Debug.WriteLine($"🏠 Updated local device {lines[0]} ({ipAddress}) status: {(isConnected ? "Connected" : "Disconnected")} (via /intellidrive/version)");
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
        #pragma warning disable CS0618
        MessagingCenter.Unsubscribe<LocalDevicesScanPageViewModel>(this, "LocalDeviceAdded");
        MessagingCenter.Unsubscribe<SaveLocalDevicePageViewModel, string>(this, "LocalDeviceAdded");
        MessagingCenter.Unsubscribe<SaveLocalDevicePageViewModel, DeviceModel>(this, "DeviceUpdated");
        MessagingCenter.Unsubscribe<SaveDevicePageViewModel, DeviceModel>(this, "DeviceUpdated");
        #pragma warning restore CS0618
        
        // Unsubscribe from StructuresVM PropertyChanged events
        if (StructuresVM != null)
        {
            StructuresVM.PropertyChanged -= OnStructuresVMPropertyChanged;
        }
    }

    #endregion

    #region Debug Methods

    private async Task DebugClearStorageAsync()
    {
        try
        {
            Debug.WriteLine("=== DEBUG: Starting storage clear operation ===");
            
            // First, let's see what's currently in storage
            Debug.WriteLine("=== Current storage content BEFORE clear ===");
            await _buildingStorage.LoadAsync(); // This will trigger our debug output
            
            // Show confirmation dialog
            bool confirm = await ShowConfirmAsync(
                "Debug: Clear Storage",
                "This will clear ALL saved buildings, floors, and devices. Continue?",
                "Yes, Clear All",
                "Cancel");
                
            if (confirm)
            {
                // Clear storage
                await _buildingStorage.ClearAllAsync();
                Debug.WriteLine("=== Storage cleared successfully ===");
                
                // Clear UI
                DropdownItems.Clear();
                SelectedBuildingName = null;
                SelectedLevelName = null;
                CurrentActiveTab = null;
                
                // Show success
                await ShowAlertAsync(
                    "Debug: Success",
                    "Storage cleared successfully! App state reset.",
                    "OK");
                    
                Debug.WriteLine("=== Storage clear operation completed ===");
            }
            else
            {
                Debug.WriteLine("=== Storage clear operation cancelled by user ===");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== ERROR in DebugClearStorageAsync: {ex.Message} ===");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            await ShowAlertAsync(
                "Debug: Error",
                $"Error clearing storage: {ex.Message}",
                "OK");
        }
    }

    #endregion

    #region UI helpers (alerts/action sheets)
    private Page? CurrentPage => Application.Current?.Windows?.FirstOrDefault()?.Page;

    private async Task ShowAlertAsync(string title, string message, string cancel)
    {
        try
        {
            var page = CurrentPage;
            if (page != null)
            {
                await page.DisplayAlert(title, message, cancel);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAlertAsync] No current page. Title='{title}' Message='{message}'");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowAlertAsync] Exception: {ex.Message}");
        }
    }

    private async Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
    {
        try
        {
            var page = CurrentPage;
            if (page != null)
            {
                return await page.DisplayAlert(title, message, accept, cancel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowConfirmAsync] Exception: {ex.Message}");
        }
        return false;
    }

    private async Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        try
        {
            var page = CurrentPage;
            if (page != null)
            {
                return await page.DisplayActionSheet(title, cancel, destruction, buttons);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowActionSheetAsync] Exception: {ex.Message}");
        }
        return null;
    }
    #endregion
}
