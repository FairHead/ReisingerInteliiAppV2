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
    }

    private void RecomputePlanState()
    {
        // Until a real PDF viewer control is integrated, prefer the PNG fallback.
        UsePdfViewer = false;
    }

    public async Task RefreshCurrentFloorPlanAsync()
    {
        if (SelectedLevel == null) return;
        
        // Reload the building and floor data to get updated PlacedDevices
        var list = await _storage.LoadAsync();
        var currentBuilding = list.FirstOrDefault(x => x.BuildingName.Equals(SelectedBuilding?.BuildingName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        if (currentBuilding != null)
        {
            var currentFloor = currentBuilding.Floors.FirstOrDefault(x => x.FloorName.Equals(SelectedLevel.FloorName, StringComparison.OrdinalIgnoreCase));
            if (currentFloor != null)
            {
                // Force complete refresh by setting SelectedLevel to the fresh data
                var oldLevelName = SelectedLevel.FloorName;
                SelectedLevel = currentFloor;
                OnPropertyChanged(nameof(SelectedLevel));
            }
        }
        
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
}
