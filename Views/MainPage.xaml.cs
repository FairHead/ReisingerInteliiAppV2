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
    
    // ✅ NEW: Store the actual image aspect ratio (constant regardless of screen rotation)
    private double _imageAspectRatio = 1.0;
    private bool _imageAspectRatioInitialized = false;

    public MainPage(MainPageViewModel viewModel)
    {
        // ⭐ CRITICAL FIX: Set BindingContext BEFORE InitializeComponent
        // This ensures commands are available when DataTemplates render
        _viewModel = viewModel;
        BindingContext = _viewModel;
        System.Diagnostics.Debug.WriteLine("✅ MainPage - BindingContext set BEFORE InitializeComponent");
        
        InitializeComponent();
        
        System.Diagnostics.Debug.WriteLine("✅ MainPage constructor - ViewModel assigned");
        
        _viewModel.AttachViewport(this);
        SetupFooterEvents();
        SetupViewModelEvents();
        
        // ✅ FIX: Wire up CollectionView child events for command execution
        SetupDropdownCardEvents();
        
        // ✅ NEW: Subscribe to PlanImage source changes to get actual image dimensions
        SetupPlanImageSizeTracking();
        
        // Listen for force device layout refresh messages
        #pragma warning disable CS0618 // MessagingCenter is obsolete; suppression until migrated
        MessagingCenter.Subscribe<MainPageViewModel>(this, "ForceDeviceLayoutRefresh", (sender) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Console.WriteLine("🔄 Force device layout refresh requested");
                InvalidateDevicesLayout();
            });
        });
        // Listen for reset request to immediately clear plan and overlay (e.g., My Place)
        MessagingCenter.Subscribe<MainPageViewModel>(this, "ResetPlanAndOverlay", (sender) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Console.WriteLine("🧹 ResetPlanAndOverlay received -> clearing PlanImage and DevicesOverlay");
                PlanImage.Source = null;
                DevicesOverlay?.Children?.Clear();
                DevicesOverlay?.InvalidateMeasure();
            });
        });
        #pragma warning restore CS0618
        
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
                        // If no level selected, clear plan and overlay
                        if (_viewModel?.StructuresVM?.SelectedLevel == null)
                        {
                            PlanImage.Source = null;
                            DevicesOverlay?.Children?.Clear();
                        }
                        else
                        {
                            SyncDevicesOverlay(); // Sync controls when level changes
                        }
                        InvalidateDevicesLayout();
                    });
                }
            };
        }

        // Ensure we are wired to the current level and can layout existing devices immediately
        WireDevicesCollection();
        SyncDevicesOverlay(); // Sync controls on initial load
        InvalidateDevicesLayout();

        // When BindableLayout creates a new visual child, position it right away
        if (DevicesOverlay != null)
        {
            DevicesOverlay.ChildAdded += (sender, args) =>
            {
                // ✅ CRITICAL FIX: Use PlacedDevice property, NOT BindingContext!
                // BindingContext is now PlacedDeviceControlViewModel, not PlacedDeviceModel
                if (args.Element is Components.PlacedDeviceControl ctrl)
                {
                    var pd = ctrl.PlacedDevice;
                    if (pd != null)
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
                }
            };
        }

    System.Diagnostics.Debug.WriteLine("MainPage initialized");
    }

    /// <summary>
    /// ✅ FIX: Wire up event handlers for GradientWifiCardComponent instances created in CollectionView DataTemplate
    /// This ensures commands execute properly even when x:Reference bindings fail in DataTemplate context
    /// </summary>
    private void SetupDropdownCardEvents()
    {
        if (DropdownItemsView == null) return;
        
        // Monitor when children are added to the CollectionView
        DropdownItemsView.ChildAdded += (sender, args) =>
        {
            try
            {
                // Find all GradientWifiCardComponent instances in the visual tree
                if (args.Element is View view)
                {
                    WireDropdownCardEvents(view);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error wiring dropdown card events: {ex.Message}");
            }
        };
        
        // Also wire up any existing children
        try
        {
            foreach (var child in DropdownItemsView.GetVisualTreeDescendants())
            {
                if (child is View view)
                {
                    WireDropdownCardEvents(view);
                }
            }
        }
        catch { }
    }

    private void WireDropdownCardEvents(View view)
    {
        if (view is Components.GradientWifiCardComponent card)
        {
            Console.WriteLine($"✅ Wiring events for GradientWifiCardComponent: {card.DeviceName}");
            
            // Unwire first to prevent duplicates
            card.MonitorClicked -= OnDeviceCard_AddToFloorPlanClicked;
            card.SettingsClicked -= OnDeviceCard_SettingsClicked;
            card.DeleteClicked -= OnDeviceCard_DeleteClicked;
            
            // Wire up events
            card.MonitorClicked += OnDeviceCard_AddToFloorPlanClicked;
            card.SettingsClicked += OnDeviceCard_SettingsClicked;
            card.DeleteClicked += OnDeviceCard_DeleteClicked;
            
            Console.WriteLine($"   ✅ Events wired successfully");
        }
        
        // Recursively check children
        if (view is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is View childView)
                {
                    WireDropdownCardEvents(childView);
                }
            }
        }
    }

    private void OnDeviceCard_AddToFloorPlanClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Components.GradientWifiCardComponent card && card.CommandParameter is DropdownItemModel item)
            {
                Console.WriteLine($"✅ AddToFloorPlan clicked for: {item.Text}");
                _viewModel?.AddDeviceToFloorPlanCommand?.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in OnDeviceCard_AddToFloorPlanClicked: {ex.Message}");
        }
    }

    private void OnDeviceCard_SettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Components.GradientWifiCardComponent card && card.CommandParameter is DropdownItemModel item)
            {
                Console.WriteLine($"✅ SettingsClicked event received for: {item.Text}");
                _viewModel?.CardSettingsCommand?.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in OnDeviceCard_SettingsClicked: {ex.Message}");
        }
    }

    private void OnDeviceCard_DeleteClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Components.GradientWifiCardComponent card && card.CommandParameter is DropdownItemModel item)
            {
                Console.WriteLine($"✅ DeleteClicked event received for: {item.Text}");
                _viewModel?.DeleteDeviceFromDropdownCommand?.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in OnDeviceCard_DeleteClicked: {ex.Message}");
        }
    }

    private void SetupViewModelEvents()
    {
        if (_viewModel != null)
        {
            _viewModel.TabActivated += (sender, tabName) =>
            {
                SetActiveTab(tabName);
                
                // ✅ Re-wire dropdown card events when tab changes and new items are loaded
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                SetupDropdownCardEvents();
                            });
                        });
                    }
                    catch { }
                });
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

    public new double Scale => PlanImage?.Scale ?? 1.0;
    public new double TranslationX => PlanImage?.TranslationX ?? 0.0;
    public new double TranslationY => PlanImage?.TranslationY ?? 0.0;

        public Point ScreenToPlan(Point screenPoint)
        {
            if (!IsPlanReady || PlanImage == null)
                return new Point(PlanWidth / 2, PlanHeight / 2);

            // Map screen to container content coordinates considering translation/scale
            // Use PlanImage for scale/translation
            if (!IsPlanReady || PlanImage == null)
                return new Point(PlanWidth / 2, PlanHeight / 2);

            // Treat screenPoint as container-local coordinates for this mapping
            var localX = screenPoint.X;
            var localY = screenPoint.Y;

            // Undo translation
            localX -= PlanImage.TranslationX;
            localY -= PlanImage.TranslationY;

            // Undo scale
            var unscaledX = localX / (PlanImage.Scale <= 0 ? 1 : PlanImage.Scale);
            var unscaledY = localY / (PlanImage.Scale <= 0 ? 1 : PlanImage.Scale);

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
            // Only update if we haven't captured the actual image dimensions yet
            if (!_imageAspectRatioInitialized && PlanImage != null && PlanImage.Width > 0 && PlanImage.Height > 0)
            {
                _planIntrinsicWidth = PlanImage.Width;
                _planIntrinsicHeight = PlanImage.Height;
                Console.WriteLine($"📏 UpdatePlanIntrinsicSize (fallback): {_planIntrinsicWidth:F0} x {_planIntrinsicHeight:F0}");
            }
            // If already initialized, DON'T update - keep the original image dimensions!
            else if (_imageAspectRatioInitialized)
            {
                Console.WriteLine($"📏 UpdatePlanIntrinsicSize: Keeping initialized values {_planIntrinsicWidth:F0} x {_planIntrinsicHeight:F0}");
            }
        }

        /// <summary>
        /// ✅ FIXED: Gets the rectangle where the image is actually drawn within the view.
        /// Uses _planIntrinsicWidth/_planIntrinsicHeight which are the ACTUAL image dimensions,
        /// not the view dimensions. This ensures correct positioning during screen rotation.
        /// </summary>
        private (double x, double y, double w, double h) GetImageDrawnRect()
        {
            if (PlanImage == null)
                return (0, 0, PlanContainer?.Width ?? 0, PlanContainer?.Height ?? 0);
            
            var viewW = PlanImage.Width;
            var viewH = PlanImage.Height;
            
            if (viewW <= 0 || viewH <= 0)
                return (0, 0, PlanContainer?.Width ?? 0, PlanContainer?.Height ?? 0);
            
            // ✅ CRITICAL: Use the stored intrinsic dimensions
            // These should be the ACTUAL image pixel dimensions, captured when the image loaded
            // NOT the view dimensions which change with screen rotation
            var imgW = _planIntrinsicWidth;
            var imgH = _planIntrinsicHeight;
            
            // If we don't have valid intrinsic dimensions yet, use view dimensions temporarily
            if (imgW <= 0 || imgH <= 0)
            {
                Console.WriteLine($"⚠️ GetImageDrawnRect: No intrinsic dimensions yet, using view dimensions");
                imgW = viewW;
                imgH = viewH;
            }
            
            // ✅ AspectFit: The image is scaled to fit within the view while maintaining aspect ratio
            // Calculate how the image fits within the current view
            var scaleToFit = Math.Min(viewW / imgW, viewH / imgH);
            var drawnW = imgW * scaleToFit;
            var drawnH = imgH * scaleToFit;
            
            // Image is centered within the view (AspectFit behavior)
            var offsetX = (viewW - drawnW) / 2;
            var offsetY = (viewH - drawnH) / 2;
            
            Console.WriteLine($"📐 GetImageDrawnRect:");
            Console.WriteLine($"   📏 View: {viewW:F2} x {viewH:F2}");
            Console.WriteLine($"   🖼️ Intrinsic Image: {imgW:F2} x {imgH:F2}");
            Console.WriteLine($"   🔄 ScaleToFit: {scaleToFit:F4}");
            Console.WriteLine($"   ✅ Drawn: offset({offsetX:F2}, {offsetY:F2}) size({drawnW:F2} x {drawnH:F2})");
            
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
                        Console.WriteLine("[PlacedDevices_CollectionChanged] -> SyncDevicesOverlay and InvalidateDevicesLayout (debounced)");
                        SyncDevicesOverlay(); // Sync first to add/remove controls
                        InvalidateDevicesLayout(); // Then update positions
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
                        if (_viewModel != null)
                            await _viewModel.SaveCurrentFloorAsync();
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
                // Immediate layout update for all property changes to ensure visual feedback
                Console.WriteLine($"🔄 PlacedDevice_PropertyChanged: {e.PropertyName} - triggering immediate layout update");
                
                try
                {
                    // Cancel any pending timer and execute immediately
                    _layoutInvalidationTimer?.Dispose();
                    _layoutInvalidationTimer = null;
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Console.WriteLine($"🎯 Immediate InvalidateDevicesLayout for {e.PropertyName}");
                        InvalidateDevicesLayout();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PlacedDevice_PropertyChanged] Immediate update error: {ex.Message}");
                }
            }
        }

        private void SyncDevicesOverlay()
        {
            if (DevicesOverlay == null || _viewModel?.StructuresVM?.SelectedLevel?.PlacedDevices == null)
            {
                Console.WriteLine("[SyncDevicesOverlay] DevicesOverlay or PlacedDevices is null. Skipping.");
                // Additionally, ensure overlay is empty if no level selected
                if (_viewModel?.StructuresVM?.SelectedLevel == null)
                {
                    DevicesOverlay?.Children?.Clear();
                }
                return;
            }

            Console.WriteLine($"[SyncDevicesOverlay] Syncing {_viewModel.StructuresVM.SelectedLevel.PlacedDevices.Count} PlacedDevices with {DevicesOverlay.Children.Count} UI controls");

            // Get existing controls and current models
            var existingControls = DevicesOverlay.Children.OfType<Components.PlacedDeviceControl>().ToList();
            var currentModels = _viewModel.StructuresVM.SelectedLevel.PlacedDevices.ToList();

            // Create lookup dictionary for current models by PlacedDeviceId
            var currentModelsById = currentModels.ToDictionary(m => m.PlacedDeviceId, m => m);

            // Update existing controls with new model instances (don't recreate!)
            foreach (var control in existingControls)
            {
                var currentModel = control.PlacedDevice;
                if (currentModel != null && currentModelsById.TryGetValue(currentModel.PlacedDeviceId, out var updatedModel))
                {
                    // ✅ FIX: Only update PlacedDevice property, NOT BindingContext!
                    // BindingContext is set in PlacedDeviceControl constructor to PlacedDeviceControlViewModel
                    // Setting it here would override the ViewModel and break all bindings
                    control.PlacedDevice = updatedModel;
                    Console.WriteLine($"[SyncDevicesOverlay] Updated control for device: {updatedModel.Name}");
                    // Remove from lookup so we don't add it again
                    currentModelsById.Remove(currentModel.PlacedDeviceId);
                }
                else
                {
                    // Model no longer exists, remove the control
                    if (currentModel != null)
                    {
                        Console.WriteLine($"[SyncDevicesOverlay] Removing control for device: {currentModel.Name}");
                    }
                    
                    // ✅ Unwire ALL event handlers before removing (including MoveModeChanged)
                    UnwireDeviceControl(control);
                    
                    DevicesOverlay.Children.Remove(control);
                    if (currentModel != null)
                    {
                        Console.WriteLine($"[SyncDevicesOverlay] All event handlers unwired for device: {currentModel.Name}");
                    }
                }
            }

            // Add controls for remaining new models
            foreach (var model in currentModelsById.Values)
            {
                Console.WriteLine($"[SyncDevicesOverlay] Adding control for device: {model.Name}");
                var control = new Components.PlacedDeviceControl
                {
                    // ✅ CRITICAL FIX: Do NOT set BindingContext here!
                    // PlacedDeviceControl constructor already sets BindingContext = PlacedDeviceControlViewModel
                    // Setting it here would override the ViewModel and break all bindings
                    PlacedDevice = model  // ✅ Only set PlacedDevice - this triggers OnPlacedDeviceChanged in code-behind
                };
                
                // ✅ Wire up ALL event handlers for the new control
                WireDeviceControl(control);
                
                DevicesOverlay.Children.Add(control);
                Console.WriteLine($"[SyncDevicesOverlay] All event handlers wired for device: {model.Name}");
            }

            Console.WriteLine($"[SyncDevicesOverlay] Sync complete. DevicesOverlay now has {DevicesOverlay.Children.Count} controls");
        }

        /// <summary>
        /// ✅ Centralized event wiring for PlacedDeviceControl to prevent duplicates and ensure all events are registered
        /// </summary>
        private void WireDeviceControl(Components.PlacedDeviceControl control)
        {
            // Remove ALL handlers first to prevent duplicates
            control.AddDeviceRequested -= OnDeviceIncreaseRequested;
            control.RemoveDeviceRequested -= OnDeviceDecreaseRequested;
            control.DeleteDeviceRequested -= OnDeviceDeleteRequested;
            control.MoveDeviceRequested -= OnDeviceMoveRequested;
            control.ModeChangedRequested -= OnDeviceModeChangedRequested;
            
            // Wire up all handlers
            control.AddDeviceRequested += OnDeviceIncreaseRequested;
            control.RemoveDeviceRequested += OnDeviceDecreaseRequested;
            control.DeleteDeviceRequested += OnDeviceDeleteRequested;
            control.MoveDeviceRequested += OnDeviceMoveRequested;
            control.ModeChangedRequested += OnDeviceModeChangedRequested;
        }

        /// <summary>
        /// ✅ Centralized event unwiring for PlacedDeviceControl to ensure clean removal
        /// </summary>
        private void UnwireDeviceControl(Components.PlacedDeviceControl control)
        {
            control.AddDeviceRequested -= OnDeviceIncreaseRequested;
            control.RemoveDeviceRequested -= OnDeviceDecreaseRequested;
            control.DeleteDeviceRequested -= OnDeviceDeleteRequested;
            control.MoveDeviceRequested -= OnDeviceMoveRequested;
            control.ModeChangedRequested -= OnDeviceModeChangedRequested;
        }

        private void InvalidateDevicesLayout()
        {
            if (DevicesOverlay == null || _viewModel?.StructuresVM?.SelectedLevel?.PlacedDevices == null)
            {
                Console.WriteLine("[InvalidateDevicesLayout] DevicesOverlay or PlacedDevices is null. Skipping.");
                // Also clear visual children when no level is active so no stray devices are shown
                if (_viewModel?.StructuresVM?.SelectedLevel == null)
                {
                    DevicesOverlay?.Children?.Clear();
                }
                return;
            }

            Console.WriteLine($"[InvalidateDevicesLayout] Processing {DevicesOverlay.Children.Count} visual children");

            // Iterate through visual children and position them based on the PlacedDevice property
            foreach (var child in DevicesOverlay.Children.OfType<Components.PlacedDeviceControl>())
            {
                var pd = child.PlacedDevice;
                
                if (pd == null)
                {
                    Console.WriteLine("[InvalidateDevicesLayout] Skipping child with null PlacedDevice.");
                    continue;
                }
                
                Console.WriteLine($"[InvalidateDevicesLayout] Processing device: {pd.Name}, Scale: {pd.Scale:F3}, X: {pd.RelativeX:F6}, Y: {pd.RelativeY:F6}");
                
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
            if (!IsPlanReady || PlanContainer == null || PlanImage == null) return;

            // ✅ DevicesOverlay ist jetzt INNERHALB des PanPinchContainer!
            // Transformationen (Zoom/Pan) werden automatisch vom PanPinchContainer übernommen.
            // Wir müssen nur die Position relativ zum PlanImage berechnen.
            var (drawnX, drawnY, drawnW, drawnH) = GetImageDrawnRect();

            Console.WriteLine($"");
            Console.WriteLine($"🏢 PositionDeviceView - Device: {pd.Name}");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════════");
            
            Console.WriteLine($"🖼️ PLAN IMAGE POSITIONING:");
            Console.WriteLine($"   📍 drawnX: {drawnX:F2}");
            Console.WriteLine($"   📍 drawnY: {drawnY:F2}");
            Console.WriteLine($"   📏 drawnW: {drawnW:F2}");
            Console.WriteLine($"   📏 drawnH: {drawnH:F2}");
            Console.WriteLine($"   📏 _planIntrinsicWidth: {_planIntrinsicWidth:F2}");
            Console.WriteLine($"   📏 _planIntrinsicHeight: {_planIntrinsicHeight:F2}");
            
            Console.WriteLine($"🔧 DEVICE MODEL STATE:");
            Console.WriteLine($"   📍 RelativeX: {pd.RelativeX:F4} (should be [0.0, 1.0])");
            Console.WriteLine($"   📍 RelativeY: {pd.RelativeY:F4} (should be [0.0, 1.0])");
            Console.WriteLine($"   📊 Device Scale: {pd.Scale:F4}");
            Console.WriteLine($"   📏 BaseWidthNorm: {pd.BaseWidthNorm:F4}");
            Console.WriteLine($"   📏 BaseHeightNorm: {pd.BaseHeightNorm:F4}");

            // Calculate device center in plan coordinates
            // DevicesOverlay ist im selben Koordinatensystem wie PlanImage (beide sind Kinder des PanPinchContainer)
            var xCenter = drawnX + pd.RelativeX * drawnW;
            var yCenter = drawnY + pd.RelativeY * drawnH;
            
            Console.WriteLine($"🎯 DEVICE CENTER CALCULATION (plan coordinates):");
            Console.WriteLine($"   🔹 xCenter = {drawnX:F2} + {pd.RelativeX:F4} * {drawnW:F2} = {xCenter:F2}");
            Console.WriteLine($"   🔹 yCenter = {drawnY:F2} + {pd.RelativeY:F4} * {drawnH:F2} = {yCenter:F2}");

            // ✅ IMPORTANT: Container sizes must match the XAML definitions!
            // MainContainer in PlacedDeviceControl.xaml: 600x600
            // Card (Border) is positioned at (175, 175) with width 250 and auto height
            const double cardIntrinsicW = 250.0;  // Actual card width from XAML
            const double containerW = 600.0;       // MainContainer size from XAML
            const double containerH = 600.0;       // MainContainer size from XAML

            // ✅ CRITICAL FIX: Scale device size PROPORTIONAL to drawn plan size
            // This ensures devices shrink/grow with the plan during rotation
            // Use drawnW as the reference (the actual drawn plan width on screen)
            var targetWidth = pd.BaseWidthNorm * drawnW; // desired visible card width = percentage of drawn plan width
            var baseScale = cardIntrinsicW > 0 ? (targetWidth / cardIntrinsicW) : 1.0;

            // Apply user's scale multiplier (scales the whole container, and thus the card inside it)
            var userScaledSize = baseScale * (pd.Scale <= 0 ? 1.0 : pd.Scale);

            Console.WriteLine($"📊 SCALE CALCULATION (proportional to drawn plan):");
            Console.WriteLine($"   🔹 drawnW = {drawnW:F2} (actual plan width on screen - changes with rotation!)");
            Console.WriteLine($"   🔹 cardIntrinsicW = {cardIntrinsicW:F2} (from XAML)");
            Console.WriteLine($"   🔹 containerW/H = {containerW:F2} (from XAML)");
            Console.WriteLine($"   🔹 targetWidth = {pd.BaseWidthNorm:F4} * {drawnW:F2} = {targetWidth:F2}");
            Console.WriteLine($"   🔹 baseScale = {targetWidth:F2} / {cardIntrinsicW:F1} = {baseScale:F4}");
            Console.WriteLine($"   🔹 userScaledSize = {baseScale:F4} * {pd.Scale:F4} = {userScaledSize:F4}");

            // Enforce minimum size for usability
            const double minScale = 0.15; // Minimum 15% size (increased to ensure buttons stay visible)
            var appliedScale = Math.Max(userScaledSize, minScale);

            Console.WriteLine($"🔒 MINIMUM SIZE PROTECTION:");
            Console.WriteLine($"   🔹 appliedScale = Math.Max({userScaledSize:F4}, {minScale:F4}) = {appliedScale:F4}");

            // Position device with center anchor
            view.AnchorX = 0.5;
            view.AnchorY = 0.5;
            view.Scale = appliedScale;

            // Center the 600x600 container at the computed device center
            var xLeft = xCenter - containerW / 2.0;
            var yTop = yCenter - containerH / 2.0;
            
            Console.WriteLine($"📍 FINAL POSITIONING:");
            Console.WriteLine($"   🔹 view.AnchorX: 0.5, view.AnchorY: 0.5");
            Console.WriteLine($"   🔹 view.Scale: {appliedScale:F4}");
            Console.WriteLine($"   🔹 xLeft = {xCenter:F2} - {containerW:F1}/2 = {xLeft:F2}");
            Console.WriteLine($"   🔹 yTop = {yCenter:F2} - {containerH:F1}/2 = {yTop:F2}");
            Console.WriteLine($"   📏 LayoutBounds: ({xLeft:F2}, {yTop:F2}, {containerW:F1}, {containerH:F1})");
            
            Console.WriteLine($"🏢 ROTATION-AWARE BEHAVIOR:");
            Console.WriteLine($"   ✅ Device positioned at fixed plan location (door position)");
            Console.WriteLine($"   ✅ Device size scales WITH the plan (portrait/landscape adaptive)");
            Console.WriteLine($"   ✅ DevicesOverlay ist INNERHALB des PanPinchContainer");
            Console.WriteLine($"   ✅ Zoom/Pan Transformationen werden automatisch übernommen!");
            Console.WriteLine($"   ✅ MoveMode: Pfeilbuttons ändern RelativeX/Y für Neupositionierung");

            // Use the full container size so all interactive buttons are inside the hit area
            AbsoluteLayout.SetLayoutBounds(view, new Rect(xLeft, yTop, containerW, containerH));
            AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
            
            // Nudge layout system to apply new bounds immediately
            try
            {
                view.InvalidateMeasure();
                DevicesOverlay?.InvalidateMeasure();
            }
            catch { }
            
            Console.WriteLine($"✅ PositionDeviceView COMPLETE - ROTATION AWARE");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════════");
            Console.WriteLine($"");
        }

        private void OnDeviceMoveRequested(object? sender, PlacedDeviceModel e)
        {
            Console.WriteLine($"");
            Console.WriteLine($"📤 OnDeviceMoveRequested - Device: {e.Name}");
            Console.WriteLine($"   📍 Updated Position: X={e.RelativeX:F6}, Y={e.RelativeY:F6}");
            Console.WriteLine($"   📊 Current Scale: {e.Scale:F4}");
            Console.WriteLine($"   💾 Triggering save only (position already updated)...");
            
            // Position is already updated by the PlacedDeviceControl - just save
            _ = _viewModel?.SaveCurrentFloorAsync();
            
            Console.WriteLine($"   ✅ Save triggered - layout handled by PropertyChanged");
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

        private void OnDeviceModeChangedRequested(object? sender, PlacedDeviceModel e)
        {
            try
            {
                Console.WriteLine($"[MainPage] OnDeviceModeChangedRequested for device: {e?.Name ?? "(null)"}");
                // Persist current floor with updated mode flags
                _ = _viewModel?.SaveCurrentFloorAsync();
                // UI is bound to model; triggers should update automatically
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Exception in OnDeviceModeChangedRequested: {ex.Message}");
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
            
            // BindingContext is now set in constructor BEFORE InitializeComponent
            // No need to set it again here - commands are already bound correctly
            System.Diagnostics.Debug.WriteLine("✅ OnHandlerChanged: BindingContext already set in constructor");
            
            Console.WriteLine($"🔄 MainPage.OnHandlerChanged - Setting up PlanContainer monitoring");
            
            if (PlanImage != null)
            {
                Console.WriteLine($"📱 INITIAL PLAN IMAGE STATE:");
                Console.WriteLine($"   📏 Scale: {PlanImage.Scale:F4}");
                Console.WriteLine($"   🔀 TranslationX: {PlanImage.TranslationX:F4}");
                Console.WriteLine($"   🔀 TranslationY: {PlanImage.TranslationY:F4}");
                Console.WriteLine($"   📐 Width: {PlanImage.Width:F2}");
                Console.WriteLine($"   📐 Height: {PlanImage.Height:F2}");

                PlanImage.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName is nameof(View.Scale) or nameof(View.TranslationX) or nameof(View.TranslationY))
                    {
                        Console.WriteLine("");
                        Console.WriteLine($"🚨 PLAN IMAGE PROPERTY CHANGED: {e.PropertyName}");
                        Console.WriteLine($"   📏 Current Scale: {PlanImage.Scale:F4}");
                        Console.WriteLine($"   🔀 Current TranslationX: {PlanImage.TranslationX:F4}");
                        Console.WriteLine($"   🔀 Current TranslationY: {PlanImage.TranslationY:F4}");
                        Console.WriteLine($"   ⚠️ THIS CHANGE AFFECTS ALL DEVICE POSITIONING!");
                        Console.WriteLine("", "background-color:yellow");

                        // Debounce viewport state updates to prevent excessive calls
                        _viewportUpdateTimer?.Dispose();
                        _viewportUpdateTimer = new Timer((_) =>
                        {
                            try
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    Console.WriteLine($"🔄 Triggering InvalidateDevicesLayout due to Plan Image change...");
                                    InvalidateDevicesLayout();
                                    PersistViewportState();
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[PlanImage.PropertyChanged] Timer callback error: {ex.Message}");
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

            if (PlanImage == null) return;
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
                if (PlanImage != null)
                {
                    level.ViewScale = PlanImage.Scale;
                    level.ViewTranslationX = PlanImage.TranslationX;
                    level.ViewTranslationY = PlanImage.TranslationY;
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
                if (PlanImage != null)
                {
                    if (level.ViewScale is double s && s > 0) PlanImage.Scale = s;
                    if (level.ViewTranslationX is double tx) PlanImage.TranslationX = tx;
                    if (level.ViewTranslationY is double ty) PlanImage.TranslationY = ty;
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

    private void OnDropdownItemSelected(object sender, SelectionChangedEventArgs e)
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
                // Check if the same structure is selected again (toggle off)
                if (!string.IsNullOrEmpty(_viewModel.SelectedBuildingName) && 
                    _viewModel.SelectedBuildingName == item.Id)
                {
                    // Toggle off: deselect structure and close bauplan
                    _viewModel.SelectedBuildingName = null;
                    _viewModel.SelectedLevelName = null;
                    
                    // Clear all selections in dropdown
                    foreach (var it in _viewModel.DropdownItems)
                        it.IsSelected = false;
                    
                    // Reset StructuresVM selection state
                    _viewModel.StructuresVM.SelectedBuilding = null;
                    _viewModel.StructuresVM.SelectedLevel = null;
                    // Clear plan and devices overlay immediately
                    PlanImage.Source = null;
                    DevicesOverlay?.Children?.Clear();
                    
                    // Close dropdown
                    _viewModel.CloseDropdown();
                    return;
                }
                
                // Select new structure
                _viewModel.SelectedBuildingName = item.Id;
                foreach (var it in _viewModel.DropdownItems)
                    it.IsSelected = it.Id == item.Id;
                
                // Auto-switch to Levels to show floors of the selected building
                _viewModel.TabTappedCommand.Execute("Levels");
            }
            else if (_viewModel.CurrentActiveTab == "Levels")
            {
                // Check if the same level is selected again (toggle off)
                if (!string.IsNullOrEmpty(_viewModel.SelectedLevelName) && 
                    _viewModel.SelectedLevelName == item.Id)
                {
                    // Toggle off: deselect level and close bauplan
                    _viewModel.SelectedLevelName = null;
                    
                    // Clear all selections in dropdown
                    foreach (var it in _viewModel.DropdownItems)
                        it.IsSelected = false;
                    
                    // Reset StructuresVM level selection but keep building selected
                    _viewModel.StructuresVM.SelectedLevel = null;
                    // Clear plan and devices overlay immediately
                    PlanImage.Source = null;
                    DevicesOverlay?.Children?.Clear();
                    DevicesOverlay?.InvalidateMeasure();
                    
                    // Close dropdown
                    _viewModel.CloseDropdown();
                    return;
                }
                
                // Select new level
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
                StructuresLabel.Style = (Application.Current?.Resources["TabLabelActive"] as Style) ?? StructuresLabel.Style;
                StructuresUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                StructuresTabBackground.Background = gradientBrush;
                break;

            case "Levels":
                // Only allow Level tab styling if levels are available (not disabled)
                if (_viewModel?.CanModifyLevelTabStyle == true)
                {
                    LevelsLabel.Style = (Application.Current?.Resources["TabLabelActive"] as Style) ?? LevelsLabel.Style;
                    LevelsUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                    LevelsTabBackground.Background = gradientBrush;
                }
                // If disabled, Level tab keeps its disabled style (red-transparent)
                break;
            case "WifiDev":
                WifiDevLabel.Style = (Application.Current?.Resources["TabLabelActive"] as Style) ?? WifiDevLabel.Style;
                WifiDevUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                WifiDevTabBackground.Background = gradientBrush;
                break;
            case "LocalDev":
                LocalDevLabel.Style = (Application.Current?.Resources["TabLabelActive"] as Style) ?? LocalDevLabel.Style;
                LocalDevUnderline.BackgroundColor = Color.FromArgb("#007AFF");
                LocalDevTabBackground.Background = gradientBrush;
                break;
        }
    }

    private void ResetAllTabs()
    {
        // Reset all tab labels and underlines to inactive state
        var transparent = Colors.Transparent;

        // Use Style instead of direct TextColor to respect XAML DataTriggers
    StructuresLabel.Style = (Application.Current?.Resources["TabLabel"] as Style) ?? StructuresLabel.Style; 
        StructuresUnderline.BackgroundColor = transparent;
        StructuresTabBackground.Background = null;

        // Only reset Level tab style if levels are available (not permanently disabled)
        // If disabled, Level tab keeps its disabled style (red-transparent) via XAML DataTrigger
        if (_viewModel?.CanModifyLevelTabStyle == true)
        {
            LevelsLabel.Style = (Application.Current?.Resources["TabLabel"] as Style) ?? LevelsLabel.Style;
        }
        LevelsUnderline.BackgroundColor = transparent;
        LevelsTabBackground.Background = null;

        WifiDevLabel.Style = (Application.Current?.Resources["TabLabel"] as Style) ?? WifiDevLabel.Style;
        WifiDevUnderline.BackgroundColor = transparent;
        WifiDevTabBackground.Background = null;

        LocalDevLabel.Style = (Application.Current?.Resources["TabLabel"] as Style) ?? LocalDevLabel.Style;
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
            #pragma warning disable CS0618
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, "ForceDeviceLayoutRefresh");
            MessagingCenter.Unsubscribe<MainPageViewModel>(this, "ResetPlanAndOverlay");
            #pragma warning restore CS0618
            Console.WriteLine("🧹 MainPage messaging subscriptions cleaned up");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cleaning up messaging subscriptions: {ex.Message}");
        }
    }
    
    /// <summary>
    /// ✅ NEW: Track actual image dimensions when a new image is loaded
    /// This ensures device positions are calculated relative to the IMAGE, not the view
    /// </summary>
    private void SetupPlanImageSizeTracking()
    {
        if (PlanImage == null) return;
        
        // When source changes, try to get actual image dimensions
        PlanImage.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(Image.Source) && PlanImage.Source != null)
            {
                // Reset flag to re-read dimensions for new image
                _imageAspectRatioInitialized = false;
                
                Console.WriteLine($"🖼️ PlanImage.Source changed - attempting to read actual image dimensions...");
                
                // Small delay to allow image to load
                await Task.Delay(100);
                
                // Try to get actual image dimensions from the source
                await TryUpdateActualImageDimensions();
            }
        };
        
        // Also track when the image is rendered for the first time
        PlanImage.SizeChanged += async (s, e) =>
        {
            if (!_imageAspectRatioInitialized && PlanImage.Source != null && PlanImage.Width > 0 && PlanImage.Height > 0)
            {
                await TryUpdateActualImageDimensions();
            }
        };
    }
    
    /// <summary>
    /// ✅ NEW: Attempts to get the actual intrinsic dimensions of the loaded image
    /// </summary>
    private async Task TryUpdateActualImageDimensions()
    {
        try
        {
            if (PlanImage?.Source == null) return;
            
            // For FileImageSource, we can read the file
            if (PlanImage.Source is FileImageSource fileSource && !string.IsNullOrEmpty(fileSource.File))
            {
                var filePath = fileSource.File;
                Console.WriteLine($"📂 Attempting to read dimensions from: {filePath}");
                
                // Try to read image info from file
                if (File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    await ReadImageDimensionsFromStream(stream);
                }
            }
            else if (PlanImage.Source is StreamImageSource streamSource)
            {
                Console.WriteLine($"📂 StreamImageSource detected - using view dimensions as fallback");
                // For stream sources, fall back to view dimensions
                FallbackToViewDimensions();
            }
            else
            {
                Console.WriteLine($"📂 Unknown source type: {PlanImage.Source.GetType().Name} - using view dimensions as fallback");
                FallbackToViewDimensions();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error reading image dimensions: {ex.Message}");
            FallbackToViewDimensions();
        }
    }
    
    /// <summary>
    /// ✅ NEW: Read image dimensions from a stream (PNG/JPEG header parsing)
    /// </summary>
    private async Task ReadImageDimensionsFromStream(Stream stream)
    {
        try
        {
            // Read first bytes to detect format and dimensions
            var header = new byte[24];
            await stream.ReadAsync(header, 0, 24);
            
            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                // PNG: Width is at bytes 16-19, Height at 20-23 (big-endian)
                var width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                var height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                
                if (width > 0 && height > 0)
                {
                    _planIntrinsicWidth = width;
                    _planIntrinsicHeight = height;
                    _imageAspectRatio = (double)width / height;
                    _imageAspectRatioInitialized = true;
                    
                    Console.WriteLine($"✅ PNG dimensions read: {width}x{height}, AspectRatio: {_imageAspectRatio:F4}");
                    
                    // Refresh layout with correct dimensions
                    MainThread.BeginInvokeOnMainThread(InvalidateDevicesLayout);
                    return;
                }
            }
            
            // JPEG signature: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                Console.WriteLine($"📷 JPEG detected - using view dimensions (JPEG parsing complex)");
                FallbackToViewDimensions();
                return;
            }
            
            Console.WriteLine($"❓ Unknown image format - using view dimensions");
            FallbackToViewDimensions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error parsing image header: {ex.Message}");
            FallbackToViewDimensions();
        }
    }
    
    /// <summary>
    /// ✅ NEW: Fallback to using view dimensions but capture aspect ratio
    /// </summary>
    private void FallbackToViewDimensions()
    {
        if (PlanImage != null && PlanImage.Width > 0 && PlanImage.Height > 0)
        {
            _planIntrinsicWidth = PlanImage.Width;
            _planIntrinsicHeight = PlanImage.Height;
            _imageAspectRatio = PlanImage.Width / PlanImage.Height;
            _imageAspectRatioInitialized = true;
            
            Console.WriteLine($"📏 Using view dimensions as fallback: {_planIntrinsicWidth:F0}x{_planIntrinsicHeight:F0}");
        }
    }
}
