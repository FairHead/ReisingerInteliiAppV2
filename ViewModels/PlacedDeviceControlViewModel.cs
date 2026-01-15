using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace ReisingerIntelliApp_V4.ViewModels;

/// <summary>
/// ViewModel for PlacedDeviceControl component - handles all device control commands
/// </summary>
public partial class PlacedDeviceControlViewModel : ObservableObject
{
    private const double ScaleMin = 0.01;
    private const double ScaleMax = 2.50;
    private const double ScaleStep = 0.05;

    [ObservableProperty]
    private PlacedDeviceModel? placedDevice;

    [ObservableProperty]
    private DeviceControlViewModel? controlViewModel;

    [ObservableProperty]
    private bool isInMoveMode;

    [ObservableProperty]
    private string selectedModeText = "Dauerauf";

    // ? Properties for UI bindings - forward from PlacedDevice
    public string Name => PlacedDevice?.Name ?? string.Empty;
    public string NetworkInfo => PlacedDevice?.NetworkInfo ?? string.Empty;

    // Events for parent (MainPage) to handle - maintains separation of concerns
    public event EventHandler<PlacedDeviceModel>? AddDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? RemoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ConfigureDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? DeleteDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? MoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ModeChangedRequested;
    public event EventHandler<bool>? PanInputBlockRequested;

    partial void OnIsInMoveModeChanged(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"?? IsInMoveMode changed: {value}");
        
        // ? CRITICAL FIX: Block/Unblock pan for ENTIRE duration of MoveMode
        // This prevents the race condition where horizontal pan gestures are detected
        // before the InteractivePressed command executes on individual buttons
        PanInputBlockRequested?.Invoke(this, value);
        System.Diagnostics.Debug.WriteLine($"?? Pan input block set to: {value} (MoveMode toggled)");
        
        OnPropertyChanged(nameof(IsInMoveMode));
    }

    partial void OnPlacedDeviceChanged(PlacedDeviceModel? value)
    {
        if (value != null)
        {
            System.Diagnostics.Debug.WriteLine($"?? PlacedDevice changed: {value.Name}");
            
            // Notify UI that Name and NetworkInfo changed
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(NetworkInfo));
            
            // Reset move mode when device changes (also releases pan block)
            IsInMoveMode = false;
            
            // Update ControlViewModel if it exists
            if (ControlViewModel != null && value.DeviceInfo != null)
            {
                ControlViewModel.SetDevice(value.DeviceInfo);
            }
        }
    }

    #region Movement Commands

    [RelayCommand]
    private void MoveUp()
    {
        if (PlacedDevice == null) return;
        
        var oldY = PlacedDevice.RelativeY;
        var moveStep = GetMovementStepSize(PlacedDevice.Scale);
        var newY = Math.Max(0.0, PlacedDevice.RelativeY - moveStep);
        
        System.Diagnostics.Debug.WriteLine($"?? MoveUp - Device: {PlacedDevice.Name}, Y: {oldY:F4} ? {newY:F4}");
        
        if (newY >= 0.0 && newY <= 1.0 && Math.Abs(PlacedDevice.RelativeY - newY) > 0.0001)
        {
            PlacedDevice.RelativeY = newY;
            MoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (PlacedDevice == null) return;
        
        var oldY = PlacedDevice.RelativeY;
        var moveStep = GetMovementStepSize(PlacedDevice.Scale);
        var newY = Math.Min(1.0, PlacedDevice.RelativeY + moveStep);
        
        System.Diagnostics.Debug.WriteLine($"?? MoveDown - Device: {PlacedDevice.Name}, Y: {oldY:F4} ? {newY:F4}");
        
        if (newY >= 0.0 && newY <= 1.0 && Math.Abs(PlacedDevice.RelativeY - newY) > 0.0001)
        {
            PlacedDevice.RelativeY = newY;
            MoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    [RelayCommand]
    private void MoveLeft()
    {
        if (PlacedDevice == null) return;
        
        var oldX = PlacedDevice.RelativeX;
        var moveStep = GetMovementStepSize(PlacedDevice.Scale);
        var newX = Math.Max(0.0, PlacedDevice.RelativeX - moveStep);
        
        System.Diagnostics.Debug.WriteLine($"?? MoveLeft - Device: {PlacedDevice.Name}, X: {oldX:F4} ? {newX:F4}");
        
        if (newX >= 0.0 && newX <= 1.0 && Math.Abs(PlacedDevice.RelativeX - newX) > 0.0001)
        {
            PlacedDevice.RelativeX = newX;
            MoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    [RelayCommand]
    private void MoveRight()
    {
        if (PlacedDevice == null) return;
        
        var oldX = PlacedDevice.RelativeX;
        var moveStep = GetMovementStepSize(PlacedDevice.Scale);
        var newX = Math.Min(1.0, PlacedDevice.RelativeX + moveStep);
        
        System.Diagnostics.Debug.WriteLine($"?? MoveRight - Device: {PlacedDevice.Name}, X: {oldX:F4} ? {newX:F4}");
        
        if (newX >= 0.0 && newX <= 1.0 && Math.Abs(PlacedDevice.RelativeX - newX) > 0.0001)
        {
            PlacedDevice.RelativeX = newX;
            MoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    #endregion

    #region Scale Commands

    [RelayCommand]
    private void ScalePlus()
    {
        if (PlacedDevice == null) return;
        
        var oldScale = PlacedDevice.Scale;
        var newScale = Math.Min(ScaleMax, Math.Round(PlacedDevice.Scale + ScaleStep, 3));
        
        System.Diagnostics.Debug.WriteLine($"? ScalePlus - Device: {PlacedDevice.Name}, Scale: {oldScale:F3} ? {newScale:F3}");
        
        if (Math.Abs(newScale - PlacedDevice.Scale) > 0.001)
        {
            PlacedDevice.Scale = newScale;
            AddDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    [RelayCommand]
    private void ScaleMinus()
    {
        if (PlacedDevice == null) return;
        
        var oldScale = PlacedDevice.Scale;
        var newScale = Math.Max(ScaleMin, Math.Round(PlacedDevice.Scale - ScaleStep, 3));
        
        System.Diagnostics.Debug.WriteLine($"? ScaleMinus - Device: {PlacedDevice.Name}, Scale: {oldScale:F3} ? {newScale:F3}");
        
        if (Math.Abs(newScale - PlacedDevice.Scale) > 0.001)
        {
            PlacedDevice.Scale = newScale;
            RemoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    #endregion

    #region Device Action Commands

    [RelayCommand]
    private void ToggleMoveMode()
    {
        IsInMoveMode = !IsInMoveMode;
        System.Diagnostics.Debug.WriteLine($"?? ToggleMoveMode - Device: {PlacedDevice?.Name}, MoveMode: {IsInMoveMode}");
        
        // Note: Pan block is already handled in OnIsInMoveModeChanged
        
        if (PlacedDevice != null)
        {
            MoveDeviceRequested?.Invoke(this, PlacedDevice);
        }
    }

    [RelayCommand]
    private void DeleteDevice()
    {
        if (PlacedDevice == null) return;
        
        System.Diagnostics.Debug.WriteLine($"??? DeleteDevice - Device: {PlacedDevice.Name}");
        DeleteDeviceRequested?.Invoke(this, PlacedDevice);
    }

    [RelayCommand]
    private void ConfigureDevice()
    {
        if (PlacedDevice == null) return;
        
        System.Diagnostics.Debug.WriteLine($"?? ConfigureDevice - Device: {PlacedDevice.Name}");
        ConfigureDeviceRequested?.Invoke(this, PlacedDevice);
    }

    [RelayCommand]
    private async Task OpenDeviceParametersAsync()
    {
        if (PlacedDevice == null) return;
        
        System.Diagnostics.Debug.WriteLine($"?? OpenDeviceParameters - Device: {PlacedDevice.Name}");
        
        try
        {
            // Serialize device data for navigation including auth credentials
            var deviceData = new
            {
                deviceId = PlacedDevice.DeviceId,
                name = PlacedDevice.Name,
                ip = PlacedDevice.DeviceInfo?.Ip ?? PlacedDevice.DeviceInfo?.IpAddress ?? string.Empty,
                ssid = PlacedDevice.DeviceInfo?.Ssid ?? string.Empty,
                username = PlacedDevice.DeviceInfo?.Username ?? string.Empty,
                password = PlacedDevice.DeviceInfo?.Password ?? string.Empty
            };
            
            var json = JsonSerializer.Serialize(deviceData);
            var encoded = Uri.EscapeDataString(json);
            
            await Shell.Current.GoToAsync($"deviceparameters?deviceData={encoded}");
            System.Diagnostics.Debug.WriteLine($"? Navigated to DeviceParametersPage for {PlacedDevice.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error navigating to DeviceParametersPage: {ex.Message}");
        }
    }

    #endregion

    #region Mode Commands

    [RelayCommand]
    private async Task OpenModeSelectorAsync()
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if (page == null) return;
        
        var options = new[] { "Dauerauf", "Lock/Unlock", "Einbahn", "Auto Halb", "Auto Ganz", "Winter" };
        var selection = await page.DisplayActionSheet("Modus wählen", "Abbrechen", null, options);
        
        if (string.IsNullOrEmpty(selection) || selection == "Abbrechen")
            return;
        
        SelectedModeText = selection;
    }

    [RelayCommand]
    private void ExecuteSelectedMode()
    {
        if (PlacedDevice == null) return;
        
        System.Diagnostics.Debug.WriteLine($"?? ExecuteSelectedMode - Mode: {SelectedModeText}");
        
        switch (SelectedModeText)
        {
            case "Dauerauf":
                PlacedDevice.DauerAuf = !PlacedDevice.DauerAuf;
                break;
            case "Lock/Unlock":
                PlacedDevice.IsLocked = !PlacedDevice.IsLocked;
                break;
            case "Einbahn":
                PlacedDevice.IsOneWay = !PlacedDevice.IsOneWay;
                break;
            case "Auto Halb":
                PlacedDevice.AutoMode = PlacedDeviceModel.AutoModeLevel.Half;
                break;
            case "Auto Ganz":
                PlacedDevice.AutoMode = PlacedDeviceModel.AutoModeLevel.Full;
                break;
            case "Winter":
                PlacedDevice.IsWinterMode = !PlacedDevice.IsWinterMode;
                break;
        }
        
        ModeChangedRequested?.Invoke(this, PlacedDevice);
    }

    #endregion

    #region Touch Event Commands

    [RelayCommand]
    private void InteractivePressed()
    {
        // ? Additional safety: reinforce pan block on any interactive press
        // This provides double-protection in case MoveMode block was somehow missed
        System.Diagnostics.Debug.WriteLine("[PlacedDeviceControl] Interactive Pressed -> Reinforce Pan Block");
        PanInputBlockRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void InteractiveReleased()
    {
        // ? Only unblock if NOT in MoveMode
        // MoveMode keeps pan blocked for entire duration
        if (!IsInMoveMode)
        {
            System.Diagnostics.Debug.WriteLine("[PlacedDeviceControl] Interactive Released -> Unblock Pan (not in MoveMode)");
            PanInputBlockRequested?.Invoke(this, false);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PlacedDeviceControl] Interactive Released -> Keep Pan Blocked (in MoveMode)");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculates movement step size based on device scale.
    /// Smaller devices get smaller movement steps for finer control.
    /// </summary>
    private double GetMovementStepSize(double deviceScale)
    {
        const double baseStepSize = 0.05;
        const double minStepSize = 0.005;
        const double maxStepSize = 0.05;
        
        var scaledStepSize = baseStepSize * Math.Max(0.1, deviceScale);
        var finalStepSize = Math.Clamp(scaledStepSize, minStepSize, maxStepSize);
        
        System.Diagnostics.Debug.WriteLine($"?? GetMovementStepSize - Scale: {deviceScale:F3}, Step: {finalStepSize:F4}");
        return finalStepSize;
    }

    #endregion
}
