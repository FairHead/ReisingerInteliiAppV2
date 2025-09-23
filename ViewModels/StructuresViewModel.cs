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

    [ObservableProperty] private ObservableCollection<Building> buildings = new();
    [ObservableProperty] private Building? selectedBuilding;
    [ObservableProperty] private ObservableCollection<Floor> levels = new();
    [ObservableProperty] private Floor? selectedLevel;

    partial void OnLevelsChanged(ObservableCollection<Floor> value)
    {
        System.Diagnostics.Debug.WriteLine($"[StructuresViewModel] Levels changed: Count={value?.Count ?? 0}");
        if (value != null)
        {
            foreach (var floor in value)
            {
                System.Diagnostics.Debug.WriteLine($"[StructuresViewModel]   Floor: {floor.FloorName}, PlacedDevices.Count={floor.PlacedDevices.Count}");
            }
        }
    }

    partial void OnSelectedLevelChanged(Floor? value)
    {
        System.Diagnostics.Debug.WriteLine($"[StructuresViewModel] SelectedLevel changed: {value?.FloorName}, PlacedDevices.Count={value?.PlacedDevices.Count ?? -1}");
        // ...existing code from MVVM Toolkit generator...
        RecomputePlanState();
        // Notify bindings that depend on derived properties
        OnPropertyChanged(nameof(CurrentPdfPath));
        OnPropertyChanged(nameof(CurrentPngPath));
        OnPropertyChanged(nameof(HasPlan));
    }

    [ObservableProperty] private bool usePdfViewer;
    public string? CurrentPdfPath => SelectedLevel?.PdfPath;
    public string? CurrentPngPath => SelectedLevel?.PngPath;
    public bool HasPlan => (!string.IsNullOrWhiteSpace(CurrentPdfPath) && File.Exists(CurrentPdfPath))
                           || (!string.IsNullOrWhiteSpace(CurrentPngPath) && File.Exists(CurrentPngPath));

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddBuildingCommand { get; }
    public IAsyncRelayCommand<Building> EditBuildingCommand { get; }

    public StructuresViewModel(IBuildingStorageService storage, PdfStorageService pdfStorage)
    {
        _storage = storage;
        _pdfStorage = pdfStorage;
    RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        AddBuildingCommand = new AsyncRelayCommand(AddBuildingAsync);
        EditBuildingCommand = new AsyncRelayCommand<Building>(EditBuildingAsync);
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
        // Only auto-select first level if no level is currently selected
        if (SelectedLevel == null && Levels.Any())
        {
            SelectedLevel = Levels.FirstOrDefault();
        }
        RecomputePlanState();
    }


    private void RecomputePlanState()
    {
        // Until a real PDF viewer control is integrated, prefer the PNG fallback.
        UsePdfViewer = false;
    }

    public async Task RefreshCurrentFloorPlanAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] ENTRY: SelectedLevel={SelectedLevel?.FloorName}, SelectedBuilding={SelectedBuilding?.BuildingName}");
        if (SelectedLevel == null) {
            System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] EXIT: SelectedLevel is null");
            return;
        }
        var list = await _storage.LoadAsync();
        System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] Loaded {list.Count} buildings from storage");
        var currentBuilding = list.FirstOrDefault(x => x.BuildingName.Equals(SelectedBuilding?.BuildingName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        if (currentBuilding != null)
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] Found building: {currentBuilding.BuildingName}");
            var currentFloor = currentBuilding.Floors.FirstOrDefault(x => x.FloorName.Equals(SelectedLevel.FloorName, StringComparison.OrdinalIgnoreCase));
            if (currentFloor != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] Found floor: {currentFloor.FloorName}, updating SelectedLevel");
                var oldLevelName = SelectedLevel.FloorName;
                System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] Old PlacedDevices.Count={SelectedLevel.PlacedDevices.Count}, New PlacedDevices.Count={currentFloor.PlacedDevices.Count}");
                SelectedLevel = currentFloor;
                OnPropertyChanged(nameof(SelectedLevel));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] Floor not found!");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] Building not found!");
        }
        var changed = false;
        if (!string.IsNullOrWhiteSpace(SelectedLevel.PdfPath) && !File.Exists(SelectedLevel.PdfPath))
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] PDF path missing: {SelectedLevel.PdfPath}");
            SelectedLevel.PdfPath = null;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(SelectedLevel.PngPath) && !File.Exists(SelectedLevel.PngPath))
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshCurrentFloorPlanAsync] PNG path missing: {SelectedLevel.PngPath}");
            SelectedLevel.PngPath = null;
            changed = true;
        }
        if (changed)
        {
            System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] Persisting updated building/floor paths");
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
                System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] SaveAsync finished");
            }
        }
        OnPropertyChanged(nameof(CurrentPdfPath));
        OnPropertyChanged(nameof(CurrentPngPath));
        OnPropertyChanged(nameof(HasPlan));
        System.Diagnostics.Debug.WriteLine("[RefreshCurrentFloorPlanAsync] EXIT");
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
}
