#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models; // core library DeviceModel
using ReisingerIntelliApp_V4.Services; // For IntellidriveApiService
using ReisingerIntelliApp_V4.Helpers; // For ServiceHelper
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.ViewModels;

public partial class DeviceControlViewModel : ObservableObject
{
    // API logic removed: local simulation only

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
            // 1. Fetch current state from real device
            var apiService = ServiceHelper.GetService<IntellidriveApiService>();
            if (apiService == null)
            {
                Debug.WriteLine("❌ IntellidriveApiService not available!");
                Error = "API Service not available";
                return;
            }

            if (Device == null)
            {
                Debug.WriteLine("❌ Device is NULL!");
                Error = "No device selected";
                return;
            }

            if (string.IsNullOrWhiteSpace(Device.Username) || string.IsNullOrWhiteSpace(Device.Password))
            {
                Debug.WriteLine("❌ Device credentials missing!");
                Error = "Zugangsdaten fehlen";
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current!.Windows[0].Page!.DisplayAlert(
                        "Zugangsdaten fehlen",
                        $"Für '{Device.Name}' sind kein Benutzername/Passwort hinterlegt.\n\nBitte unter Geräte-Einstellungen die Zugangsdaten eingeben.",
                        "OK");
                });
                return;
            }

            Debug.WriteLine($"🔄 Fetching current door state for device: {Device.Name} ({Device.Ip})");
            
            // Get current state from device
            var stateResponse = await apiService.GetDoorStateAsync(Device, ct);
            Debug.WriteLine($"📥 Door state response: {stateResponse}");
            
            // Parse state to determine if door is open
            // Expected response format: {"Success":true,"Message":"open"} or {"Success":true,"Message":"closed"}
            bool currentlyOpen = stateResponse?.Contains("open", StringComparison.OrdinalIgnoreCase) == true;
            Debug.WriteLine($"🚪 Current door state: {(currentlyOpen ? "OPEN" : "CLOSED")}");
            
            bool intendToOpen = !currentlyOpen; // toggle
            Debug.WriteLine($"🎯 Intended action: {(intendToOpen ? "OPEN" : "CLOSE")} door");

            // 2. Send appropriate command to device
            string response;
            if (intendToOpen)
            {
                Debug.WriteLine("📤 Sending OPEN command to device...");
                response = await apiService.OpenDoorAsync(Device, ct);
                Debug.WriteLine($"📥 OPEN response: {response}");
            }
            else
            {
                Debug.WriteLine("📤 Sending CLOSE command to device...");
                response = await apiService.CloseDoorAsync(Device, ct);
                Debug.WriteLine($"📥 CLOSE response: {response}");
            }

            // 3. Update UI state based on intended action
            IsDoorOpen = intendToOpen;
            Debug.WriteLine($"✅ Door state updated in UI: {(IsDoorOpen ? "OPEN" : "CLOSED")}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ ToggleDoorAsync error: {ex.Message}");
            Error = $"Door control failed: {ex.Message}";
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
