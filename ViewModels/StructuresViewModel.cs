using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4.ViewModels;

public partial class StructuresViewModel : ObservableObject
{
    private readonly IBuildingStorageService _storage;
    private readonly PdfStorageService _pdfStorage;
    private readonly FloorPlanService _floorPlanService;
    private readonly IntellidriveApiService _apiService;

    [ObservableProperty] private ObservableCollection<Building> buildings = new();
    [ObservableProperty] private Building? selectedBuilding;
    [ObservableProperty] private ObservableCollection<Floor> levels = new();
    [ObservableProperty] private Floor? selectedLevel;
    [ObservableProperty] private ObservableCollection<PlacedDeviceModel> currentFloorPins = new();

    [ObservableProperty] private bool usePdfViewer;
    public string? CurrentPdfPath => SelectedLevel?.PdfPath;
    public string? CurrentPngPath => SelectedLevel?.PngPath;
    public bool HasPlan => (!string.IsNullOrWhiteSpace(CurrentPdfPath) && File.Exists(CurrentPdfPath))
                           || (!string.IsNullOrWhiteSpace(CurrentPngPath) && File.Exists(CurrentPngPath));

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddBuildingCommand { get; }
    public IAsyncRelayCommand<Building> EditBuildingCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> PinTappedCommand { get; }
    public IAsyncRelayCommand<object> PinDraggedCommand { get; }
    public IAsyncRelayCommand<PlacedDeviceModel> DeletePinCommand { get; }

    public StructuresViewModel(IBuildingStorageService storage, PdfStorageService pdfStorage, FloorPlanService floorPlanService, IntellidriveApiService apiService)
    {
        _storage = storage;
        _pdfStorage = pdfStorage;
        _floorPlanService = floorPlanService;
        _apiService = apiService;
    RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        AddBuildingCommand = new AsyncRelayCommand(AddBuildingAsync);
        EditBuildingCommand = new AsyncRelayCommand<Building>(EditBuildingAsync);
        PinTappedCommand = new AsyncRelayCommand<PlacedDeviceModel>(OnPinTappedAsync);
        PinDraggedCommand = new AsyncRelayCommand<object>(OnPinDraggedAsync);
        DeletePinCommand = new AsyncRelayCommand<PlacedDeviceModel>(OnDeletePinAsync);
    }

    public async Task LoadAsync(string? selectBuilding = null)
    {
        var list = await _storage.LoadAsync();
        Buildings = new ObservableCollection<Building>(list);

        if (!string.IsNullOrWhiteSpace(selectBuilding))
        {
            SelectedBuilding = Buildings.FirstOrDefault(b => b.BuildingName.Equals(selectBuilding, StringComparison.OrdinalIgnoreCase));
            if (SelectedBuilding != null)
            {
                Levels = new ObservableCollection<Floor>(SelectedBuilding.Floors);
            }
        }
    }

    partial void OnSelectedBuildingChanged(Building? value)
    {
        Levels = new ObservableCollection<Floor>(value?.Floors ?? new());
        // Reset selected level when building changes
        SelectedLevel = Levels.FirstOrDefault();
        RecomputePlanState();
    }

    partial void OnSelectedLevelChanged(Floor? value)
    {
        RecomputePlanState();
        // Notify bindings that depend on derived properties
        OnPropertyChanged(nameof(CurrentPdfPath));
        OnPropertyChanged(nameof(CurrentPngPath));
        OnPropertyChanged(nameof(HasPlan));
        
        // Load pins for the selected floor
        _ = LoadCurrentFloorPinsAsync();
    }

    private void RecomputePlanState()
    {
        // Until a real PDF viewer control is integrated, prefer the PNG fallback.
        UsePdfViewer = false;
    }

    public async Task RefreshCurrentFloorPlanAsync()
    {
        if (SelectedLevel == null) return;
        var changed = false;
        if (!string.IsNullOrWhiteSpace(SelectedLevel.PdfPath) && !File.Exists(SelectedLevel.PdfPath))
        {
            SelectedLevel.PdfPath = null;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(SelectedLevel.PngPath) && !File.Exists(SelectedLevel.PngPath))
        {
            SelectedLevel.PngPath = null;
            changed = true;
        }
        if (changed)
        {
            // Persist updated buildings
            var list = await _storage.LoadAsync();
            var b = list.FirstOrDefault(x => x.BuildingName.Equals(SelectedBuilding?.BuildingName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (b != null)
            {
                var f = b.Floors.FirstOrDefault(x => x.FloorName.Equals(SelectedLevel.FloorName, StringComparison.OrdinalIgnoreCase));
                if (f != null)
                {
                    f.PdfPath = SelectedLevel.PdfPath;
                    f.PngPath = SelectedLevel.PngPath;
                }
                await _storage.SaveAsync(list);
            }
        }
        OnPropertyChanged(nameof(CurrentPdfPath));
        OnPropertyChanged(nameof(CurrentPngPath));
        OnPropertyChanged(nameof(HasPlan));
    }

    private async Task AddBuildingAsync()
    {
        await Shell.Current.GoToAsync("structureeditor");
    }

    private async Task EditBuildingAsync(Building? building)
    {
        if (building == null) return;
        var route = $"structureeditor?name={Uri.EscapeDataString(building.BuildingName)}";
        await Shell.Current.GoToAsync(route);
    }

    #region Device Pin Management

    /// <summary>
    /// Loads pins for the currently selected floor
    /// </summary>
    private async Task LoadCurrentFloorPinsAsync()
    {
        if (SelectedBuilding == null || SelectedLevel == null)
        {
            CurrentFloorPins.Clear();
            return;
        }

        try
        {
            var pins = await _floorPlanService.GetFloorPinsAsync(SelectedBuilding.BuildingName, SelectedLevel.FloorName);
            CurrentFloorPins.Clear();
            foreach (var pin in pins)
            {
                CurrentFloorPins.Add(pin);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading floor pins: {ex.Message}");
            CurrentFloorPins.Clear();
        }
    }

    /// <summary>
    /// Adds a saved device pin to the current floor at the specified coordinates
    /// </summary>
    public async Task<bool> AddSavedDevicePinAsync(DeviceModel device, double relativeX, double relativeY)
    {
        if (SelectedBuilding == null || SelectedLevel == null) return false;

        try
        {
            var placedDevice = await _floorPlanService.AddDevicePinAsync(
                SelectedBuilding.BuildingName,
                SelectedLevel.FloorName,
                device,
                relativeX,
                relativeY);

            CurrentFloorPins.Add(placedDevice);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding saved device pin: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a local device pin to the current floor at the specified coordinates
    /// </summary>
    public async Task<bool> AddLocalDevicePinAsync(LocalNetworkDeviceModel device, double relativeX, double relativeY)
    {
        if (SelectedBuilding == null || SelectedLevel == null) return false;

        try
        {
            var placedDevice = await _floorPlanService.AddLocalDevicePinAsync(
                SelectedBuilding.BuildingName,
                SelectedLevel.FloorName,
                device,
                relativeX,
                relativeY);

            CurrentFloorPins.Add(placedDevice);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding local device pin: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handles pin tap - shows device action menu
    /// </summary>
    private async Task OnPinTappedAsync(PlacedDeviceModel? pin)
    {
        if (pin == null) return;

        try
        {
            var deviceName = pin.DeviceName;
            var actions = new List<string>
            {
                "Open Door",
                "Close Door",
                "Device Settings",
                "Resize Pin",
                "Delete Pin",
                "Cancel"
            };

            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"Device Actions - {deviceName}",
                "Cancel",
                null,
                actions.ToArray());

            switch (action)
            {
                case "Open Door":
                    await ExecuteDoorActionAsync(pin, "open");
                    break;
                case "Close Door":
                    await ExecuteDoorActionAsync(pin, "close");
                    break;
                case "Device Settings":
                    await OpenDeviceSettingsAsync(pin);
                    break;
                case "Resize Pin":
                    await ShowResizePinDialogAsync(pin);
                    break;
                case "Delete Pin":
                    await OnDeletePinAsync(pin);
                    break;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Action failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Handles pin drag completion - updates position
    /// </summary>
    private async Task OnPinDraggedAsync(object? dragData)
    {
        if (SelectedBuilding == null || SelectedLevel == null || dragData == null) return;

        try
        {
            // Extract drag data - expecting anonymous object with DeviceId, X, Y
            var data = dragData.GetType();
            var deviceId = data.GetProperty("DeviceId")?.GetValue(dragData)?.ToString();
            var x = (double)(data.GetProperty("X")?.GetValue(dragData) ?? 0.0);
            var y = (double)(data.GetProperty("Y")?.GetValue(dragData) ?? 0.0);

            if (string.IsNullOrEmpty(deviceId)) return;

            await _floorPlanService.UpdatePinPositionAsync(
                SelectedBuilding.BuildingName,
                SelectedLevel.FloorName,
                deviceId,
                x,
                y);

            // Update local model
            var pin = CurrentFloorPins.FirstOrDefault(p => p.DeviceId == deviceId);
            if (pin != null)
            {
                pin.X = x;
                pin.Y = y;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating pin position: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a device pin from the floor
    /// </summary>
    private async Task OnDeletePinAsync(PlacedDeviceModel? pin)
    {
        if (pin == null || SelectedBuilding == null || SelectedLevel == null) return;

        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Pin",
                $"Remove '{pin.DeviceName}' from this floor plan?",
                "Delete",
                "Cancel");

            if (!confirm) return;

            await _floorPlanService.RemovePinAsync(
                SelectedBuilding.BuildingName,
                SelectedLevel.FloorName,
                pin.DeviceId);

            CurrentFloorPins.Remove(pin);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Delete failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Executes door control actions
    /// </summary>
    private async Task ExecuteDoorActionAsync(PlacedDeviceModel pin, string action)
    {
        var device = pin.GetApiDevice();
        if (device == null)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Device not available for control", "OK");
            return;
        }

        try
        {
            string result = action.ToLowerInvariant() switch
            {
                "open" => await _apiService.OpenDoorAsync(device),
                "close" => await _apiService.CloseDoorAsync(device),
                _ => throw new ArgumentException($"Unknown action: {action}")
            };

            await Application.Current.MainPage.DisplayAlert("Success", $"Door {action} command sent", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Door {action} failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Opens device settings page
    /// </summary>
    private async Task OpenDeviceSettingsAsync(PlacedDeviceModel pin)
    {
        // This would navigate to the DeviceSettingsTabbedPage with the device
        // For now, show a placeholder message
        await Application.Current.MainPage.DisplayAlert(
            "Device Settings",
            $"Settings for {pin.DeviceName} will open here.\nThis connects to DeviceSettingsTabbedPage.",
            "OK");
    }

    /// <summary>
    /// Shows dialog to resize pin
    /// </summary>
    private async Task ShowResizePinDialogAsync(PlacedDeviceModel pin)
    {
        if (SelectedBuilding == null || SelectedLevel == null) return;

        var sizeOptions = new[] { "Small (24px)", "Medium (32px)", "Large (48px)", "Extra Large (64px)" };
        var currentSizeText = pin.Size switch
        {
            24 => "Small (24px)",
            32 => "Medium (32px)",
            48 => "Large (48px)",
            64 => "Extra Large (64px)",
            _ => "Medium (32px)"
        };

        var action = await Application.Current.MainPage.DisplayActionSheet(
            $"Resize Pin - {pin.DeviceName}",
            "Cancel",
            null,
            sizeOptions);

        if (action == "Cancel" || action == null) return;

        var newSize = action switch
        {
            "Small (24px)" => 24.0,
            "Medium (32px)" => 32.0,
            "Large (48px)" => 48.0,
            "Extra Large (64px)" => 64.0,
            _ => 32.0
        };

        try
        {
            await _floorPlanService.UpdatePinSizeAsync(
                SelectedBuilding.BuildingName,
                SelectedLevel.FloorName,
                pin.DeviceId,
                newSize);

            pin.Size = newSize;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Resize failed: {ex.Message}", "OK");
        }
    }

    #endregion
}
