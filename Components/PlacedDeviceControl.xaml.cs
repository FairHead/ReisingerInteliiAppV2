using ReisingerIntelliApp_V4.Models;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.Components;

public partial class PlacedDeviceControl : ContentView
{
    private const double ScaleMin = 0.01; // Reduced from 0.20 to 0.0 for much smaller scaling
    private const double ScaleMax = 2.50;
    private const double ScaleStep = 0.05;
private void OnControlLoaded(object sender, EventArgs e)
{
    // Optional: Debug-Ausgabe oder Initialisierung
}
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

    /// <summary>
    /// Calculates movement step size based on device scale.
    /// Smaller devices get smaller movement steps for finer control.
    /// </summary>
    /// <param name="deviceScale">Current scale of the device</param>
    /// <returns>Movement step size (0.005 to 0.05)</returns>
    private double GetMovementStepSize(double deviceScale)
    {
        // Base step size is 5% (0.05)
        const double baseStepSize = 0.05;
        const double minStepSize = 0.005; // Minimum step size for very small devices (0.5%)
        const double maxStepSize = 0.05;  // Maximum step size for large devices (5%)
        
        // Scale the step size proportionally to device scale
        // At scale 0.1 -> step = 0.005 (very small steps)
        // At scale 1.0 -> step = 0.05 (normal steps)
        // At scale 2.0+ -> step = 0.05 (max steps)
        var scaledStepSize = baseStepSize * Math.Max(0.1, deviceScale);
        var finalStepSize = Math.Clamp(scaledStepSize, minStepSize, maxStepSize);
        
        Debug.WriteLine($"📏 GetMovementStepSize - Scale: {deviceScale:F3}, Step: {finalStepSize:F4}");
        return finalStepSize;
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
            model.Scale = Math.Max(model.Scale - 0.05, 0.05); // Min scale 0.05 (reduced from 0.1)

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
        var moveButtonsContainer = this.FindByName<AbsoluteLayout>("ArrowButtonsContainer");
        if (moveButtonsContainer != null)
        {
            moveButtonsContainer.IsVisible = show;
            Debug.WriteLine($"🔲 PlacedDeviceControl.ToggleMoveButtons - Visibility: {show}");
        }
        else
        {
            Debug.WriteLine($"❌ PlacedDeviceControl.ToggleMoveButtons - ArrowButtonsContainer not found!");
        }
    }

    // Movement methods for arrow buttons - with strict bounds checking to prevent PanPinchContainer conflicts
    private void OnMoveUpClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldY = model.RelativeY;
            var moveStep = GetMovementStepSize(model.Scale);
            var newY = Math.Max(0.0, model.RelativeY - moveStep);
            
            Debug.WriteLine($"⬆️ PlacedDeviceControl.OnMoveUpClicked - Device: {model.Name}");
            Debug.WriteLine($"   📍 OLD Y: {oldY:F4}");
            Debug.WriteLine($"   📏 MOVE STEP: {moveStep:F4}");
            Debug.WriteLine($"   📍 NEW Y: {newY:F4}");
            Debug.WriteLine($"   🚨 BOUNDS CHECK: Y={newY:F4} in [0.0, 1.0] = {(newY >= 0.0 && newY <= 1.0)}");
            
            // Strict bounds check to prevent PanPinchContainer conflicts
            if (newY >= 0.0 && newY <= 1.0 && Math.Abs(model.RelativeY - newY) > 0.0001)
            {
                model.RelativeY = newY;
                Debug.WriteLine($"   ✅ Y Position Updated: {oldY:F4} → {model.RelativeY:F4}");
                MoveDeviceRequested?.Invoke(this, model);
                Debug.WriteLine($"   ✅ MoveDeviceRequested event fired");
            }
            else
            {
                Debug.WriteLine($"   ❌ Y Position unchanged (at boundary or invalid bounds)");
            }
        }
    }

    private void OnMoveDownClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Debug.WriteLine($"");
            Debug.WriteLine($"⬇️ =============== MOVE DOWN ANALYSIS ===============");
            Debug.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldY = model.RelativeY;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedY = model.RelativeY + moveStep;
            var newY = Math.Min(1.0, calculatedY);
            
            Debug.WriteLine($"📊 MOVEMENT CALCULATION:");
            Debug.WriteLine($"   📍 OLD RelativeY: {oldY:F6}");
            Debug.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Debug.WriteLine($"   🧮 Calculated Y: {oldY:F6} + {moveStep:F6} = {calculatedY:F6}");
            Debug.WriteLine($"   🔒 Clamped Y: Math.Min(1.0, {calculatedY:F6}) = {newY:F6}");
            Debug.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeY - newY):F6}");
            
            Debug.WriteLine($"🚨 VALIDATION CHECKS:");
            Debug.WriteLine($"   ✅ Y >= 0.0: {newY >= 0.0} ({newY:F6} >= 0.0)");
            Debug.WriteLine($"   ✅ Y <= 1.0: {newY <= 1.0} ({newY:F6} <= 1.0)");
            Debug.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeY - newY) > 0.0001} ({Math.Abs(model.RelativeY - newY):F6} > 0.0001)");
            
            // Enhanced boundary check - prevent movement that could cause Touch-Event conflicts
            var isNearBottomEdge = newY > 0.9; // Warning when approaching bottom edge
            var isAtBottomEdge = newY >= 1.0; // At absolute edge
            
            Debug.WriteLine($"🚨 ENHANCED BOUNDS ANALYSIS:");
            Debug.WriteLine($"   ⚠️ Near Bottom Edge: {isNearBottomEdge} (Y > 0.9)");
            Debug.WriteLine($"   🚫 At Bottom Edge: {isAtBottomEdge} (Y >= 1.0)");
            
            if (isNearBottomEdge)
            {
                Debug.WriteLine($"   ⚠️ WARNING: Device approaching dangerous edge zone!");
                Debug.WriteLine($"   🔍 POTENTIAL TOUCH CONFLICT ZONE!");
            }
            
            var willUpdate = newY >= 0.0 && newY <= 1.0 && Math.Abs(model.RelativeY - newY) > 0.0001;
            Debug.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Debug.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeY = newY;
                Debug.WriteLine($"   📍 New RelativeY Set: {model.RelativeY:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Debug.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Debug.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Debug.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newY < 0.0) Debug.WriteLine($"   ⚠️ newY < 0.0: {newY:F6}");
                if (newY > 1.0) Debug.WriteLine($"   ⚠️ newY > 1.0: {newY:F6} - BOUNDARY PROTECTION ACTIVE!");
                if (Math.Abs(model.RelativeY - newY) <= 0.0001) Debug.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeY - newY):F6}");
                Debug.WriteLine($"   🛡️ PROTECTED: Boundary conflict prevented!");
            }
            
            Debug.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Debug.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Debug.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Debug.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Debug.WriteLine($"⬇️ =============== MOVE DOWN COMPLETE ===============");
            Debug.WriteLine($"");
        }
    }

    private void OnMoveLeftClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Debug.WriteLine($"");
            Debug.WriteLine($"⬅️ =============== MOVE LEFT ANALYSIS ===============");
            Debug.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldX = model.RelativeX;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedX = model.RelativeX - moveStep;
            var newX = Math.Max(0.0, calculatedX);
            
            Debug.WriteLine($"📊 MOVEMENT CALCULATION:");
            Debug.WriteLine($"   📍 OLD RelativeX: {oldX:F6}");
            Debug.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Debug.WriteLine($"   🧮 Calculated X: {oldX:F6} - {moveStep:F6} = {calculatedX:F6}");
            Debug.WriteLine($"   🔒 Clamped X: Math.Max(0.0, {calculatedX:F6}) = {newX:F6}");
            Debug.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeX - newX):F6}");
            
            Debug.WriteLine($"🚨 VALIDATION CHECKS:");
            Debug.WriteLine($"   ✅ X >= 0.0: {newX >= 0.0} ({newX:F6} >= 0.0)");
            Debug.WriteLine($"   ✅ X <= 1.0: {newX <= 1.0} ({newX:F6} <= 1.0)");
            Debug.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeX - newX) > 0.0001} ({Math.Abs(model.RelativeX - newX):F6} > 0.0001)");
            
            // Enhanced boundary check - prevent movement that could cause Touch-Event conflicts
            var isNearLeftEdge = newX < 0.1; // Warning when approaching left edge
            var isAtLeftEdge = newX <= 0.0; // At absolute edge
            
            Debug.WriteLine($"🚨 ENHANCED BOUNDS ANALYSIS:");
            Debug.WriteLine($"   ⚠️ Near Left Edge: {isNearLeftEdge} (X < 0.1)");
            Debug.WriteLine($"   🚫 At Left Edge: {isAtLeftEdge} (X <= 0.0)");
            
            if (isNearLeftEdge)
            {
                Debug.WriteLine($"   ⚠️ WARNING: Device approaching dangerous edge zone!");
                Debug.WriteLine($"   🔍 POTENTIAL TOUCH CONFLICT ZONE!");
            }
            
            var willUpdate = newX >= 0.0 && newX <= 1.0 && Math.Abs(model.RelativeX - newX) > 0.0001;
            Debug.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Debug.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeX = newX;
                Debug.WriteLine($"   📍 New RelativeX Set: {model.RelativeX:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Debug.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Debug.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Debug.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newX < 0.0) Debug.WriteLine($"   ⚠️ newX < 0.0: {newX:F6} - BOUNDARY PROTECTION ACTIVE!");
                if (newX > 1.0) Debug.WriteLine($"   ⚠️ newX > 1.0: {newX:F6}");
                if (Math.Abs(model.RelativeX - newX) <= 0.0001) Debug.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeX - newX):F6}");
                Debug.WriteLine($"   🛡️ PROTECTED: Boundary conflict prevented!");
            }
            
            Debug.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Debug.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Debug.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Debug.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Debug.WriteLine($"⬅️ =============== MOVE LEFT COMPLETE ===============");
            Debug.WriteLine($"");
        }
    }

    private void OnMoveRightClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Debug.WriteLine($"");
            Debug.WriteLine($"➡️ =============== MOVE RIGHT ANALYSIS ===============");
            Debug.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldX = model.RelativeX;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedX = model.RelativeX + moveStep;
            var newX = Math.Min(1.0, calculatedX);
            
            Debug.WriteLine($"📊 MOVEMENT CALCULATION:");
            Debug.WriteLine($"   📍 OLD RelativeX: {oldX:F6}");
            Debug.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Debug.WriteLine($"   🧮 Calculated X: {oldX:F6} + {moveStep:F6} = {calculatedX:F6}");
            Debug.WriteLine($"   🔒 Clamped X: Math.Min(1.0, {calculatedX:F6}) = {newX:F6}");
            Debug.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeX - newX):F6}");
            
            Debug.WriteLine($"🚨 VALIDATION CHECKS:");
            Debug.WriteLine($"   ✅ X >= 0.0: {newX >= 0.0} ({newX:F6} >= 0.0)");
            Debug.WriteLine($"   ✅ X <= 1.0: {newX <= 1.0} ({newX:F6} <= 1.0)");
            Debug.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeX - newX) > 0.0001} ({Math.Abs(model.RelativeX - newX):F6} > 0.0001)");
            
            var willUpdate = newX >= 0.0 && newX <= 1.0 && Math.Abs(model.RelativeX - newX) > 0.0001;
            Debug.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Debug.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeX = newX;
                Debug.WriteLine($"   📍 New RelativeX Set: {model.RelativeX:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Debug.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Debug.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Debug.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newX < 0.0) Debug.WriteLine($"   ⚠️ newX < 0.0: {newX:F6}");
                if (newX > 1.0) Debug.WriteLine($"   ⚠️ newX > 1.0: {newX:F6} - PANPINCH PROTECTION ACTIVE!");
                if (Math.Abs(model.RelativeX - newX) <= 0.0001) Debug.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeX - newX):F6}");
                Debug.WriteLine($"   🛡️ PROTECTED: PanPinchContainer conflict prevented!");
            }
            
            Debug.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Debug.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Debug.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Debug.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Debug.WriteLine($"➡️ =============== MOVE RIGHT COMPLETE ===============");
            Debug.WriteLine($"");
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
            
            Debug.WriteLine($"➕ PlacedDeviceControl.OnScalePlusClicked - Device: {pd.Name}");
            Debug.WriteLine($"   📊 Old Scale: {oldScale:F3}");
            Debug.WriteLine($"   📊 New Scale: {newScale:F3}");
            Debug.WriteLine($"   📊 Scale Difference: {Math.Abs(newScale - pd.Scale):F6}");
            
            // Use a larger threshold for more reliable change detection
            if (Math.Abs(newScale - pd.Scale) > 0.001)
            {
                pd.Scale = newScale;
                
                Debug.WriteLine($"   📊 Scale Applied: {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                AddDeviceRequested?.Invoke(this, pd);
                Debug.WriteLine($"   ✅ AddDeviceRequested event fired");
            }
            else
            {
                Debug.WriteLine($"   ❌ Scale change too small, skipped");
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
            
            Debug.WriteLine($"➖ PlacedDeviceControl.OnScaleMinusClicked - Device: {pd.Name}");
            Debug.WriteLine($"   📊 Old Scale: {oldScale:F3}");
            Debug.WriteLine($"   📊 New Scale: {newScale:F3}");
            Debug.WriteLine($"   📊 Scale Difference: {Math.Abs(newScale - pd.Scale):F6}");
            
            // Use a larger threshold for more reliable change detection
            if (Math.Abs(newScale - pd.Scale) > 0.001)
            {
                pd.Scale = newScale;
                
                Debug.WriteLine($"   📊 Scale Applied: {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                RemoveDeviceRequested?.Invoke(this, pd);
                Debug.WriteLine($"   ✅ RemoveDeviceRequested event fired");
            }
            else
            {
                Debug.WriteLine($"   ❌ Scale change too small, skipped");
            }
        }
    }
}
