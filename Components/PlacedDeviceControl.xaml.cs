using ReisingerIntelliApp_V4.Models;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.Components;

public partial class PlacedDeviceControl : ContentView
{
    private const double ScaleMin = 0.20;
    private const double ScaleMax = 2.50;
    private const double ScaleStep = 0.05;

    public static readonly BindableProperty PlacedDeviceProperty =
        BindableProperty.Create(nameof(PlacedDevice), typeof(PlacedDeviceModel), typeof(PlacedDeviceControl), null, propertyChanged: OnPlacedDeviceChanged);

    public PlacedDeviceModel PlacedDevice
    {
        get => (PlacedDeviceModel)GetValue(PlacedDeviceProperty);
        set => SetValue(PlacedDeviceProperty, value);
    }

    // Events for device actions
    public event EventHandler<PlacedDeviceModel>? AddDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? RemoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ConfigureDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? DeleteDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? MoveDeviceRequested;
    public event EventHandler<PlacedDeviceModel>? ToggleDoorRequested;

    // For move mode
    private bool _isInMoveMode = false;

    public PlacedDeviceControl()
    {
        InitializeComponent();
    }

    private static void OnPlacedDeviceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlacedDeviceControl control && newValue is PlacedDeviceModel placedDevice)
        {
            Debug.WriteLine($"🔄 PlacedDeviceControl.OnPlacedDeviceChanged - Device: {placedDevice.Name}");
            Debug.WriteLine($"   📊 Initial Scale: {placedDevice.Scale:F2}");
            Debug.WriteLine($"   📍 Initial Position: X={placedDevice.RelativeX:F2}, Y={placedDevice.RelativeY:F2}");
            control.BindingContext = placedDevice;
        }
    }

    private PlacedDeviceModel? GetModel()
    {
        // Prefer explicitly set PlacedDevice, fall back to BindingContext
        return PlacedDevice ?? BindingContext as PlacedDeviceModel;
    }

    private void OnAddDeviceClicked(object sender, EventArgs e)
    {
        // Scale up the device by 0.05 (5%)
        var model = GetModel();
        if (model != null)
        {
            var oldScale = model.Scale;
            model.Scale = Math.Min(model.Scale + 0.05, 3.0); // Max scale 3.0

            Debug.WriteLine($"➕ PlacedDeviceControl.OnAddDeviceClicked - Device: {model.Name}");
            Debug.WriteLine($"   📊 Scale Change: {oldScale:F2} → {model.Scale:F2}");
            Debug.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");

            // Notify parent that scale changed (no move)
            AddDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ AddDeviceRequested event fired");
        }
    }

    private void OnRemoveDeviceClicked(object sender, EventArgs e)
    {
        // Scale down the device by 0.05 (5%)
        var model = GetModel();
        if (model != null)
        {
            var oldScale = model.Scale;
            model.Scale = Math.Max(model.Scale - 0.05, 0.1); // Min scale 0.1

            Debug.WriteLine($"➖ PlacedDeviceControl.OnRemoveDeviceClicked - Device: {model.Name}");
            Debug.WriteLine($"   📊 Scale Change: {oldScale:F2} → {model.Scale:F2}");
            Debug.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");

            // Notify parent that scale changed (no move)
            RemoveDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ RemoveDeviceRequested event fired");
        }
    }

    private void OnConfigureDeviceClicked(object sender, EventArgs e)
    {
        ConfigureDeviceRequested?.Invoke(this, PlacedDevice);
    }

    private void OnDeleteDeviceClicked(object sender, EventArgs e)
    {
        DeleteDeviceRequested?.Invoke(this, PlacedDevice);
    }

    private void OnMoveDeviceClicked(object sender, EventArgs e)
    {
        _isInMoveMode = !_isInMoveMode;
        
        var model = GetModel();
        Debug.WriteLine($"🔄 PlacedDeviceControl.OnMoveDeviceClicked - Device: {model?.Name}");
        Debug.WriteLine($"   🎯 Move Mode: {_isInMoveMode}");
        if (model != null)
        {
            Debug.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");
            Debug.WriteLine($"   📊 Current Scale: {model.Scale:F2}");
        }
        
        ToggleMoveButtons(_isInMoveMode);
        if (model != null)
            MoveDeviceRequested?.Invoke(this, model);
        Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
    }

    private void ToggleMoveButtons(bool show)
    {
        // Toggle visibility of move arrow buttons
        var moveButtonsContainer = this.FindByName<Grid>("ArrowButtonsContainer");
        if (moveButtonsContainer != null)
        {
            moveButtonsContainer.IsVisible = show;
            Debug.WriteLine($"🔲 PlacedDeviceControl.ToggleMoveButtons - Visibility: {show}");
        }
    }

    // Movement methods for arrow buttons
    private async void OnMoveUpClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldY = model.RelativeY;
            model.RelativeY = Math.Max(0.0, model.RelativeY - 0.05);
            
            Debug.WriteLine($"⬆️ PlacedDeviceControl.OnMoveUpClicked - Device: {model.Name}");
            Debug.WriteLine($"   📍 Y Position Change: {oldY:F2} → {model.RelativeY:F2}");
            Debug.WriteLine($"   📍 X Position (unchanged): {model.RelativeX:F2}");
            Debug.WriteLine($"   📊 Scale (unchanged): {model.Scale:F2}");
            
            // Trigger save after position change
            MoveDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
        }
    }

    private async void OnMoveDownClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldY = model.RelativeY;
            model.RelativeY = Math.Min(1.0, model.RelativeY + 0.05);
            
            Debug.WriteLine($"⬇️ PlacedDeviceControl.OnMoveDownClicked - Device: {model.Name}");
            Debug.WriteLine($"   📍 Y Position Change: {oldY:F2} → {model.RelativeY:F2}");
            Debug.WriteLine($"   📍 X Position (unchanged): {model.RelativeX:F2}");
            Debug.WriteLine($"   📊 Scale (unchanged): {model.Scale:F2}");
            
            // Trigger save after position change
            MoveDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
        }
    }

    private async void OnMoveLeftClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldX = model.RelativeX;
            model.RelativeX = Math.Max(0.0, model.RelativeX - 0.05);
            
            Debug.WriteLine($"⬅️ PlacedDeviceControl.OnMoveLeftClicked - Device: {model.Name}");
            Debug.WriteLine($"   📍 X Position Change: {oldX:F2} → {model.RelativeX:F2}");
            Debug.WriteLine($"   📍 Y Position (unchanged): {model.RelativeY:F2}");
            Debug.WriteLine($"   📊 Scale (unchanged): {model.Scale:F2}");
            
            // Trigger save after position change
            MoveDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
        }
    }

    private async void OnMoveRightClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldX = model.RelativeX;
            model.RelativeX = Math.Min(1.0, model.RelativeX + 0.05);
            
            Debug.WriteLine($"➡️ PlacedDeviceControl.OnMoveRightClicked - Device: {model.Name}");
            Debug.WriteLine($"   📍 X Position Change: {oldX:F2} → {model.RelativeX:F2}");
            Debug.WriteLine($"   📍 Y Position (unchanged): {model.RelativeY:F2}");
            Debug.WriteLine($"   📊 Scale (unchanged): {model.Scale:F2}");
            
            // Trigger save after position change
            MoveDeviceRequested?.Invoke(this, model);
            Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
        }
    }

    private void OnToggleDoorClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            model.IsDoorOpen = !model.IsDoorOpen;
            ToggleDoorRequested?.Invoke(this, model);
        }
    }

    // Plus = größer (5%)
    private void OnScalePlusClicked(object sender, EventArgs e)
    {
        var pd = GetModel();
        if (pd is not null)
        {
            var oldScale = pd.Scale;
            var newScale = Math.Min(ScaleMax, Math.Round(pd.Scale + ScaleStep, 3));
            if (Math.Abs(newScale - pd.Scale) > double.Epsilon)
            {
                pd.Scale = newScale;
                
                Debug.WriteLine($"➕ PlacedDeviceControl.OnScalePlusClicked - Device: {pd.Name}");
                Debug.WriteLine($"   📊 Scale Change: {oldScale:F3} → {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                AddDeviceRequested?.Invoke(this, pd);
                Debug.WriteLine($"   ✅ AddDeviceRequested event fired");
            }
        }
    }

    // Minus = kleiner (5%)
    private void OnScaleMinusClicked(object sender, EventArgs e)
    {
        var pd = GetModel();
        if (pd is not null)
        {
            var oldScale = pd.Scale;
            var newScale = Math.Max(ScaleMin, Math.Round(pd.Scale - ScaleStep, 3));
            if (Math.Abs(newScale - pd.Scale) > double.Epsilon)
            {
                pd.Scale = newScale;
                
                Debug.WriteLine($"➖ PlacedDeviceControl.OnScaleMinusClicked - Device: {pd.Name}");
                Debug.WriteLine($"   📊 Scale Change: {oldScale:F3} → {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                RemoveDeviceRequested?.Invoke(this, pd);
                Debug.WriteLine($"   ✅ RemoveDeviceRequested event fired");
            }
        }
    }
}
