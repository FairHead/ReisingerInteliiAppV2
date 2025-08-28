using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4.ViewModels;

[QueryProperty(nameof(Name), "name")]
public partial class StructureEditorViewModel : ObservableObject
{
    private readonly IBuildingStorageService _storage;
    private string? _originalName; // Tracks the name of the building when the editor was opened

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private ObservableCollection<Floor> floors = new();
    [ObservableProperty] private string newFloorName = string.Empty;

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand AddFloorCommand { get; }
    public IRelayCommand<Floor> RemoveFloorCommand { get; }

    public StructureEditorViewModel(IBuildingStorageService storage)
    {
        _storage = storage;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddFloorCommand = new RelayCommand(AddFloor);
        RemoveFloorCommand = new RelayCommand<Floor>(RemoveFloor);
    }

    public async Task InitializeAsync()
    {
        var list = await _storage.LoadAsync();
        if (!string.IsNullOrWhiteSpace(Name))
        {
            var existing = list.FirstOrDefault(b => b.BuildingName.Equals(Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Floors = new ObservableCollection<Floor>(existing.Floors);
            }
            // Remember which building is being edited so save can update (even if name changes)
            _originalName = Name;
        }
    }

    private void AddFloor()
    {
        var floorName = string.IsNullOrWhiteSpace(NewFloorName) ? $"Floor {Floors.Count + 1}" : NewFloorName.Trim();
        Floors.Add(new Floor { FloorName = floorName });
        NewFloorName = string.Empty;
    }

    private void RemoveFloor(Floor? floor)
    {
        if (floor == null) return;
        Floors.Remove(floor);
    }

    private async Task SaveAsync()
    {
        var trimmedName = Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            await Application.Current.MainPage.DisplayAlert("Validation", "Building name is required.", "OK");
            return;
        }

        var list = await _storage.LoadAsync();
        // Prefer to locate by original name (the building we opened for editing)
        Building? existing = null;
        if (!string.IsNullOrWhiteSpace(_originalName))
        {
            existing = list.FirstOrDefault(b => b.BuildingName.Equals(_originalName, StringComparison.OrdinalIgnoreCase));
        }
        // Fallback to current name (edit/new scenarios)
        existing ??= list.FirstOrDefault(b => b.BuildingName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            // If renaming, ensure no collision with another building
            var collision = list.FirstOrDefault(b => !ReferenceEquals(b, existing) && b.BuildingName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
            if (collision != null)
            {
                await Application.Current.MainPage.DisplayAlert("Validation", "A building with this name already exists.", "OK");
                return;
            }

            existing.BuildingName = trimmedName;
            existing.Floors = new ObservableCollection<Floor>(Floors);
        }
        else
        {
            // New building creation
            if (list.Any(b => b.BuildingName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                await Application.Current.MainPage.DisplayAlert("Validation", "A building with this name already exists.", "OK");
                return;
            }
            list.Add(new Building { BuildingName = trimmedName, Floors = new ObservableCollection<Floor>(Floors) });
        }

    await _storage.SaveAsync(list);

    // Notify listeners and navigate back
    MessagingCenter.Send(this, "BuildingSaved", trimmedName);
    await Shell.Current.GoToAsync("..");
    }
}
