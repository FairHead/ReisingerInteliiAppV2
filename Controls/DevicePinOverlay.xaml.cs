using ReisingerIntelliApp_V4.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ReisingerIntelliApp_V4.Controls;

public partial class DevicePinOverlay : ContentView
{
    public static readonly BindableProperty PlacedDevicesProperty =
        BindableProperty.Create(nameof(PlacedDevices), typeof(ObservableCollection<PlacedDeviceModel>), typeof(DevicePinOverlay),
            propertyChanged: OnPlacedDevicesChanged);

    public static readonly BindableProperty PinTappedCommandProperty =
        BindableProperty.Create(nameof(PinTappedCommand), typeof(ICommand), typeof(DevicePinOverlay));

    public static readonly BindableProperty PinDraggedCommandProperty =
        BindableProperty.Create(nameof(PinDraggedCommand), typeof(ICommand), typeof(DevicePinOverlay));

    public static readonly BindableProperty ContainerWidthProperty =
        BindableProperty.Create(nameof(ContainerWidth), typeof(double), typeof(DevicePinOverlay), 1.0,
            propertyChanged: OnContainerSizeChanged);

    public static readonly BindableProperty ContainerHeightProperty =
        BindableProperty.Create(nameof(ContainerHeight), typeof(double), typeof(DevicePinOverlay), 1.0,
            propertyChanged: OnContainerSizeChanged);

    private readonly Dictionary<string, DevicePinWidget> _pinWidgets = new();
    private DevicePinWidget? _draggingPin;
    private Point _dragStartPoint;
    private bool _isDragging;

    public DevicePinOverlay()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    public ObservableCollection<PlacedDeviceModel>? PlacedDevices
    {
        get => (ObservableCollection<PlacedDeviceModel>?)GetValue(PlacedDevicesProperty);
        set => SetValue(PlacedDevicesProperty, value);
    }

    public ICommand? PinTappedCommand
    {
        get => (ICommand?)GetValue(PinTappedCommandProperty);
        set => SetValue(PinTappedCommandProperty, value);
    }

    public ICommand? PinDraggedCommand
    {
        get => (ICommand?)GetValue(PinDraggedCommandProperty);
        set => SetValue(PinDraggedCommandProperty, value);
    }

    public double ContainerWidth
    {
        get => (double)GetValue(ContainerWidthProperty);
        set => SetValue(ContainerWidthProperty, value);
    }

    public double ContainerHeight
    {
        get => (double)GetValue(ContainerHeightProperty);
        set => SetValue(ContainerHeightProperty, value);
    }

    private static void OnPlacedDevicesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinOverlay overlay)
        {
            overlay.RefreshPins();
        }
    }

    private static void OnContainerSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DevicePinOverlay overlay)
        {
            overlay.UpdatePinPositions();
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        ContainerWidth = Width;
        ContainerHeight = Height;
    }

    private void RefreshPins()
    {
        // Clear existing pins
        PinContainer.Children.Clear();
        _pinWidgets.Clear();

        if (PlacedDevices == null) return;

        // Add pins for each placed device
        foreach (var placedDevice in PlacedDevices)
        {
            CreatePinWidget(placedDevice);
        }

        // Subscribe to collection changes
        if (PlacedDevices is ObservableCollection<PlacedDeviceModel> observableCollection)
        {
            observableCollection.CollectionChanged -= OnPlacedDevicesCollectionChanged;
            observableCollection.CollectionChanged += OnPlacedDevicesCollectionChanged;
        }
    }

    private void OnPlacedDevicesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (PlacedDeviceModel device in e.NewItems)
                    {
                        CreatePinWidget(device);
                    }
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (PlacedDeviceModel device in e.OldItems)
                    {
                        RemovePinWidget(device.DeviceId);
                    }
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                RefreshPins();
                break;
        }
    }

    private void CreatePinWidget(PlacedDeviceModel placedDevice)
    {
        var pinWidget = new DevicePinWidget
        {
            PlacedDevice = placedDevice,
            PinTappedCommand = PinTappedCommand
        };

        // Add pan gesture for dragging
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += (s, e) => OnPinPan(s, e, placedDevice);
        pinWidget.GestureRecognizers.Add(panGesture);

        _pinWidgets[placedDevice.DeviceId] = pinWidget;
        PinContainer.Children.Add(pinWidget);

        UpdatePinPosition(pinWidget, placedDevice);
    }

    private void RemovePinWidget(string deviceId)
    {
        if (_pinWidgets.TryGetValue(deviceId, out var widget))
        {
            PinContainer.Children.Remove(widget);
            _pinWidgets.Remove(deviceId);
        }
    }

    private void UpdatePinPositions()
    {
        foreach (var kvp in _pinWidgets)
        {
            var widget = kvp.Value;
            var placedDevice = widget.PlacedDevice;
            if (placedDevice != null)
            {
                UpdatePinPosition(widget, placedDevice);
            }
        }
    }

    private void UpdatePinPosition(DevicePinWidget widget, PlacedDeviceModel placedDevice)
    {
        if (ContainerWidth <= 0 || ContainerHeight <= 0) return;

        // Convert relative coordinates (0.0-1.0) to absolute position
        var absoluteX = placedDevice.X * ContainerWidth;
        var absoluteY = placedDevice.Y * ContainerHeight;

        // Center the pin on the coordinates
        var rect = new Rect(
            absoluteX - (placedDevice.Size / 2),
            absoluteY - (placedDevice.Size / 2),
            placedDevice.Size,
            placedDevice.Size);

        AbsoluteLayout.SetLayoutBounds(widget, rect);
        AbsoluteLayout.SetLayoutFlags(widget, AbsoluteLayoutFlags.None);
    }

    private void OnPinPan(object? sender, PanUpdatedEventArgs e, PlacedDeviceModel placedDevice)
    {
        if (sender is not DevicePinWidget widget) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _draggingPin = widget;
                _dragStartPoint = new Point(e.TotalX, e.TotalY);
                _isDragging = true;
                break;

            case GestureStatus.Running:
                if (_isDragging && _draggingPin == widget)
                {
                    // Update pin position during drag
                    var currentBounds = AbsoluteLayout.GetLayoutBounds(widget);
                    var newX = currentBounds.X + e.TotalX - _dragStartPoint.X;
                    var newY = currentBounds.Y + e.TotalY - _dragStartPoint.Y;

                    // Clamp to container bounds
                    newX = Math.Max(0, Math.Min(ContainerWidth - placedDevice.Size, newX));
                    newY = Math.Max(0, Math.Min(ContainerHeight - placedDevice.Size, newY));

                    var newBounds = new Rect(newX, newY, currentBounds.Width, currentBounds.Height);
                    AbsoluteLayout.SetLayoutBounds(widget, newBounds);

                    _dragStartPoint = new Point(e.TotalX, e.TotalY);
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isDragging && _draggingPin == widget)
                {
                    // Convert final position back to relative coordinates
                    var finalBounds = AbsoluteLayout.GetLayoutBounds(widget);
                    var centerX = finalBounds.X + (placedDevice.Size / 2);
                    var centerY = finalBounds.Y + (placedDevice.Size / 2);

                    var relativeX = centerX / ContainerWidth;
                    var relativeY = centerY / ContainerHeight;

                    // Clamp to valid range
                    relativeX = Math.Max(0.0, Math.Min(1.0, relativeX));
                    relativeY = Math.Max(0.0, Math.Min(1.0, relativeY));

                    // Execute drag command to persist the new position
                    if (PinDraggedCommand?.CanExecute(null) == true)
                    {
                        var dragData = new { DeviceId = placedDevice.DeviceId, X = relativeX, Y = relativeY };
                        PinDraggedCommand.Execute(dragData);
                    }

                    _draggingPin = null;
                    _isDragging = false;
                }
                break;
        }
    }
}