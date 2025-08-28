using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

public class Building
{
    public string BuildingName { get; set; } = string.Empty;
    public ObservableCollection<Floor> Floors { get; set; } = new();
}
