using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Controls;

public partial class DevicePinOverlay : ContentView
{
    public static readonly BindableProperty PlacedDevicesProperty = BindableProperty.Create(
        nameof(PlacedDevices), typeof(ObservableCollection<PlacedDeviceModel>), typeof(DevicePinOverlay), 
        propertyChanged: OnPlacedDevicesChanged);

    public static readonly BindableProperty IsPlacementModeProperty = BindableProperty.Create(
        nameof(IsPlacementMode), typeof(bool), typeof(DevicePinOverlay), false,
        propertyChanged: OnPlacementModeChanged);

    public ObservableCollection<PlacedDeviceModel>? PlacedDevices
    {
        get => (ObservableCollection<PlacedDeviceModel>?)GetValue(PlacedDevicesProperty);
        set => SetValue(PlacedDevicesProperty, value);
    }

    public bool IsPlacementMode
    {
        get => (bool)GetValue(IsPlacementModeProperty);
        set => SetValue(IsPlacementModeProperty, value);
    }

    // Events for device interactions
    public event EventHandler<PlacedDeviceModel>? DoorControlRequested;
    public event EventHandler<PlacedDeviceModel>? SettingsRequested;
    public event EventHandler<PlacedDeviceModel>? DeviceDeleted;
    public event EventHandler<PlacedDeviceModel>? DevicePositionChanged;
    public event EventHandler<(double X, double Y)>? DevicePlacementRequested;

    private readonly Dictionary<PlacedDeviceModel, DevicePin> _pinMap = new();
    private DeviceModel? _pendingDevice; // Device waiting to be placed

    public DevicePinOverlay()
    {
        InitializeComponent();
        
        // Add tap gesture for placing devices
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnOverlayTapped;
        PinContainer.GestureRecognizers.Add(tapGesture);
    }

    private static void OnPlacedDevicesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinOverlay overlay)
        {
            if (oldValue is ObservableCollection<PlacedDeviceModel> oldCollection)
            {
                oldCollection.CollectionChanged -= overlay.OnPlacedDevicesCollectionChanged;
            }

            if (newValue is ObservableCollection<PlacedDeviceModel> newCollection)
            {
                newCollection.CollectionChanged += overlay.OnPlacedDevicesCollectionChanged;
                overlay.RefreshPins();
            }
        }
    }

    private static void OnPlacementModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinOverlay overlay)
        {
            overlay.PlacementHint.IsVisible = (bool)newValue;
        }
    }

    private void OnPlacedDevicesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshPins();
    }

    private void RefreshPins()
    {
        // Clear existing pins
        foreach (var pin in _pinMap.Values)
        {
            PinContainer.Children.Remove(pin);
        }
        _pinMap.Clear();

        // Add pins for all placed devices
        if (PlacedDevices != null)
        {
            foreach (var device in PlacedDevices)
            {
                AddPin(device);
            }
        }
    }

    private void AddPin(PlacedDeviceModel device)
    {
        var pin = new DevicePin
        {
            PlacedDevice = device
        };

        // Subscribe to pin events
        pin.DoorControlRequested += OnPinDoorControlRequested;
        pin.SettingsRequested += OnPinSettingsRequested;
        pin.ResizeRequested += OnPinResizeRequested;
        pin.DeleteRequested += OnPinDeleteRequested;
        pin.PositionChanged += OnPinPositionChanged;

        _pinMap[device] = pin;
        PinContainer.Children.Add(pin);
    }

    private void OnPinDoorControlRequested(object? sender, PlacedDeviceModel device)
    {
        DoorControlRequested?.Invoke(this, device);
    }

    private void OnPinSettingsRequested(object? sender, PlacedDeviceModel device)
    {
        SettingsRequested?.Invoke(this, device);
    }

    private void OnPinResizeRequested(object? sender, PlacedDeviceModel device)
    {
        // Show resize options
        ShowResizeOptions(device);
    }

    private void OnPinDeleteRequested(object? sender, PlacedDeviceModel device)
    {
        // Remove from collection (this will trigger refresh)
        PlacedDevices?.Remove(device);
        DeviceDeleted?.Invoke(this, device);
    }

    private void OnPinPositionChanged(object? sender, (PlacedDeviceModel Device, double DeltaX, double DeltaY) args)
    {
        DevicePositionChanged?.Invoke(this, args.Device);
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        if (!IsPlacementMode) return;

        // Get tap position relative to the overlay
        var position = e.GetPosition(this);
        if (position.HasValue)
        {
            DevicePlacementRequested?.Invoke(this, (position.Value.X, position.Value.Y));
        }
    }

    public void StartDevicePlacement(DeviceModel device)
    {
        _pendingDevice = device;
        IsPlacementMode = true;
    }

    public void CompleteDevicePlacement(double x, double y)
    {
        if (_pendingDevice == null) return;

        var placedDevice = new PlacedDeviceModel
        {
            DeviceId = _pendingDevice.DeviceId,
            DeviceName = _pendingDevice.Name,
            X = x,
            Y = y,
            Scale = 1.0,
            DeviceType = _pendingDevice.Type == AppDeviceType.WifiDevice ? DeviceType.WifiDevice : DeviceType.LocalDevice,
            DeviceIp = _pendingDevice.Ip,
            Username = _pendingDevice.Username,
            Password = _pendingDevice.Password,
            PlacedAt = DateTime.Now
        };

        PlacedDevices?.Add(placedDevice);
        
        _pendingDevice = null;
        IsPlacementMode = false;
    }

    private async void ShowResizeOptions(PlacedDeviceModel device)
    {
        var action = await Application.Current.MainPage.DisplayActionSheet(
            $"Resize {device.DeviceName}", 
            "Cancel", 
            null, 
            "Small (0.8x)", 
            "Normal (1.0x)", 
            "Large (1.2x)", 
            "Extra Large (1.5x)");

        if (action != null && action != "Cancel")
        {
            var newScale = action switch
            {
                "Small (0.8x)" => 0.8,
                "Normal (1.0x)" => 1.0,
                "Large (1.2x)" => 1.2,
                "Extra Large (1.5x)" => 1.5,
                _ => device.Scale
            };

            device.Scale = newScale;
            
            // Update the pin's scale
            if (_pinMap.TryGetValue(device, out var pin))
            {
                pin.PinBorder.Scale = newScale;
            }

            DevicePositionChanged?.Invoke(this, device);
        }
    }
}