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

    public Floor()
    {
        // Attach debug handler to PlacedDevices collection
        placedDevices.CollectionChanged += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[Floor] PlacedDevices.CollectionChanged: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, OldItems={e.OldItems?.Count ?? 0}, TotalCount={placedDevices.Count}");
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is PlacedDeviceModel pd)
                        System.Diagnostics.Debug.WriteLine($"[Floor]   Added: {pd.DeviceInfo?.DeviceId} / {pd.DeviceInfo?.Name}");
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is PlacedDeviceModel pd)
                        System.Diagnostics.Debug.WriteLine($"[Floor]   Removed: {pd.DeviceInfo?.DeviceId} / {pd.DeviceInfo?.Name}");
                }
            }
        };
    }

    // Optional viewport persistence per floor (PanPinchContainer state)
    [ObservableProperty]
    private double? viewScale;

    [ObservableProperty]
    private double? viewTranslationX;

    [ObservableProperty]
    private double? viewTranslationY;
}
