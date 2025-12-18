using ReisingerIntelliApp_V4.Models;
using System;
using ReisingerIntelliApp_V4.ViewModels;
using ReisingerIntelliApp_V4.Helpers;

namespace ReisingerIntelliApp_V4.Components;

public partial class PlacedDeviceControl : ContentView
{
    private PlacedDeviceControlViewModel? _viewModel;

    public PlacedDeviceControl()
    {
        InitializeComponent();
        
        // Initialize ViewModel from DI
        _viewModel = ServiceHelper.GetService<PlacedDeviceControlViewModel>();
        BindingContext = _viewModel;
        
        // Wire up events from ViewModel to expose to parent
        if (_viewModel != null)
        {
            _viewModel.AddDeviceRequested += (s, e) => AddDeviceRequested?.Invoke(this, e);
            _viewModel.RemoveDeviceRequested += (s, e) => RemoveDeviceRequested?.Invoke(this, e);
            _viewModel.ConfigureDeviceRequested += (s, e) => ConfigureDeviceRequested?.Invoke(this, e);
            _viewModel.DeleteDeviceRequested += (s, e) => DeleteDeviceRequested?.Invoke(this, e);
            _viewModel.MoveDeviceRequested += (s, e) => MoveDeviceRequested?.Invoke(this, e);
            _viewModel.ModeChangedRequested += (s, e) => ModeChangedRequested?.Invoke(this, e);
            _viewModel.PanInputBlockRequested += (s, e) => 
            {
#pragma warning disable CS0618
                MessagingCenter.Send(this, "PanInputBlock", e);
                MessagingCenter.Send(this, "GlobalMoveMode", e);
#pragma warning restore CS0618
            };
        }
    }

    public static readonly BindableProperty PlacedDeviceProperty =
        BindableProperty.Create(nameof(PlacedDevice), typeof(PlacedDeviceModel), typeof(PlacedDeviceControl), null, propertyChanged: OnPlacedDeviceChanged);

    public PlacedDeviceModel PlacedDevice
    {
        get => (PlacedDeviceModel)GetValue(PlacedDeviceProperty);
        set => SetValue(PlacedDeviceProperty, value);
    }

    public static readonly BindableProperty ControlViewModelProperty =
        BindableProperty.Create(
            nameof(ControlViewModel),
            typeof(DeviceControlViewModel),
            typeof(PlacedDeviceControl),
            default(DeviceControlViewModel),
            propertyChanged: OnControlViewModelChanged);

    public DeviceControlViewModel? ControlViewModel
    {
        get => (DeviceControlViewModel?)GetValue(ControlViewModelProperty);
        set => SetValue(ControlViewModelProperty, value);
    }

    // Events for parent (MainPage) to subscribe to
    public event EventHandler<PlacedDeviceModel>? AddDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? RemoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ConfigureDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? DeleteDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? MoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ToggleDoorRequested;
    public event EventHandler<PlacedDeviceModel>? ModeChangedRequested;

    private static void OnPlacedDeviceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlacedDeviceControl control && newValue is PlacedDeviceModel placedDevice)
        {
            Console.WriteLine($"🔄 PlacedDeviceControl.OnPlacedDeviceChanged - Device: {placedDevice.Name}");
            
            // Update ViewModel's PlacedDevice
            if (control._viewModel != null)
            {
                control._viewModel.PlacedDevice = placedDevice;
            }
            
            // Update ControlViewModel if it exists
            if (control.ControlViewModel != null && placedDevice.DeviceInfo != null)
            {
                control.ControlViewModel.SetDevice(placedDevice.DeviceInfo);
                control.WireDoorStateSync(control.ControlViewModel);
            }
            else if (control.ControlViewModel == null)
            {
                // Lazy resolve from DI if available
                try
                {
                    var vm = ServiceHelper.GetService<DeviceControlViewModel>();
                    if (vm != null)
                    {
                        control.ControlViewModel = vm;
                        if (placedDevice.DeviceInfo != null)
                        {
                            vm.SetDevice(placedDevice.DeviceInfo);
                            control.WireDoorStateSync(vm);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to resolve DeviceControlViewModel: {ex.Message}");
                }
            }
        }
    }

    private static void OnControlViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlacedDeviceControl control && newValue is DeviceControlViewModel vm)
        {
            // Update ViewModel's ControlViewModel
            if (control._viewModel != null)
            {
                control._viewModel.ControlViewModel = vm;
            }
            
            // Ensure the VM has the device reference
            if (control.PlacedDevice?.DeviceInfo != null)
            {
                vm.SetDevice(control.PlacedDevice.DeviceInfo);
                control.WireDoorStateSync(vm);
            }
        }
    }

    private void WireDoorStateSync(DeviceControlViewModel vm)
    {
        vm.PropertyChanged -= VmOnPropertyChanged;
        vm.PropertyChanged += VmOnPropertyChanged;
        if (PlacedDevice != null)
        {
            PlacedDevice.IsDoorOpen = vm.IsDoorOpen;
        }
    }

    private void VmOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is DeviceControlViewModel vm && e.PropertyName == nameof(DeviceControlViewModel.IsDoorOpen))
        {
            if (PlacedDevice != null)
            {
                PlacedDevice.IsDoorOpen = vm.IsDoorOpen;
            }
        }
    }

    // Keep event handlers for Loaded events (these are for debugging/logging, not business logic)
    private void OnControlLoaded(object sender, EventArgs e)
    {
        Console.WriteLine("[PlacedDeviceControl] Loaded - wiring visibility observers");
        try
        {
            var container = this.FindByName<AbsoluteLayout>("ArrowButtonsContainer");
            if (container != null)
            {
                Console.WriteLine($"[PlacedDeviceControl] ArrowButtonsContainer initial IsVisible={container.IsVisible}");
                container.PropertyChanged -= ArrowContainerOnPropertyChanged;
                container.PropertyChanged += ArrowContainerOnPropertyChanged;
            }

            var moveBtn = this.FindByName<ImageButton>("DevicePlacementModeBtn");
            if (moveBtn != null)
            {
                Console.WriteLine("[PlacedDeviceControl] DevicePlacementModeBtn found");
                moveBtn.PropertyChanged -= MoveBtnOnPropertyChanged;
                moveBtn.PropertyChanged += MoveBtnOnPropertyChanged;
            }
            
            UpdateMoveModeUI();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlacedDeviceControl] OnControlLoaded error: {ex.Message}");
        }
    }

    private void OnArrowLoaded(object sender, EventArgs e)
    {
        if (sender is VisualElement ve)
        {
            var elem = ve as Element;
            var id = elem?.AutomationId ?? elem?.StyleId ?? "(no-id)";
            var src = (ve as ImageButton)?.Source;
            Console.WriteLine($"[PlacedDeviceControl] Arrow Loaded: {ve.GetType().Name} Id={id} Visible={ve.IsVisible}");
            ve.PropertyChanged -= ArrowOnPropertyChanged;
            ve.PropertyChanged += ArrowOnPropertyChanged;
        }
    }

    private void OnScaleButtonLoaded(object sender, EventArgs e)
    {
        if (sender is VisualElement ve)
        {
            var elem = ve as Element;
            var id = elem?.AutomationId ?? elem?.StyleId ?? "(no-id)";
            Console.WriteLine($"[PlacedDeviceControl] ScaleButton Loaded: {ve.GetType().Name} Id={id} Visible={ve.IsVisible}");
            ve.PropertyChanged -= ScaleButtonOnPropertyChanged;
            ve.PropertyChanged += ScaleButtonOnPropertyChanged;
        }
    }

    private void ArrowContainerOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement ve)
        {
            Console.WriteLine($"[PlacedDeviceControl] ArrowButtonsContainer IsVisible changed -> {ve.IsVisible}");
        }
    }

    private void MoveBtnOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ImageButton btn && (e.PropertyName == nameof(btn.IsEnabled) || e.PropertyName == nameof(btn.IsVisible)))
        {
            Console.WriteLine($"[PlacedDeviceControl] MoveBtn state: IsEnabled={btn.IsEnabled}, IsVisible={btn.IsVisible}");
        }
    }

    private void ArrowOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is VisualElement ve && (e.PropertyName == nameof(ve.IsVisible) || e.PropertyName == nameof(ve.IsEnabled)))
        {
            Console.WriteLine($"[PlacedDeviceControl] Arrow state change: {((Element)ve).GetType().Name} IsVisible={ve.IsVisible} IsEnabled={ve.IsEnabled}");
        }
    }

    private void ScaleButtonOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is VisualElement ve && (e.PropertyName == nameof(ve.IsVisible) || e.PropertyName == nameof(ve.IsEnabled)))
        {
            Console.WriteLine($"[PlacedDeviceControl] ScaleButton state change: {((Element)ve).GetType().Name} IsVisible={ve.IsVisible} IsEnabled={ve.IsEnabled}");
        }
    }

    private void UpdateMoveModeUI()
    {
        var moveBtn = this.FindByName<ImageButton>("DevicePlacementModeBtn");
        if (moveBtn != null && _viewModel != null)
        {
            VisualStateManager.GoToState(moveBtn, _viewModel.IsInMoveMode ? "Active" : "Inactive");
        }
    }
}
