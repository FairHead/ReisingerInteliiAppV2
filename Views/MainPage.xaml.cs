using Microsoft.Maui.Controls.Shapes;
using ReisingerIntelliApp_V4.ViewModels;
using ReisingerIntelliApp_V4.Helpers;
using ReisingerIntelliApp_V4.Services;
using ReisingerIntelliApp_V4.Models;
using System.Linq;
using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ReisingerIntelliApp_V4.Views;

public partial class MainPage : ContentPage, IPlanViewportService
{
    private MainPageViewModel? _viewModel;
    private double _planIntrinsicWidth;
    private double _planIntrinsicHeight;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.AttachViewport(this);
        SetupFooterEvents();
        SetupViewModelEvents();
        
        // Listen for force device layout refresh messages
        MessagingCenter.Subscribe<MainPageViewModel>(this, "ForceDeviceLayoutRefresh", (sender) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Console.WriteLine("🔄 Force device layout refresh requested");
                InvalidateDevicesLayout();
            });
        });
        
        // Hook plan updates to refresh image source when properties change
        if (_viewModel.StructuresVM != null)
        {
            _viewModel.StructuresVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.StructuresVM.CurrentPngPath))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlanImage.Source = _viewModel.StructuresVM.CurrentPngPath;
                        UpdatePlanIntrinsicSize();
                        InvalidateDevicesLayout();
                    });
                }
                else if (e.PropertyName == nameof(_viewModel.StructuresVM.SelectedLevel))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WireDevicesCollection();
                        InvalidateDevicesLayout();
                    });
                }
            };
        }

        // Ensure we are wired to the current level and can layout existing devices immediately
        WireDevicesCollection();
        InvalidateDevicesLayout();

        // When BindableLayout creates a new visual child, position it right away
        if (DevicesOverlay != null)
        {
            DevicesOverlay.ChildAdded += (sender, args) =>
            {
                if (args.Element is Components.PlacedDeviceControl ctrl && ctrl.BindingContext is PlacedDeviceModel pd)
                {
                    // Wire scale events and position the control
                    ctrl.AddDeviceRequested -= OnDeviceIncreaseRequested;
                    ctrl.RemoveDeviceRequested -= OnDeviceDecreaseRequested;
                    ctrl.DeleteDeviceRequested -= OnDeviceDeleteRequested;
                    ctrl.MoveDeviceRequested -= OnDeviceMoveRequested;
                    ctrl.AddDeviceRequested += OnDeviceIncreaseRequested;
                    ctrl.RemoveDeviceRequested += OnDeviceDecreaseRequested;
                    ctrl.DeleteDeviceRequested += OnDeviceDeleteRequested;
                    ctrl.MoveDeviceRequested += OnDeviceMoveRequested;
                    PositionDeviceView(ctrl, pd);
                }
            };
        }

    System.Diagnostics.Debug.WriteLine("MainPage initialized");
    }



    private void SetupViewModelEvents()
    {
        if (_viewModel != null)
        {
            _viewModel.TabActivated += (sender, tabName) =>
            {
                SetActiveTab(tabName);
            };
            
            _viewModel.TabDeactivated += (sender, e) =>
            {
                ResetAllTabs();
            };
        }

    }

    #region Plan viewport service implementation

        public bool IsPlanReady => PlanImage?.Source != null && _planIntrinsicWidth > 0 && _planIntrinsicHeight > 0;

        public double PlanWidth => _planIntrinsicWidth;
        public double PlanHeight => _planIntrinsicHeight;

        public new double Scale => PlanContainer?.Content?.Scale ?? 1.0;
        public new double TranslationX => PlanContainer?.Content?.TranslationX ?? 0.0;
        public new double TranslationY => PlanContainer?.Content?.TranslationY ?? 0.0;

        public Point ScreenToPlan(Point screenPoint)
        {
            if (!IsPlanReady || PlanImage == null)
                return new Point(PlanWidth / 2, PlanHeight / 2);

            // Map screen to container content coordinates considering translation/scale
            var container = PlanContainer;
            var content = container.Content as View;
            if (content == null) return new Point(PlanWidth / 2, PlanHeight / 2);

            // Treat screenPoint as container-local coordinates for this mapping
            var localX = screenPoint.X;
            var localY = screenPoint.Y;

            // Undo translation
            localX -= content.TranslationX;
            localY -= content.TranslationY;

            // Undo scale
            var unscaledX = localX / (content.Scale <= 0 ? 1 : content.Scale);
            var unscaledY = localY / (content.Scale <= 0 ? 1 : content.Scale);

            // Image is AspectFit centered; compute the drawn image rect within content
            var (drawnX, drawnY, drawnW, drawnH) = GetImageDrawnRect();
            // Map local content point to image pixel
            var xInImage = (unscaledX - drawnX) / drawnW * PlanWidth;
            var yInImage = (unscaledY - drawnY) / drawnH * PlanHeight;
            return new Point(xInImage, yInImage);
        }

        public Point GetViewportCenterInPlan()
        {
            // Use the plan container's own center in its local coordinate space
            var cx = PlanContainer?.Width > 0 ? PlanContainer.Width / 2 : 0;
            var cy = PlanContainer?.Height > 0 ? PlanContainer.Height / 2 : 0;
            return ScreenToPlan(new Point(cx, cy));
        }

        private void UpdatePlanIntrinsicSize()
        {
            // Use current view's arranged size as approximation of intrinsic drawn size
            _planIntrinsicWidth = PlanImage?.Width ?? 0;
            _planIntrinsicHeight = PlanImage?.Height ?? 0;
        }

        private (double x, double y, double w, double h) GetImageDrawnRect()
        {
            if (PlanImage == null || _planIntrinsicWidth <= 0 || _planIntrinsicHeight <= 0)
                return (0, 0, PlanContainer.Width, PlanContainer.Height);

            var viewW = PlanImage.Width;
            var viewH = PlanImage.Height;
            if (viewW <= 0 || viewH <= 0)
                return (0, 0, PlanContainer.Width, PlanContainer.Height);

            var imgW = _planIntrinsicWidth;
            var imgH = _planIntrinsicHeight;
            var scale = Math.Min(viewW / imgW, viewH / imgH);
            var drawnW = imgW * scale;
            var drawnH = imgH * scale;
            var offsetX = (viewW - drawnW) / 2;
            var offsetY = (viewH - drawnH) / 2;
            return (offsetX, offsetY, drawnW, drawnH);
        }

    #endregion

    #region Devices overlay layout

        private void WireDevicesCollection()
        {
            var level = _viewModel?.StructuresVM?.SelectedLevel;
            if (level?.PlacedDevices == null) return;
            level.PlacedDevices.CollectionChanged -= PlacedDevices_CollectionChanged;
            level.PlacedDevices.CollectionChanged += PlacedDevices_CollectionChanged;

            foreach (var pd in level.PlacedDevices)
            {
                pd.PropertyChanged -= PlacedDevice_PropertyChanged;
                pd.PropertyChanged += PlacedDevice_PropertyChanged;
            }
        }

        private void PlacedDevices_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine($"[PlacedDevices_CollectionChanged] Action: {e.Action}, NewItems: {e.NewItems?.Count ?? 0}, OldItems: {e.OldItems?.Count ?? 0}");
            
            // Debounce layout invalidation to prevent excessive calls
            _layoutInvalidationTimer?.Dispose();
            _layoutInvalidationTimer = new Timer((_) => 
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        Console.WriteLine("[PlacedDevices_CollectionChanged] -> InvalidateDevicesLayout (debounced)");
                        InvalidateDevicesLayout();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PlacedDevices_CollectionChanged] Timer callback error: {ex.Message}");
                }
            }, null, 50, Timeout.Infinite);
            
            // Don't call async operations from collection changed events - this can cause deadlocks
            try
            {
                Console.WriteLine("[PlacedDevices_CollectionChanged] Scheduling SaveCurrentFloorAsync...");
                // Use fire-and-forget with proper error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _viewModel?.SaveCurrentFloorAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PlacedDevices_CollectionChanged] SaveCurrentFloorAsync error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlacedDevices_CollectionChanged] Exception: {ex.Message}\n{ex.StackTrace}");
            }

            if (e.NewItems != null)
            {
                foreach (var obj in e.NewItems.OfType<PlacedDeviceModel>())
                {
                    obj.PropertyChanged -= PlacedDevice_PropertyChanged;
                    obj.PropertyChanged += PlacedDevice_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var obj in e.OldItems.OfType<PlacedDeviceModel>())
                {
                    obj.PropertyChanged -= PlacedDevice_PropertyChanged;
                }
            }
        }

        private Timer? _layoutInvalidationTimer;
        private Timer? _viewportUpdateTimer;

        private void PlacedDevice_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PlacedDeviceModel.RelativeX) or nameof(PlacedDeviceModel.RelativeY) or nameof(PlacedDeviceModel.Scale)
                or nameof(PlacedDeviceModel.BaseWidthNorm) or nameof(PlacedDeviceModel.BaseHeightNorm))
            {
                // Immediate layout update for scale changes to ensure visual feedback
                if (e.PropertyName == nameof(PlacedDeviceModel.Scale))
                {
                    try
                    {
                    Console.WriteLine("🔄 Force device layout refresh requested");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PlacedDevice_PropertyChanged] Scale update error: {ex.Message}");
                    }
                }
                else
                {
                    // Debounce layout invalidation for position changes only
                    _layoutInvalidationTimer?.Dispose();
                    _layoutInvalidationTimer = new Timer((_) => 
                    {
                        try
                        {
                            MainThread.BeginInvokeOnMainThread(InvalidateDevicesLayout);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[PlacedDevice_PropertyChanged] Timer callback error: {ex.Message}");
                        }
                    }, null, 10, Timeout.Infinite);
                }
            }
        }

        private void InvalidateDevicesLayout()
        {
            if (DevicesOverlay == null || _viewModel?.StructuresVM?.SelectedLevel?.PlacedDevices == null)
            {
                Console.WriteLine("[InvalidateDevicesLayout] DevicesOverlay or PlacedDevices is null. Skipping.");
                return;
            }

            Console.WriteLine($"[InvalidateDevicesLayout] Processing {DevicesOverlay.Children.Count} visual children");

            // Iterate through visual children and position them based on the bound model
            foreach (var child in DevicesOverlay.Children.OfType<Components.PlacedDeviceControl>())
            {
                if (child.BindingContext is not PlacedDeviceModel pd)
                {
                    Console.WriteLine("[InvalidateDevicesLayout] Skipping child with null or invalid BindingContext.");
                    continue;
                }
                Console.WriteLine($"[InvalidateDevicesLayout] Processing device: {pd.Name}, Scale: {pd.Scale:F3}, X: {pd.RelativeX}, Y: {pd.RelativeY}");
                
                // Wire events only once - remove first to prevent duplicates
                child.AddDeviceRequested -= OnDeviceIncreaseRequested;
                child.RemoveDeviceRequested -= OnDeviceDecreaseRequested;
                child.DeleteDeviceRequested -= OnDeviceDeleteRequested; // <--- FEHLTE!
                child.MoveDeviceRequested -= OnDeviceMoveRequested;
                
                // Then add them back
                child.AddDeviceRequested += OnDeviceIncreaseRequested;
                child.RemoveDeviceRequested += OnDeviceDecreaseRequested;
                child.DeleteDeviceRequested += OnDeviceDeleteRequested; // <--- HINZUFÜGEN!
                child.MoveDeviceRequested += OnDeviceMoveRequested;

                PositionDeviceView(child, pd);
            }
            
            Console.WriteLine($"✅ InvalidateDevicesLayout complete");
        }

        private void OnDeviceIncreaseRequested(object? sender, PlacedDeviceModel e)
        {
            Console.WriteLine($"🔼 OnDeviceIncreaseRequested - Device: {e.Name}");
            Console.WriteLine($"   📊 Current Scale: {e.Scale:F3}");
            
            // Scale already updated by control; just clamp, persist, and re-layout
            var originalScale = e.Scale;
            e.Scale = Math.Clamp(e.Scale, 0.05, 3.0); // Reduced min from 0.1 to 0.05
            
            Console.WriteLine($"   📊 After Clamp: {e.Scale:F3}");
            Console.WriteLine($"   📊 Scale Changed: {(originalScale != e.Scale ? "YES" : "NO")}");
            
            _ = _viewModel?.SaveCurrentFloorAsync();
            // Force immediate layout update for scale changes
            InvalidateDevicesLayout();
            
            Console.WriteLine($"   ✅ OnDeviceIncreaseRequested complete");
        }

        private void OnDeviceDecreaseRequested(object? sender, PlacedDeviceModel e)
        {
            Console.WriteLine($"🔽 OnDeviceDecreaseRequested - Device: {e.Name}");
            Console.WriteLine($"   📊 Current Scale: {e.Scale:F3}");
            
            // Scale already updated by control; just clamp, persist, and re-layout
            var originalScale = e.Scale;
            e.Scale = Math.Clamp(e.Scale, 0.05, 3.0); // Reduced min from 0.1 to 0.05
            
            Console.WriteLine($"   📊 After Clamp: {e.Scale:F3}");
            Console.WriteLine($"   📊 Scale Changed: {(originalScale != e.Scale ? "YES" : "NO")}");
            
            _ = _viewModel?.SaveCurrentFloorAsync();
            // Force immediate layout update for scale changes
            InvalidateDevicesLayout();
            
            Console.WriteLine($"   ✅ OnDeviceDecreaseRequested complete");
        }

        private void PositionDeviceView(Components.PlacedDeviceControl view, PlacedDeviceModel pd)
        {
            if (!IsPlanReady) return;
            
            // SIMPLIFIED APPROACH for Smart Building: 
            // DevicesOverlay is INSIDE PanPinchContainer, so it automatically zooms/pans with the plan
            // We only need to position devices relative to the plan image WITHOUT any transformation
            var (drawnX, drawnY, drawnW, drawnH) = GetImageDrawnRect();

            Console.WriteLine($"");
            Console.WriteLine($"🏢 PositionDeviceView - SMART BUILDING SIMPLIFIED - Device: {pd.Name}");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════════");
            
            Console.WriteLine($"🖼️ PLAN IMAGE POSITIONING:");
            Console.WriteLine($"   📍 drawnX: {drawnX:F2}");
            Console.WriteLine($"   📍 drawnY: {drawnY:F2}");
            Console.WriteLine($"   📏 drawnW: {drawnW:F2}");
            Console.WriteLine($"   📏 drawnH: {drawnH:F2}");
            
            Console.WriteLine($"🔧 DEVICE MODEL STATE:");
            Console.WriteLine($"   📍 RelativeX: {pd.RelativeX:F4} (should be [0.0, 1.0])");
            Console.WriteLine($"   📍 RelativeY: {pd.RelativeY:F4} (should be [0.0, 1.0])");
            Console.WriteLine($"   📊 Device Scale: {pd.Scale:F4}");
            Console.WriteLine($"   📏 BaseWidthNorm: {pd.BaseWidthNorm:F4}");
            Console.WriteLine($"   📏 BaseHeightNorm: {pd.BaseHeightNorm:F4}");

            // Calculate device center in plan coordinates (NO transformation needed - PanPinchContainer handles it)
            var xCenter = drawnX + pd.RelativeX * drawnW;
            var yCenter = drawnY + pd.RelativeY * drawnH;
            
            Console.WriteLine($"🎯 DEVICE CENTER CALCULATION (plan coordinates):");
            Console.WriteLine($"   🔹 xCenter = {drawnX:F2} + {pd.RelativeX:F4} * {drawnW:F2} = {xCenter:F2}");
            Console.WriteLine($"   🔹 yCenter = {drawnY:F2} + {pd.RelativeY:F4} * {drawnH:F2} = {yCenter:F2}");

            // Use intrinsic template size and scale for plan size adaptation
            const double intrinsicW = 160.0;
            const double intrinsicH = 180.0;

            // Calculate scale based on plan size and user preference (NO PlanScale multiplication!)
            var targetWidth = pd.BaseWidthNorm * drawnW;
            var baseScale = intrinsicW > 0 ? (targetWidth / intrinsicW) : 1.0;
            
            // Apply user's scale multiplier
            var userScaledSize = baseScale * (pd.Scale <= 0 ? 1.0 : pd.Scale);

            Console.WriteLine($"📊 SCALE CALCULATION (plan adaptation only):");
            Console.WriteLine($"   🔹 targetWidth = {pd.BaseWidthNorm:F4} * {drawnW:F2} = {targetWidth:F2}");
            Console.WriteLine($"   🔹 baseScale = {targetWidth:F2} / {intrinsicW:F1} = {baseScale:F4}");
            Console.WriteLine($"   🔹 userScaledSize = {baseScale:F4} * {pd.Scale:F4} = {userScaledSize:F4}");

            // Enforce minimum size for usability
            const double minScale = 0.0125; // Minimum 1.25% size
            var appliedScale = Math.Max(userScaledSize, minScale);

            Console.WriteLine($"🔒 MINIMUM SIZE PROTECTION:");
            Console.WriteLine($"   🔹 appliedScale = Math.Max({userScaledSize:F4}, {minScale:F4}) = {appliedScale:F4}");

            // Position device with center anchor (SIMPLE positioning - no transformation)
            view.AnchorX = 0.5;
            view.AnchorY = 0.5;
            view.Scale = appliedScale;

            var xLeft = xCenter - intrinsicW / 2.0;
            var yTop = yCenter - intrinsicH / 2.0;
            
            Console.WriteLine($"📍 FINAL POSITIONING (PanPinchContainer handles zoom/pan automatically):");
            Console.WriteLine($"   🔹 view.AnchorX: 0.5, view.AnchorY: 0.5");
            Console.WriteLine($"   🔹 view.Scale: {appliedScale:F4}");
            Console.WriteLine($"   🔹 xLeft = {xCenter:F2} - {intrinsicW:F1}/2 = {xLeft:F2}");
            Console.WriteLine($"   🔹 yTop = {yCenter:F2} - {intrinsicH:F1}/2 = {yTop:F2}");
            Console.WriteLine($"   📏 LayoutBounds: ({xLeft:F2}, {yTop:F2}, {intrinsicW:F1}, {intrinsicH:F1})");
            
            // SMART BUILDING: Device stays at fixed position on plan, zooms with plan automatically
            Console.WriteLine($"🏢 SMART BUILDING BEHAVIOR:");
            Console.WriteLine($"   ✅ Device positioned at fixed plan location (door position)");
            Console.WriteLine($"   ✅ Will zoom/pan with plan automatically via PanPinchContainer");
            Console.WriteLine($"   ✅ Manual movement ONLY changes RelativeX/Y, NOT plan state");
            Console.WriteLine($"   ✅ Represents physical door control at building location");

            AbsoluteLayout.SetLayoutBounds(view, new Rect(xLeft, yTop, intrinsicW, intrinsicH));
            AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
            
            Console.WriteLine($"✅ PositionDeviceView COMPLETE - SMART BUILDING READY");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════════");
            Console.WriteLine($"");
        }

        private void OnDeviceMoveRequested(object? sender, PlacedDeviceModel e)
        {
            Console.WriteLine($"");
            Console.WriteLine($"📤 OnDeviceMoveRequested - Device: {e.Name}");
            Console.WriteLine($"   📍 Updated Position: X={e.RelativeX:F6}, Y={e.RelativeY:F6}");
            Console.WriteLine($"   📊 Current Scale: {e.Scale:F4}");
            Console.WriteLine($"   🔄 Triggering save and layout refresh...");
            
            // Position already updated by control; persist and re-layout
            _ = _viewModel?.SaveCurrentFloorAsync();
            InvalidateDevicesLayout();
            
            Console.WriteLine($"   ✅ Save and layout refresh triggered");
            Console.WriteLine($"");
        }

        private void OnDeviceDeleteRequested(object? sender, PlacedDeviceModel e)
        {
            Console.WriteLine($"[MainPage] OnDeviceDeleteRequested called for device: {e?.Name ?? "(null)"}");
            try
            {
                var level = _viewModel?.StructuresVM?.SelectedLevel;
                if (level?.PlacedDevices == null)
                {
                    Console.WriteLine("[MainPage] PlacedDevices collection is null!");
                    return;
                }
                if (e == null)
                {
                    Console.WriteLine("[MainPage] PlacedDeviceModel argument is null!");
                    return;
                }
                // Remove the device from the current floor and persist
                if (level.PlacedDevices.Contains(e))
                {
                    Console.WriteLine($"[MainPage] Removing device from PlacedDevices: {e.Name}");
                    level.PlacedDevices.Remove(e);
                    _ = _viewModel?.SaveCurrentFloorAsync();
                }
                else
                {
                    Console.WriteLine($"[MainPage] Device not found in PlacedDevices: {e.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Exception in OnDeviceDeleteRequested: {ex.Message}");
            }
            finally
            {
                InvalidateDevicesLayout();
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdatePlanIntrinsicSize();
            InvalidateDevicesLayout();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            Console.WriteLine($"🔄 MainPage.OnHandlerChanged - Setting up PlanContainer monitoring");
            
            if (PlanContainer.Content is View content)
            {
                Console.WriteLine($"📱 INITIAL PLAN CONTAINER STATE:");
                Console.WriteLine($"   📏 Content.Scale: {content.Scale:F4}");
                Console.WriteLine($"   🔀 Content.TranslationX: {content.TranslationX:F4}");
                Console.WriteLine($"   🔀 Content.TranslationY: {content.TranslationY:F4}");
                Console.WriteLine($"   📐 Content.Width: {content.Width:F2}");
                Console.WriteLine($"   📐 Content.Height: {content.Height:F2}");
                
                content.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName is nameof(View.Scale) or nameof(View.TranslationX) or nameof(View.TranslationY))
                    {
                        Console.WriteLine($"");
                        Console.WriteLine($"🚨 PLAN CONTAINER PROPERTY CHANGED: {e.PropertyName}");
                        Console.WriteLine($"   📏 Current Scale: {content.Scale:F4}");
                        Console.WriteLine($"   🔀 Current TranslationX: {content.TranslationX:F4}");
                        Console.WriteLine($"   🔀 Current TranslationY: {content.TranslationY:F4}");
                        Console.WriteLine($"   ⚠️ THIS CHANGE AFFECTS ALL DEVICE POSITIONING!");
                        Console.WriteLine($"");
                        
                        // Debounce viewport state updates to prevent excessive calls
                        _viewportUpdateTimer?.Dispose();
                        _viewportUpdateTimer = new Timer((_) => 
                        {
                            try
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    Console.WriteLine($"🔄 Triggering InvalidateDevicesLayout due to Plan Container change...");
                                    InvalidateDevicesLayout();
                                    PersistViewportState();
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[content.PropertyChanged] Timer callback error: {ex.Message}");
                            }
                        }, null, 100, Timeout.Infinite);
                    }
                };

                // Try restore viewport state for current floor
                RestoreViewportState();
            }
            
            PlanContainer.SizeChanged += (s, args) =>
            {
                Console.WriteLine($"📐 PLAN CONTAINER SIZE CHANGED:");
                Console.WriteLine($"   📏 New Size: {PlanContainer.Width:F2} x {PlanContainer.Height:F2}");
                
                UpdatePlanIntrinsicSize();
                // Don't immediately invalidate layout on size changes
                _layoutInvalidationTimer?.Dispose();
                _layoutInvalidationTimer = new Timer((_) => MainThread.BeginInvokeOnMainThread(InvalidateDevicesLayout), null, 100, Timeout.Infinite);
            };

            PlanImage.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(Width) or nameof(Height))
                {
                    Console.WriteLine($"🖼️ PLAN IMAGE SIZE CHANGED: {e.PropertyName}");
                    Console.WriteLine($"   📏 PlanImage.Width: {PlanImage.Width:F2}");
                    Console.WriteLine($"   📏 PlanImage.Height: {PlanImage.Height:F2}");
                    
                    var newWidth = PlanImage.Width;
                    var newHeight = PlanImage.Height;
                    
                    // Only update if the change is significant (avoid micro-changes that cause flickering)
                    if (Math.Abs(newWidth - _planIntrinsicWidth) > 1.0 || Math.Abs(newHeight - _planIntrinsicHeight) > 1.0)
                    {
                        Console.WriteLine($"   🔄 Significant size change detected - updating layout");
                        UpdatePlanIntrinsicSize();
                        
                        // Don't immediately invalidate layout on image size changes
                        _layoutInvalidationTimer?.Dispose();
                        _layoutInvalidationTimer = new Timer((_) => MainThread.BeginInvokeOnMainThread(InvalidateDevicesLayout), null, 100, Timeout.Infinite);
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️ Micro size change ignored to prevent layout thrashing");
                    }
                }
            };

            // Ensure device collection hooks are active for current level when handler is ready
            WireDevicesCollection();
            InvalidateDevicesLayout();
        }

    #endregion

    #region Viewport persistence

        private void PersistViewportState()
        {
            try
            {
                var level = _viewModel?.StructuresVM?.SelectedLevel;
                if (level == null) return;
                if (PlanContainer.Content is View content)
                {
                    level.ViewScale = content.Scale;
                    level.ViewTranslationX = content.TranslationX;
                    level.ViewTranslationY = content.TranslationY;
                    _ = _viewModel?.SaveCurrentFloorAsync();
                }
            }
            catch { }
        }

        private void RestoreViewportState()
        {
            try
            {
                var level = _viewModel?.StructuresVM?.SelectedLevel;
                if (level == null) return;
                if (PlanContainer.Content is View content)
                {
                    if (level.ViewScale is double s && s > 0) content.Scale = s;
                    if (level.ViewTranslationX is double tx) content.TranslationX = tx;
                    if (level.ViewTranslationY is double ty) content.TranslationY = ty;
                }
            }
            catch { }
        }

    #endregion

    #region Footer and tab handlers

    private void SetupFooterEvents()
    {
        if (_viewModel != null)
        {
            Footer.LeftSectionTapped += (s, e) => _viewModel.LeftSectionTappedCommand.Execute(null);
            Footer.CenterButtonTapped += (s, e) => _viewModel.CenterButtonTappedCommand.Execute(null);
            Footer.RightSectionTapped += (s, e) => _viewModel.RightSectionTappedCommand.Execute(null);
        }
    }

    private void OnBackgroundTapped(object? sender, TappedEventArgs e)
    {
        _viewModel?.CloseDropdown();
    }

    private void OnDropdownContentTapped(object? sender, TappedEventArgs e)
    {
        // Prevent the background tap from being triggered when clicking inside dropdown
    }

    private async void OnDropdownItemSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_viewModel?.CurrentActiveTab == null) return;
            var selected = e.CurrentSelection?.FirstOrDefault();
            if (selected is not ReisingerIntelliApp_V4.Models.DropdownItemModel item) return;

            // Clear selection for tap-like behavior
            if (sender is CollectionView cv) cv.SelectedItem = null;

            if (_viewModel.CurrentActiveTab == "Structures")
            {
                // Update selected building and highlight
                _viewModel.SelectedBuildingName = item.Id;
                foreach (var it in _viewModel.DropdownItems)
                    it.IsSelected = it.Id == item.Id;
                // Auto-switch to Levels to show floors of the selected building
                _viewModel.TabTappedCommand.Execute("Levels");
            }
            else if (_viewModel.CurrentActiveTab == "Levels")
            {
                // Select a level for later operations and highlight
                _viewModel.SelectedLevelName = item.Id;
                foreach (var it in _viewModel.DropdownItems)
                    it.IsSelected = it.Id == item.Id;
            }
        }
        catch { }
    }

    private void SetActiveTab(string tabName)
    {
        // Reset all tabs to inactive state
        ResetAllTabs();

        // Create blue gradient for active tab background
        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#20007AFF"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#40007AFF"), Offset = 0.5f },
                new GradientStop { Color = Color.FromArgb("#20007AFF"), Offset = 1.0f }
            }
        };

        switch (tabName)
        {
            case "Structures":
                StructuresLabel.TextColor = Color.FromArgb("#007AFF");
                StructuresUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                StructuresTabBackground.Background = gradientBrush;
                break;
            case "Levels":
                LevelsLabel.TextColor = Color.FromArgb("#007AFF");
                LevelsUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                LevelsTabBackground.Background = gradientBrush;
                break;
            case "WifiDev":
                WifiDevLabel.TextColor = Color.FromArgb("#007AFF");
                WifiDevUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                WifiDevTabBackground.Background = gradientBrush;
                break;
            case "LocalDev":
                LocalDevLabel.TextColor = Color.FromArgb("#007AFF");
                LocalDevUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                LocalDevTabBackground.Background = gradientBrush;
                break;
        }
    }

    private void ResetAllTabs()
    {
        // Reset all tab labels and underlines to inactive state
        var grayColor = Color.FromArgb("#808080");
        var transparent = Colors.Transparent;

        StructuresLabel.TextColor = grayColor;
        StructuresUnderline.BackgroundColor = transparent;
        StructuresTabBackground.Background = null;

        LevelsLabel.TextColor = grayColor;
        LevelsUnderline.BackgroundColor = transparent;
        LevelsTabBackground.Background = null;

        WifiDevLabel.TextColor = grayColor;
        WifiDevUnderline.BackgroundColor = transparent;
        WifiDevTabBackground.Background = null;

        LocalDevLabel.TextColor = grayColor;
        LocalDevUnderline.BackgroundColor = transparent;
        LocalDevTabBackground.Background = null;
    }

    #endregion

    private void AttachPlacementObservers(PlacedDeviceModel pd, View view)
    {
        void PdOnPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PlacedDeviceModel.Scale))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PositionDeviceView((Components.PlacedDeviceControl)view, pd);
                    _ = _viewModel?.SaveCurrentFloorAsync(); // persist Scale per floor
                });
            }
        }
        pd.PropertyChanged -= PdOnPropertyChanged;
        pd.PropertyChanged += PdOnPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up timers to prevent memory leaks and resource conflicts
        try
        {
            _layoutInvalidationTimer?.Dispose();
            _layoutInvalidationTimer = null;
            
            _viewportUpdateTimer?.Dispose();
            _viewportUpdateTimer = null;
            
            Console.WriteLine("🧹 MainPage timers cleaned up on disappearing");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cleaning up timers: {ex.Message}");
        }
        
        // Clean up messaging subscriptions
        try
        {
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, "ForceDeviceLayoutRefresh");
            Console.WriteLine("🧹 MainPage messaging subscriptions cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cleaning up messaging subscriptions: {ex.Message}");
        }
    }
}
