using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ReisingerIntelliApp_V4.Models;

public partial class Floor : ObservableObject
{
    [ObservableProperty]
    private string floorName = string.Empty;
    
    [ObservableProperty]
    private string? pdfPath = null;
    
    [ObservableProperty]
    private string? pngPath = null;

    // Placed devices for this floor (persisted). Keep as collection for per-floor separation.
    [ObservableProperty]
    private ObservableCollection<PlacedDeviceModel> placedDevices = new();

    // Optional viewport persistence per floor (PanPinchContainer state)
    [ObservableProperty]
    private double? viewScale;

    [ObservableProperty]
    private double? viewTranslationX;

    [ObservableProperty]
    private double? viewTranslationY;
}
