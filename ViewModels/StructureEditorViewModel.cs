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
    private readonly PdfStorageService _pdfStorage;
    private string? _originalName; // Tracks the name of the building when the editor was opened

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private ObservableCollection<Floor> floors = new();
    [ObservableProperty] private string newFloorName = string.Empty;

    // Add computed property for UI binding
    public string Title => string.IsNullOrEmpty(Name) ? "New Building" : $"Edit Building: {Name}";

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand AddFloorCommand { get; }
    public IRelayCommand<Floor> RemoveFloorCommand { get; }
    public IAsyncRelayCommand<Floor> UploadPdfCommand { get; }
    public IAsyncRelayCommand<Floor> DeletePdfCommand { get; }
    public IAsyncRelayCommand BackToMainPageCommand { get; }

    public StructureEditorViewModel(IBuildingStorageService storage, PdfStorageService pdfStorage)
    {
        _storage = storage;
        _pdfStorage = pdfStorage;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddFloorCommand = new RelayCommand(AddFloor);
        RemoveFloorCommand = new RelayCommand<Floor>(RemoveFloor);
        UploadPdfCommand = new AsyncRelayCommand<Floor>(UploadPdfAsync);
        DeletePdfCommand = new AsyncRelayCommand<Floor>(DeletePdfAsync);
        BackToMainPageCommand = new AsyncRelayCommand(BackToMainPageAsync);
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
        
        // Notify UI about title change
        OnPropertyChanged(nameof(Title));
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

    private async Task UploadPdfAsync(Floor? floor)
    {
        if (floor == null) return;
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select floor plan PDF",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } },
                })
            });
            if (result == null) return;

            var imported = await _pdfStorage.ImportPdfAsync(new Building { BuildingName = Name }, floor, result, generatePreviewPng: true);
            floor.PdfPath = imported.pdfPath;
            floor.PngPath = imported.pngPath;

            // Debug output to verify PDF path is set
            System.Diagnostics.Debug.WriteLine($"? PDF uploaded for floor '{floor.FloorName}': {floor.PdfPath}");

            await PersistAsync(closeAndNotify: false);
            MessagingCenter.Send(this, "FloorPlanChanged", (Name, floor.FloorName));
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Upload failed: {ex.Message}", "OK");
        }
    }

    private async Task DeletePdfAsync(Floor? floor)
    {
        if (floor == null) return;
        var confirm = await Application.Current.MainPage.DisplayAlert("Delete Plan", $"Delete plan for '{floor.FloorName}'?", "Delete", "Cancel");
        if (!confirm) return;
        try
        {
            await _pdfStorage.DeleteFloorAssetsAsync(new Building { BuildingName = Name }, floor);
            floor.PdfPath = null;
            floor.PngPath = null;

            // Debug output to verify PDF path is cleared
            System.Diagnostics.Debug.WriteLine($"? PDF deleted for floor '{floor.FloorName}'");

            await PersistAsync(closeAndNotify: false);
            MessagingCenter.Send(this, "FloorPlanChanged", (Name, floor.FloorName));
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Delete failed: {ex.Message}", "OK");
        }
    }

    private async Task SaveAsync()
    {
        await PersistAsync(closeAndNotify: true);
    }

    private async Task BackToMainPageAsync()
    {
        // Navigate directly to the root MainPage to ensure all dropdowns are closed
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async Task PersistAsync(bool closeAndNotify)
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

    if (closeAndNotify)
    {
        // Notify listeners and navigate back
        MessagingCenter.Send(this, "BuildingSaved", trimmedName);
        await Shell.Current.GoToAsync("..");
    }
    }
}
