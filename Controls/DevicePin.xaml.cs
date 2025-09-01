using Microsoft.Maui.Controls;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Controls;

public partial class DevicePin : ContentView
{
    public static readonly BindableProperty PlacedDeviceProperty = BindableProperty.Create(
        nameof(PlacedDevice), typeof(PlacedDeviceModel), typeof(DevicePin), propertyChanged: OnPlacedDeviceChanged);

    public static readonly BindableProperty DeviceIconSourceProperty = BindableProperty.Create(
        nameof(DeviceIconSource), typeof(string), typeof(DevicePin), "device_icon.svg");

    public PlacedDeviceModel? PlacedDevice
    {
        get => (PlacedDeviceModel?)GetValue(PlacedDeviceProperty);
        set => SetValue(PlacedDeviceProperty, value);
    }

    public string DeviceIconSource
    {
        get => (string)GetValue(DeviceIconSourceProperty);
        set => SetValue(DeviceIconSourceProperty, value);
    }

    // Events for pin interactions
    public event EventHandler<PlacedDeviceModel>? DoorControlRequested;
    public event EventHandler<PlacedDeviceModel>? SettingsRequested;
    public event EventHandler<PlacedDeviceModel>? ResizeRequested;
    public event EventHandler<PlacedDeviceModel>? DeleteRequested;
    public event EventHandler<(PlacedDeviceModel Device, double DeltaX, double DeltaY)>? PositionChanged;

    private bool _isDragging;
    private double _startX, _startY;

    public DevicePin()
    {
        InitializeComponent();
    }

    private static void OnPlacedDeviceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePin pin && newValue is PlacedDeviceModel device)
        {
            // Update visual properties based on device
            pin.DeviceIconSource = device.DeviceType == DeviceType.WifiDevice ? "wifi_icon.svg" : "local_icon.svg";
            
            // Apply scale
            pin.PinBorder.Scale = device.Scale;
            
            // Position the pin
            pin.TranslationX = device.X;
            pin.TranslationY = device.Y;
        }
    }

    private void OnPinTapped(object? sender, TappedEventArgs e)
    {
        // Toggle action panel visibility
        ActionPanel.IsVisible = !ActionPanel.IsVisible;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (PlacedDevice == null) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = true;
                _startX = TranslationX;
                _startY = TranslationY;
                // Hide action panel during drag
                ActionPanel.IsVisible = false;
                // Bring to front during drag
                ZIndex = 1000;
                break;

            case GestureStatus.Running:
                if (_isDragging)
                {
                    TranslationX = _startX + e.TotalX;
                    TranslationY = _startY + e.TotalY;
                }
                break;

            case GestureStatus.Completed:
                if (_isDragging)
                {
                    _isDragging = false;
                    ZIndex = 0;
                    
                    // Update the model with new position
                    var deltaX = TranslationX - PlacedDevice.X;
                    var deltaY = TranslationY - PlacedDevice.Y;
                    
                    PlacedDevice.X = TranslationX;
                    PlacedDevice.Y = TranslationY;
                    
                    // Notify parent of position change
                    PositionChanged?.Invoke(this, (PlacedDevice, deltaX, deltaY));
                }
                break;

            case GestureStatus.Canceled:
                if (_isDragging)
                {
                    _isDragging = false;
                    ZIndex = 0;
                    // Restore original position
                    TranslationX = _startX;
                    TranslationY = _startY;
                }
                break;
        }
    }

    private void OnDoorControlTapped(object? sender, TappedEventArgs e)
    {
        if (PlacedDevice != null)
        {
            DoorControlRequested?.Invoke(this, PlacedDevice);
        }
        ActionPanel.IsVisible = false;
    }

    private void OnSettingsTapped(object? sender, TappedEventArgs e)
    {
        if (PlacedDevice != null)
        {
            SettingsRequested?.Invoke(this, PlacedDevice);
        }
        ActionPanel.IsVisible = false;
    }

    private void OnResizeTapped(object? sender, TappedEventArgs e)
    {
        if (PlacedDevice != null)
        {
            ResizeRequested?.Invoke(this, PlacedDevice);
        }
        ActionPanel.IsVisible = false;
    }

    private void OnDeleteTapped(object? sender, TappedEventArgs e)
    {
        if (PlacedDevice != null)
        {
            DeleteRequested?.Invoke(this, PlacedDevice);
        }
        ActionPanel.IsVisible = false;
    }
}