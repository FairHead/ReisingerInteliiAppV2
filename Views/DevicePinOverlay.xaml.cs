using System.Collections.ObjectModel;
using System.Windows.Input;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Views;

public partial class DevicePinOverlay : ContentView
{
    public static readonly BindableProperty PlacedDevicesProperty = BindableProperty.Create(
        nameof(PlacedDevices), 
        typeof(ObservableCollection<PlacedDeviceModel>), 
        typeof(DevicePinOverlay),
        new ObservableCollection<PlacedDeviceModel>(),
        propertyChanged: OnPlacedDevicesChanged);

    public static readonly BindableProperty ScaleUpCommandProperty = BindableProperty.Create(
        nameof(ScaleUpCommand), 
        typeof(ICommand), 
        typeof(DevicePinOverlay));

    public static readonly BindableProperty ScaleDownCommandProperty = BindableProperty.Create(
        nameof(ScaleDownCommand), 
        typeof(ICommand), 
        typeof(DevicePinOverlay));

    public static readonly BindableProperty ToggleDoorCommandProperty = BindableProperty.Create(
        nameof(ToggleDoorCommand), 
        typeof(ICommand), 
        typeof(DevicePinOverlay));

    public static readonly BindableProperty UpdatePositionCommandProperty = BindableProperty.Create(
        nameof(UpdatePositionCommand), 
        typeof(ICommand), 
        typeof(DevicePinOverlay));

    public ObservableCollection<PlacedDeviceModel> PlacedDevices
    {
        get => (ObservableCollection<PlacedDeviceModel>)GetValue(PlacedDevicesProperty);
        set => SetValue(PlacedDevicesProperty, value);
    }

    public ICommand ScaleUpCommand
    {
        get => (ICommand)GetValue(ScaleUpCommandProperty);
        set => SetValue(ScaleUpCommandProperty, value);
    }

    public ICommand ScaleDownCommand
    {
        get => (ICommand)GetValue(ScaleDownCommandProperty);
        set => SetValue(ScaleDownCommandProperty, value);
    }

    public ICommand ToggleDoorCommand
    {
        get => (ICommand)GetValue(ToggleDoorCommandProperty);
        set => SetValue(ToggleDoorCommandProperty, value);
    }

    public ICommand UpdatePositionCommand
    {
        get => (ICommand)GetValue(UpdatePositionCommandProperty);
        set => SetValue(UpdatePositionCommandProperty, value);
    }

    public DevicePinOverlay()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private static void OnPlacedDevicesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinOverlay overlay && newValue is ObservableCollection<PlacedDeviceModel> devices)
        {
            // Update positions when devices change
            overlay.UpdatePinPositions();
        }
    }

    private void UpdatePinPositions()
    {
        // This will be called when device positions need to be updated
        // For now, this is a placeholder for more complex positioning logic
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not PlacedDeviceModel device)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Update visual position during drag
                border.TranslationX += e.TotalX;
                border.TranslationY += e.TotalY;
                break;

            case GestureStatus.Completed:
                // Calculate new relative position and update model
                var parent = border.Parent as VisualElement;
                if (parent != null)
                {
                    var newX = Math.Max(0, Math.Min(1, (border.X + border.TranslationX) / parent.Width));
                    var newY = Math.Max(0, Math.Min(1, (border.Y + border.TranslationY) / parent.Height));
                    
                    // Reset visual translation
                    border.TranslationX = 0;
                    border.TranslationY = 0;
                    
                    // Update model and persist
                    device.X = newX;
                    device.Y = newY;
                    
                    UpdatePositionCommand?.Execute(device);
                }
                break;
        }
    }
}