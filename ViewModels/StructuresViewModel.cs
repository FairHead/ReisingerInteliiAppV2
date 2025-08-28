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

    [ObservableProperty] private ObservableCollection<Building> buildings = new();
    [ObservableProperty] private Building? selectedBuilding;
    [ObservableProperty] private ObservableCollection<Floor> levels = new();
    [ObservableProperty] private Floor? selectedLevel;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddBuildingCommand { get; }
    public IAsyncRelayCommand<Building> EditBuildingCommand { get; }

    public StructuresViewModel(IBuildingStorageService storage)
    {
        _storage = storage;
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
