#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models; // core library DeviceModel
// API logic removed
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.ViewModels;

public partial class DeviceControlViewModel : ObservableObject
{
    // API logic removed: local simulation only

    public DeviceControlViewModel() { }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;
    // Removed envelope tracking
    [ObservableProperty] private bool isDoorOpen; // derived state for UI convenience
    [ObservableProperty] private bool doorActionInProgress; // disables toggle button while waiting

    public string ToggleGlyph => IsDoorOpen ? "C" : "O"; // example glyph mapping (converter still used in XAML)
    public string ToggleText => IsDoorOpen ? "close" : "open";
    partial void OnIsDoorOpenChanged(bool value){ OnPropertyChanged(nameof(ToggleGlyph)); OnPropertyChanged(nameof(ToggleText)); }

    public DeviceModel? Device { get; private set; }

    public void SetDevice(DeviceModel device) => Device = device;

    private async Task Run(Func<DeviceModel, CancellationToken, Task> op, string activity)
    {
        if (Device == null) { Error = "No device selected"; return; }
        if (IsBusy) return;
        try
        {
            IsBusy = true; Error = null;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await op(Device, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Debug.WriteLine($"❌ {activity} failed: {ex}");
        }
        finally { IsBusy = false; }
    }

    // Refresh disabled (no API)
    [RelayCommand]
    private Task RefreshStateAsync() => Task.CompletedTask;

    private void UpdateDoorStateFromEnvelope() { }

    [RelayCommand]
    private Task ToggleDoorAsync() => Run(async (d, ct) =>
    {
        Debug.WriteLine("▶️ ToggleDoorAsync invoked");
        if (DoorActionInProgress) return;
        DoorActionInProgress = true;
        try
        {
            // 1. Always fetch current state to base decision on real device, not cached flag
            bool currentlyOpen = IsDoorOpen;
            bool intendToOpen = !currentlyOpen; // toggle
            // Simulate instant toggle
            await Task.Delay(150, ct);
            IsDoorOpen = intendToOpen;
        }
        finally
        {
            DoorActionInProgress = false;
            Debug.WriteLine("⏹️ ToggleDoorAsync finished");
        }
    }, "ToggleDoor");

    // Command helpers removed

    private Task Command(string name, Func<DeviceModel, CancellationToken, Task> op)
        => Run(async (d, ct) => { await op(d, ct).ConfigureAwait(false); }, name);
}
