using ReisingerIntelliApp_V4.Models;
using System.Windows.Input;

namespace ReisingerIntelliApp_V4.Controls;

public partial class DevicePinWidget : ContentView
{
    public static readonly BindableProperty PinTappedCommandProperty =
        BindableProperty.Create(nameof(PinTappedCommand), typeof(ICommand), typeof(DevicePinWidget));

    public static readonly BindableProperty PlacedDeviceProperty =
        BindableProperty.Create(nameof(PlacedDevice), typeof(PlacedDeviceModel), typeof(DevicePinWidget),
            propertyChanged: OnPlacedDeviceChanged);

    public DevicePinWidget()
    {
        InitializeComponent();
    }

    public ICommand? PinTappedCommand
    {
        get => (ICommand?)GetValue(PinTappedCommandProperty);
        set => SetValue(PinTappedCommandProperty, value);
    }

    public PlacedDeviceModel? PlacedDevice
    {
        get => (PlacedDeviceModel?)GetValue(PlacedDeviceProperty);
        set => SetValue(PlacedDeviceProperty, value);
    }

    private static void OnPlacedDeviceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinWidget widget && newValue is PlacedDeviceModel placedDevice)
        {
            widget.BindingContext = placedDevice;
        }
    }

    private void OnPinTapped(object? sender, TappedEventArgs e)
    {
        if (PinTappedCommand?.CanExecute(PlacedDevice) == true)
        {
            PinTappedCommand.Execute(PlacedDevice);
        }
    }
}