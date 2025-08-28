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
            if (string.IsNullOrWhiteSpace(json))
                return new List<Building>();

            var list = JsonSerializer.Deserialize<List<Building>>(json, JsonOpts) ?? new List<Building>();
            // Ensure non-null collections
            foreach (var b in list)
            {
                b.Floors ??= new();
                foreach (var f in b.Floors)
                {
                    f.PlacedDevices ??= new();
                }
            }
            return list;
        }
        catch
        {
            return new List<Building>();
        }
    }

    public async Task SaveAsync(IList<Building> buildings)
    {
        var json = JsonSerializer.Serialize(buildings, JsonOpts);
        await SecureStorage.SetAsync(StorageKey, json);
    }
}
