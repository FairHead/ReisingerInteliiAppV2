using ReisingerIntelliApp_V4.Models;
using System;

namespace ReisingerIntelliApp_V4.Components;

public partial class PlacedDeviceControl : ContentView
{
    private const double ScaleMin = 0.01; // Reduced from 0.20 to 0.0 for much smaller scaling
    private const double ScaleMax = 2.50;
    private const double ScaleStep = 0.05;

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
            // Ensure UI reflects initial state
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
            Console.WriteLine($"[PlacedDeviceControl] Arrow Loaded: {ve.GetType().Name} Id={id} Visible={ve.IsVisible} SourceType={src?.GetType().Name ?? "(null)"} Source={src}");
            ve.PropertyChanged -= ArrowOnPropertyChanged;
            ve.PropertyChanged += ArrowOnPropertyChanged;
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

    private void ScaleButtonOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is VisualElement ve && (e.PropertyName == nameof(ve.IsVisible) || e.PropertyName == nameof(ve.IsEnabled)))
        {
            Console.WriteLine($"[PlacedDeviceControl] ScaleButton state change: {((Element)ve).GetType().Name} IsVisible={ve.IsVisible} IsEnabled={ve.IsEnabled}");
        }
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
        public bool IsInMoveMode
        {
            get => _isInMoveMode;
            set
            {
                if (_isInMoveMode != value)
                {
                    _isInMoveMode = value;
                    OnPropertyChanged(nameof(IsInMoveMode));
                    UpdateMoveModeUI();
                }
            }
        }

        public new event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected new virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    public PlacedDeviceControl()
    {
        InitializeComponent();
        this.BindingContext = this;
    }

    private void OnInteractivePressed(object? sender, EventArgs e)
    {
        try
        {
#pragma warning disable CS0618
            MessagingCenter.Send(this, "PanInputBlock", true);
#pragma warning restore CS0618
            Console.WriteLine("[PlacedDeviceControl] Interactive Pressed -> Block Pan");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlacedDeviceControl] OnInteractivePressed error: {ex.Message}");
        }
    }

    private void OnInteractiveReleased(object? sender, EventArgs e)
    {
        try
        {
#pragma warning disable CS0618
            MessagingCenter.Send(this, "PanInputBlock", false);
#pragma warning restore CS0618
            Console.WriteLine("[PlacedDeviceControl] Interactive Released -> Unblock Pan");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlacedDeviceControl] OnInteractiveReleased error: {ex.Message}");
        }
    }

    private static void OnPlacedDeviceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PlacedDeviceControl control && newValue is PlacedDeviceModel placedDevice)
        {
            Console.WriteLine($"🔄 PlacedDeviceControl.OnPlacedDeviceChanged - Device: {placedDevice.Name}");
            Console.WriteLine($"   📊 Initial Scale: {placedDevice.Scale:F2}");
            Console.WriteLine($"   📍 Initial Position: X={placedDevice.RelativeX:F2}, Y={placedDevice.RelativeY:F2}");
            control.BindingContext = placedDevice;
            // Ensure move mode is reset to default when a new device is assigned
            control.IsInMoveMode = false;
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
        
            Console.WriteLine($"📏 GetMovementStepSize - Scale: {deviceScale:F3}, Step: {finalStepSize:F4}");
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

            Console.WriteLine($"➕ PlacedDeviceControl.OnAddDeviceClicked - Device: {model.Name}");
            Console.WriteLine($"   📊 Scale Change: {oldScale:F2} → {model.Scale:F2}");
            Console.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");

            // Notify parent that scale changed (no move)
            AddDeviceRequested?.Invoke(this, model);
            Console.WriteLine($"   ✅ AddDeviceRequested event fired");
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

            Console.WriteLine($"➖ PlacedDeviceControl.OnRemoveDeviceClicked - Device: {model.Name}");
            Console.WriteLine($"   📊 Scale Change: {oldScale:F2} → {model.Scale:F2}");
            Console.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");

            // Notify parent that scale changed (no move)
            RemoveDeviceRequested?.Invoke(this, model);
            Console.WriteLine($"   ✅ RemoveDeviceRequested event fired");
        }
    }

    private void OnConfigureDeviceClicked(object sender, EventArgs e)
    {
        ConfigureDeviceRequested?.Invoke(this, PlacedDevice);
    }

    private void OnDeleteDeviceClicked(object sender, EventArgs e)
    {
        Console.WriteLine($"[PlacedDeviceControl] Delete button clicked for device: {PlacedDevice?.Name ?? "(null)"}");
        if (DeleteDeviceRequested != null)
        {
            Console.WriteLine($"[PlacedDeviceControl] Raising DeleteDeviceRequested event for device: {PlacedDevice?.Name ?? "(null)"}");
            if (PlacedDevice != null)
                DeleteDeviceRequested.Invoke(this, PlacedDevice);
        }
        else
        {
            Console.WriteLine("[PlacedDeviceControl] DeleteDeviceRequested event has no subscribers!");
        }
    }

    private void OnMoveDeviceClicked(object sender, EventArgs e)
    {
        IsInMoveMode = !IsInMoveMode;
        var model = GetModel();
        Console.WriteLine($"🔄 PlacedDeviceControl.OnMoveDeviceClicked - Device: {model?.Name}");
        Console.WriteLine($"   🎯 Move Mode: {IsInMoveMode}");
        if (model != null)
        {
            Console.WriteLine($"   📍 Current Position: X={model.RelativeX:F2}, Y={model.RelativeY:F2}");
            Console.WriteLine($"   📊 Current Scale: {model.Scale:F2}");
        }
        if (model != null)
            MoveDeviceRequested?.Invoke(this, model);
        Console.WriteLine($"   ✅ MoveDeviceRequested event fired");
    }

    private void UpdateMoveModeUI()
    {
        // Update button visual state
        var moveBtn = this.FindByName<ImageButton>("DevicePlacementModeBtn");
        if (moveBtn != null)
        {
            VisualStateManager.GoToState(moveBtn, IsInMoveMode ? "Active" : "Inactive");
        }
        
        // Ensure visibility is set (backup for XAML binding)
        var scaleGrid = this.FindByName<Grid>("ScaleBtnGrid");
        if (scaleGrid != null)
        {
            scaleGrid.IsVisible = IsInMoveMode;
            Console.WriteLine($"🔲 UpdateMoveModeUI - ScaleBtnGrid.IsVisible = {IsInMoveMode}");
        }
        
        var arrowContainer = this.FindByName<AbsoluteLayout>("ArrowButtonsContainer");
        if (arrowContainer != null)
        {
            arrowContainer.IsVisible = IsInMoveMode;
            Console.WriteLine($"🔲 UpdateMoveModeUI - ArrowButtonsContainer.IsVisible = {IsInMoveMode}");
        }
    }

    // ToggleMoveButtons method removed - visibility is now handled by XAML binding to IsInMoveMode

    // Movement methods for arrow buttons - with strict bounds checking to prevent PanPinchContainer conflicts
    private void OnMoveUpClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            var oldY = model.RelativeY;
            var moveStep = GetMovementStepSize(model.Scale);
            var newY = Math.Max(0.0, model.RelativeY - moveStep);
            
            Console.WriteLine($"⬆️ PlacedDeviceControl.OnMoveUpClicked - Device: {model.Name}");
            Console.WriteLine($"   📍 OLD Y: {oldY:F4}");
            Console.WriteLine($"   📏 MOVE STEP: {moveStep:F4}");
            Console.WriteLine($"   📍 NEW Y: {newY:F4}");
            Console.WriteLine($"   🚨 BOUNDS CHECK: Y={newY:F4} in [0.0, 1.0] = {(newY >= 0.0 && newY <= 1.0)}");
            
            // Strict bounds check to prevent PanPinchContainer conflicts
            if (newY >= 0.0 && newY <= 1.0 && Math.Abs(model.RelativeY - newY) > 0.0001)
            {
                model.RelativeY = newY;
                Console.WriteLine($"   ✅ Y Position Updated: {oldY:F4} → {model.RelativeY:F4}");
                MoveDeviceRequested?.Invoke(this, model);
                Console.WriteLine($"   ✅ MoveDeviceRequested event fired");
            }
            else
            {
                Console.WriteLine($"   ❌ Y Position unchanged (at boundary or invalid bounds)");
            }
        }
    }

    private void OnMoveDownClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Console.WriteLine($"");
            Console.WriteLine($"⬇️ =============== MOVE DOWN ANALYSIS ===============");
            Console.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldY = model.RelativeY;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedY = model.RelativeY + moveStep;
            var newY = Math.Min(1.0, calculatedY);
            
            Console.WriteLine($"📊 MOVEMENT CALCULATION:");
            Console.WriteLine($"   📍 OLD RelativeY: {oldY:F6}");
            Console.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Console.WriteLine($"   🧮 Calculated Y: {oldY:F6} + {moveStep:F6} = {calculatedY:F6}");
            Console.WriteLine($"   🔒 Clamped Y: Math.Min(1.0, {calculatedY:F6}) = {newY:F6}");
            Console.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeY - newY):F6}");
            
            Console.WriteLine($"🚨 VALIDATION CHECKS:");
            Console.WriteLine($"   ✅ Y >= 0.0: {newY >= 0.0} ({newY:F6} >= 0.0)");
            Console.WriteLine($"   ✅ Y <= 1.0: {newY <= 1.0} ({newY:F6} <= 1.0)");
            Console.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeY - newY) > 0.0001} ({Math.Abs(model.RelativeY - newY):F6} > 0.0001)");
            
            // Enhanced boundary check - prevent movement that could cause Touch-Event conflicts
            var isNearBottomEdge = newY > 0.9; // Warning when approaching bottom edge
            var isAtBottomEdge = newY >= 1.0; // At absolute edge
            
            Console.WriteLine($"🚨 ENHANCED BOUNDS ANALYSIS:");
            Console.WriteLine($"   ⚠️ Near Bottom Edge: {isNearBottomEdge} (Y > 0.9)");
            Console.WriteLine($"   🚫 At Bottom Edge: {isAtBottomEdge} (Y >= 1.0)");
            
            if (isNearBottomEdge)
            {
                Console.WriteLine($"   ⚠️ WARNING: Device approaching dangerous edge zone!");
                Console.WriteLine($"   🔍 POTENTIAL TOUCH CONFLICT ZONE!");
            }
            
            var willUpdate = newY >= 0.0 && newY <= 1.0 && Math.Abs(model.RelativeY - newY) > 0.0001;
            Console.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Console.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeY = newY;
                Console.WriteLine($"   📍 New RelativeY Set: {model.RelativeY:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Console.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Console.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Console.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newY < 0.0) Console.WriteLine($"   ⚠️ newY < 0.0: {newY:F6}");
                if (newY > 1.0) Console.WriteLine($"   ⚠️ newY > 1.0: {newY:F6} - BOUNDARY PROTECTION ACTIVE!");
                if (Math.Abs(model.RelativeY - newY) <= 0.0001) Console.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeY - newY):F6}");
                Console.WriteLine($"   🛡️ PROTECTED: Boundary conflict prevented!");
            }
            
            Console.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Console.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Console.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Console.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Console.WriteLine($"⬇️ =============== MOVE DOWN COMPLETE ===============");
            Console.WriteLine($"");
        }
    }

    private void OnMoveLeftClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Console.WriteLine($"");
            Console.WriteLine($"⬅️ =============== MOVE LEFT ANALYSIS ===============");
            Console.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldX = model.RelativeX;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedX = model.RelativeX - moveStep;
            var newX = Math.Max(0.0, calculatedX);
            
            Console.WriteLine($"📊 MOVEMENT CALCULATION:");
            Console.WriteLine($"   📍 OLD RelativeX: {oldX:F6}");
            Console.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Console.WriteLine($"   🧮 Calculated X: {oldX:F6} - {moveStep:F6} = {calculatedX:F6}");
            Console.WriteLine($"   🔒 Clamped X: Math.Max(0.0, {calculatedX:F6}) = {newX:F6}");
            Console.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeX - newX):F6}");
            
            Console.WriteLine($"🚨 VALIDATION CHECKS:");
            Console.WriteLine($"   ✅ X >= 0.0: {newX >= 0.0} ({newX:F6} >= 0.0)");
            Console.WriteLine($"   ✅ X <= 1.0: {newX <= 1.0} ({newX:F6} <= 1.0)");
            Console.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeX - newX) > 0.0001} ({Math.Abs(model.RelativeX - newX):F6} > 0.0001)");
            
            // Enhanced boundary check - prevent movement that could cause Touch-Event conflicts
            var isNearLeftEdge = newX < 0.1; // Warning when approaching left edge
            var isAtLeftEdge = newX <= 0.0; // At absolute edge
            
            Console.WriteLine($"🚨 ENHANCED BOUNDS ANALYSIS:");
            Console.WriteLine($"   ⚠️ Near Left Edge: {isNearLeftEdge} (X < 0.1)");
            Console.WriteLine($"   🚫 At Left Edge: {isAtLeftEdge} (X <= 0.0)");
            
            if (isNearLeftEdge)
            {
                Console.WriteLine($"   ⚠️ WARNING: Device approaching dangerous edge zone!");
                Console.WriteLine($"   🔍 POTENTIAL TOUCH CONFLICT ZONE!");
            }
            
            var willUpdate = newX >= 0.0 && newX <= 1.0 && Math.Abs(model.RelativeX - newX) > 0.0001;
            Console.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Console.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeX = newX;
                Console.WriteLine($"   📍 New RelativeX Set: {model.RelativeX:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Console.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Console.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Console.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newX < 0.0) Console.WriteLine($"   ⚠️ newX < 0.0: {newX:F6} - BOUNDARY PROTECTION ACTIVE!");
                if (newX > 1.0) Console.WriteLine($"   ⚠️ newX > 1.0: {newX:F6}");
                if (Math.Abs(model.RelativeX - newX) <= 0.0001) Console.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeX - newX):F6}");
                Console.WriteLine($"   🛡️ PROTECTED: Boundary conflict prevented!");
            }
            
            Console.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Console.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Console.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Console.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Console.WriteLine($"⬅️ =============== MOVE LEFT COMPLETE ===============");
            Console.WriteLine($"");
        }
    }

    private void OnMoveRightClicked(object sender, EventArgs e)
    {
        var model = GetModel();
        if (model != null)
        {
            Console.WriteLine($"");
            Console.WriteLine($"➡️ =============== MOVE RIGHT ANALYSIS ===============");
            Console.WriteLine($"🔧 DEVICE: {model.Name}");
            
            var oldX = model.RelativeX;
            var moveStep = GetMovementStepSize(model.Scale);
            var calculatedX = model.RelativeX + moveStep;
            var newX = Math.Min(1.0, calculatedX);
            
            Console.WriteLine($"📊 MOVEMENT CALCULATION:");
            Console.WriteLine($"   📍 OLD RelativeX: {oldX:F6}");
            Console.WriteLine($"   📏 Move Step: {moveStep:F6}");
            Console.WriteLine($"   🧮 Calculated X: {oldX:F6} + {moveStep:F6} = {calculatedX:F6}");
            Console.WriteLine($"   🔒 Clamped X: Math.Min(1.0, {calculatedX:F6}) = {newX:F6}");
            Console.WriteLine($"   📏 Change Amount: {Math.Abs(model.RelativeX - newX):F6}");
            
            Console.WriteLine($"🚨 VALIDATION CHECKS:");
            Console.WriteLine($"   ✅ X >= 0.0: {newX >= 0.0} ({newX:F6} >= 0.0)");
            Console.WriteLine($"   ✅ X <= 1.0: {newX <= 1.0} ({newX:F6} <= 1.0)");
            Console.WriteLine($"   ✅ Significant Change: {Math.Abs(model.RelativeX - newX) > 0.0001} ({Math.Abs(model.RelativeX - newX):F6} > 0.0001)");
            
            var willUpdate = newX >= 0.0 && newX <= 1.0 && Math.Abs(model.RelativeX - newX) > 0.0001;
            Console.WriteLine($"🎯 WILL UPDATE: {willUpdate}");
            
            if (willUpdate)
            {
                Console.WriteLine($"✅ UPDATING POSITION...");
                model.RelativeX = newX;
                Console.WriteLine($"   📍 New RelativeX Set: {model.RelativeX:F6}");
                
                MoveDeviceRequested?.Invoke(this, model);
                Console.WriteLine($"   🚀 MoveDeviceRequested event fired");
                Console.WriteLine($"   📤 This will trigger PositionDeviceView with new coordinates");
            }
            else
            {
                Console.WriteLine($"❌ POSITION NOT UPDATED:");
                if (newX < 0.0) Console.WriteLine($"   ⚠️ newX < 0.0: {newX:F6}");
                if (newX > 1.0) Console.WriteLine($"   ⚠️ newX > 1.0: {newX:F6} - PANPINCH PROTECTION ACTIVE!");
                if (Math.Abs(model.RelativeX - newX) <= 0.0001) Console.WriteLine($"   ⚠️ Change too small: {Math.Abs(model.RelativeX - newX):F6}");
                Console.WriteLine($"   🛡️ PROTECTED: PanPinchContainer conflict prevented!");
            }
            
            Console.WriteLine($"🔍 CURRENT DEVICE STATE AFTER OPERATION:");
            Console.WriteLine($"   📍 RelativeX: {model.RelativeX:F6}");
            Console.WriteLine($"   📍 RelativeY: {model.RelativeY:F6}");
            Console.WriteLine($"   📊 Scale: {model.Scale:F4}");
            Console.WriteLine($"➡️ =============== MOVE RIGHT COMPLETE ===============");
            Console.WriteLine($"");
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
            
            Console.WriteLine($"➕ PlacedDeviceControl.OnScalePlusClicked - Device: {pd.Name}");
            Console.WriteLine($"   📊 Old Scale: {oldScale:F3}");
            Console.WriteLine($"   📊 New Scale: {newScale:F3}");
            Console.WriteLine($"   📊 Scale Difference: {Math.Abs(newScale - pd.Scale):F6}");
            
            // Use a larger threshold for more reliable change detection
            if (Math.Abs(newScale - pd.Scale) > 0.001)
            {
                pd.Scale = newScale;
                
                Console.WriteLine($"   📊 Scale Applied: {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                AddDeviceRequested?.Invoke(this, pd);
                Console.WriteLine($"   ✅ AddDeviceRequested event fired");
            }
            else
            {
                Console.WriteLine($"   ❌ Scale change too small, skipped");
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
            
            Console.WriteLine($"➖ PlacedDeviceControl.OnScaleMinusClicked - Device: {pd.Name}");
            Console.WriteLine($"   📊 Old Scale: {oldScale:F3}");
            Console.WriteLine($"   📊 New Scale: {newScale:F3}");
            Console.WriteLine($"   📊 Scale Difference: {Math.Abs(newScale - pd.Scale):F6}");
            
            // Use a larger threshold for more reliable change detection
            if (Math.Abs(newScale - pd.Scale) > 0.001)
            {
                pd.Scale = newScale;
                
                Console.WriteLine($"   📊 Scale Applied: {pd.Scale:F3}");
                
                // Fire the event to notify MainPage to update layout
                RemoveDeviceRequested?.Invoke(this, pd);
                Console.WriteLine($"   ✅ RemoveDeviceRequested event fired");
            }
            else
            {
                Console.WriteLine($"   ❌ Scale change too small, skipped");
            }
        }
    }
}
