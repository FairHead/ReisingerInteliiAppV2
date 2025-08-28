using CommunityToolkit.Mvvm.ComponentModel;

namespace ReisingerIntelliApp_V4.Models;

public class TabItemModel
{
    public string TabName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<DropdownItemModel> Items { get; set; } = new();
}

public partial class DropdownItemModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string SubText { get; set; } = string.Empty;
    public bool HasActions { get; set; }
    
    // Controls whether the connection status UI is shown for this item
    [ObservableProperty]
    private bool showStatus = false;

    // Selection state for visual highlighting in Structures/Levels (and reusable elsewhere)
    [ObservableProperty]
    private bool isSelected = false;
    
    [ObservableProperty]
    private bool isConnected = false;
    
    // Computed properties that automatically notify when IsConnected changes
    public string ConnectionStatus => IsConnected ? "Connected" : "Disconnected";
    public Color ConnectionColor => IsConnected ? Colors.Green : Colors.Red;
    
    // Override the partial method to notify computed properties when IsConnected changes
    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionStatus));
        OnPropertyChanged(nameof(ConnectionColor));
    }
}
