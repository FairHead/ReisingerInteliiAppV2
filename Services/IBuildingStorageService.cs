using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Services;

public interface IBuildingStorageService
{
    Task<IList<Building>> LoadAsync();
    Task SaveAsync(IList<Building> buildings);
    Task ClearAllAsync();
}
