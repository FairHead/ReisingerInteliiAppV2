using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    private static void OnPlacedDevicesChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is DevicePinOverlay overlay)
        {
            // Unsubscribe from old collection
            if (oldValue is ObservableCollection<PlacedDeviceModel> oldCollection)
            {
                oldCollection.CollectionChanged -= overlay.OnDevicesCollectionChanged;
            }

            // Subscribe to new collection
            if (newValue is ObservableCollection<PlacedDeviceModel> newCollection)
            {
                newCollection.CollectionChanged += overlay.OnDevicesCollectionChanged;
                overlay.UpdatePinPositions();
            }
        }
    }

    private void OnDevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdatePinPositions();
    }

    private void UpdatePinPositions()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Clear existing pins
            PinContainer.Children.Clear();

            if (PlacedDevices == null) return;

            // Create pins for each placed device
            foreach (var device in PlacedDevices)
            {
                CreateDevicePin(device);
            }
        });
    }

    private void CreateDevicePin(PlacedDeviceModel device)
    {
        var pin = new Border
        {
            BackgroundColor = Color.FromArgb("#2A2A2A"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#007AFF"),
            StrokeThickness = 2,
            Padding = new Thickness(8, 6),
            Scale = device.Scale,
            BindingContext = device
        };

        var content = new StackLayout
        {
            Spacing = 4,
            Orientation = StackOrientation.Vertical
        };

        // Device Name
        content.Children.Add(new Label
        {
            Text = device.DeviceName,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        });

        // Type indicator
        content.Children.Add(new Label
        {
            Text = device.DeviceType,
            FontSize = 8,
            TextColor = Color.FromArgb("#B0B0B0"),
            HorizontalOptions = LayoutOptions.Center
        });

        // Control Buttons
        var buttonStack = new HorizontalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.Center
        };

        // Scale Down Button
        var scaleDownButton = new Button
        {
            Text = "−",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#FF6B6B"),
            TextColor = Colors.White,
            WidthRequest = 24,
            HeightRequest = 24,
            CornerRadius = 12,
            Padding = new Thickness(0),
            Command = ScaleDownCommand,
            CommandParameter = device
        };

        // Door Button
        var doorButton = new Button
        {
            Text = "🚪",
            FontSize = 12,
            BackgroundColor = Color.FromArgb("#4ECDC4"),
            TextColor = Colors.White,
            WidthRequest = 30,
            HeightRequest = 24,
            CornerRadius = 12,
            Padding = new Thickness(0),
            Command = ToggleDoorCommand,
            CommandParameter = device
        };

        // Scale Up Button
        var scaleUpButton = new Button
        {
            Text = "+",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#51CF66"),
            TextColor = Colors.White,
            WidthRequest = 24,
            HeightRequest = 24,
            CornerRadius = 12,
            Padding = new Thickness(0),
            Command = ScaleUpCommand,
            CommandParameter = device
        };

        buttonStack.Children.Add(scaleDownButton);
        buttonStack.Children.Add(doorButton);
        buttonStack.Children.Add(scaleUpButton);
        content.Children.Add(buttonStack);

        pin.Content = content;

        // Add pan gesture
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += (s, e) => OnPanUpdated(pin, e);
        pin.GestureRecognizers.Add(panGesture);

        // Position the pin using AbsoluteLayout
        AbsoluteLayout.SetLayoutBounds(pin, new Rect(device.X, device.Y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(pin, AbsoluteLayoutFlags.PositionProportional);

        // Add to container
        PinContainer.Children.Add(pin);
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
                var layoutBounds = AbsoluteLayout.GetLayoutBounds(border);
                var newX = Math.Max(0, Math.Min(1, layoutBounds.X + (border.TranslationX / PinContainer.Width)));
                var newY = Math.Max(0, Math.Min(1, layoutBounds.Y + (border.TranslationY / PinContainer.Height)));
                
                // Reset visual translation
                border.TranslationX = 0;
                border.TranslationY = 0;
                
                // Update model and position
                device.X = newX;
                device.Y = newY;
                
                // Update layout bounds
                AbsoluteLayout.SetLayoutBounds(border, new Rect(newX, newY, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
                
                UpdatePositionCommand?.Execute(device);
                break;
        }
    }
}