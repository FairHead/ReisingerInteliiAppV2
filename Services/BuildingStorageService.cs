using System.Text.Json;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Services;

public class BuildingStorageService : IBuildingStorageService
{
    private const string StorageKey = "Buildings";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public async Task<IList<Building>> LoadAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(StorageKey);

            System.Diagnostics.Debug.WriteLine($"🏢 BuildingStorageService.LoadAsync - Raw JSON length: {json?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(json))
            {
                System.Diagnostics.Debug.WriteLine($"🏢 BuildingStorageService.LoadAsync - No data found, returning empty list");
                return new List<Building>();
            }

            var list = JsonSerializer.Deserialize<List<Building>>(json, JsonOpts) ?? new List<Building>();

            System.Diagnostics.Debug.WriteLine($"🏢 BuildingStorageService.LoadAsync - Loaded {list.Count} buildings");
            foreach (var building in list)
            {
                building.Floors ??= new();
                System.Diagnostics.Debug.WriteLine($"   📍 Building: {building.BuildingName} with {building.Floors.Count} floors");
                foreach (var floor in building.Floors)
                {
                    System.Diagnostics.Debug.WriteLine($"      🏠 Floor: {floor.FloorName} with {floor.PlacedDevices.Count} devices, PdfPath: {floor.PdfPath ?? "null"}");
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ BuildingStorageService.LoadAsync - Error: {ex.Message}");
            return new List<Building>();
        }
    }

    public async Task SaveAsync(IList<Building> buildings)
    {
        try
        {
            var json = JsonSerializer.Serialize(buildings, JsonOpts);
            System.Diagnostics.Debug.WriteLine($"🏢 BuildingStorageService.SaveAsync - Saving {buildings.Count} buildings, JSON length: {json.Length}");
            await SecureStorage.SetAsync(StorageKey, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ BuildingStorageService.SaveAsync - Error: {ex.Message}");
        }
    }

    // Method to completely clear all building data (for debugging/reset)
    public async Task ClearAllAsync()
    {
        try
        {
            await Task.Run(() => SecureStorage.Remove(StorageKey));
            System.Diagnostics.Debug.WriteLine($"🧹 BuildingStorageService.ClearAllAsync - All building data cleared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ BuildingStorageService.ClearAllAsync - Error: {ex.Message}");
        }
    }
}
