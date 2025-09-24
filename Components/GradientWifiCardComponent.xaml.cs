using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;

namespace ReisingerIntelliApp_V4.Components
{
    public partial class GradientWifiCardComponent : ContentView
    {
        // Commands exposed so parent DataTemplate can bind application commands
        public static readonly BindableProperty AddToFloorPlanCommandProperty =
            BindableProperty.Create(nameof(AddToFloorPlanCommand), typeof(ICommand), typeof(GradientWifiCardComponent), default(ICommand));

        public static readonly BindableProperty ShowDeviceOptionsCommandProperty =
            BindableProperty.Create(nameof(ShowDeviceOptionsCommand), typeof(ICommand), typeof(GradientWifiCardComponent), default(ICommand));

        public static readonly BindableProperty DeleteCommandProperty =
            BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(GradientWifiCardComponent), default(ICommand));

        public static readonly BindableProperty SettingsCommandProperty =
            BindableProperty.Create(nameof(SettingsCommand), typeof(ICommand), typeof(GradientWifiCardComponent), default(ICommand));

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(GradientWifiCardComponent), null);

        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(GradientWifiCardComponent), string.Empty);

        public static readonly BindableProperty ShowNetworkInfoProperty =
            BindableProperty.Create(nameof(ShowNetworkInfo), typeof(bool), typeof(GradientWifiCardComponent), false);

        public static readonly BindableProperty AddButtonEnabledProperty =
            BindableProperty.Create(nameof(AddButtonEnabled), typeof(bool), typeof(GradientWifiCardComponent), true);

        // Visibility / behavior toggles
        public static readonly BindableProperty ShowStatusProperty =
            BindableProperty.Create(nameof(ShowStatus), typeof(bool), typeof(GradientWifiCardComponent), false);

        public static readonly BindableProperty IsPlacedOnCurrentFloorProperty =
            BindableProperty.Create(nameof(IsPlacedOnCurrentFloor), typeof(bool), typeof(GradientWifiCardComponent), false);

        public static readonly BindableProperty ShowAddButtonProperty =
            BindableProperty.Create(nameof(ShowAddButton), typeof(bool), typeof(GradientWifiCardComponent), true);

        public static readonly BindableProperty ShowSettingsButtonProperty =
            BindableProperty.Create(nameof(ShowSettingsButton), typeof(bool), typeof(GradientWifiCardComponent), true);

        public static readonly BindableProperty IsSelectedProperty =
            BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(GradientWifiCardComponent), false, propertyChanged: OnIsSelectedChanged);

        // Bindable Properties
        public static readonly BindableProperty DeviceNameProperty =
            BindableProperty.Create(nameof(DeviceName), typeof(string), typeof(GradientWifiCardComponent), "WiFi Device", propertyChanged: OnDeviceNameChanged);

        public static readonly BindableProperty LastSeenProperty =
            BindableProperty.Create(nameof(LastSeen), typeof(string), typeof(GradientWifiCardComponent), "13:46", propertyChanged: OnLastSeenChanged);

        public static readonly BindableProperty NetworkInfoProperty =
            BindableProperty.Create(nameof(NetworkInfo), typeof(string), typeof(GradientWifiCardComponent), string.Empty, propertyChanged: OnNetworkInfoChanged);

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(GradientWifiCardComponent), false, propertyChanged: OnConnectionStatusChanged);

        // Properties
        public string DeviceName
        {
            get => (string)GetValue(DeviceNameProperty);
            set => SetValue(DeviceNameProperty, value);
        }

        public string LastSeen
        {
            get => (string)GetValue(LastSeenProperty);
            set => SetValue(LastSeenProperty, value);
        }

        public string NetworkInfo
        {
            get => (string)GetValue(NetworkInfoProperty);
            set => SetValue(NetworkInfoProperty, value);
        }

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

    // Events
    public event EventHandler? MonitorClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? DeleteClicked;

        // Properties for commands and toggles
        public ICommand? AddToFloorPlanCommand
        {
            get => (ICommand?)GetValue(AddToFloorPlanCommandProperty);
            set => SetValue(AddToFloorPlanCommandProperty, value);
        }

        public ICommand? ShowDeviceOptionsCommand
        {
            get => (ICommand?)GetValue(ShowDeviceOptionsCommandProperty);
            set => SetValue(ShowDeviceOptionsCommandProperty, value);
        }

        public ICommand? DeleteCommand
        {
            get => (ICommand?)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public ICommand? SettingsCommand
        {
            get => (ICommand?)GetValue(SettingsCommandProperty);
            set => SetValue(SettingsCommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool ShowNetworkInfo
        {
            get => (bool)GetValue(ShowNetworkInfoProperty);
            set => SetValue(ShowNetworkInfoProperty, value);
        }

        public bool AddButtonEnabled
        {
            get => (bool)GetValue(AddButtonEnabledProperty);
            set => SetValue(AddButtonEnabledProperty, value);
        }

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        public bool IsPlacedOnCurrentFloor
        {
            get => (bool)GetValue(IsPlacedOnCurrentFloorProperty);
            set => SetValue(IsPlacedOnCurrentFloorProperty, value);
        }

        public bool ShowAddButton
        {
            get => (bool)GetValue(ShowAddButtonProperty);
            set => SetValue(ShowAddButtonProperty, value);
        }

        public bool ShowSettingsButton
        {
            get => (bool)GetValue(ShowSettingsButtonProperty);
            set => SetValue(ShowSettingsButtonProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public GradientWifiCardComponent()
        {
            InitializeComponent();
            UpdateConnectionStatus();
            UpdateSelectionState();
        }

        // Property Changed Handlers
        private static void OnDeviceNameChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // Property change handled via binding - no direct UI manipulation needed
        }

        private static void OnLastSeenChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // Property change handled via binding - no direct UI manipulation needed
        }

        private static void OnNetworkInfoChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // Property change handled via binding - no direct UI manipulation needed
        }

        private static void OnConnectionStatusChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GradientWifiCardComponent control)
            {
                control.UpdateConnectionStatus();
            }
        }

        private static void OnIsSelectedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GradientWifiCardComponent control)
            {
                control.UpdateSelectionState();
            }
        }

        // Private Methods
        private void UpdateConnectionStatus()
        {
            // Connection status updates are handled via binding and triggers in XAML
            // No direct UI manipulation needed here
        }

        private void UpdateSelectionState()
        {
            // Selection state updates are handled via binding and visual states in XAML
            // No direct UI manipulation needed here
        }

        // Event Handlers
        private void OnMonitorClicked(object sender, EventArgs e)
        {
            // Execute bound command if present
            if (AddToFloorPlanCommand?.CanExecute(CommandParameter) == true)
                AddToFloorPlanCommand.Execute(CommandParameter);
            MonitorClicked?.Invoke(this, e);
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            if (SettingsCommand?.CanExecute(CommandParameter) == true)
                SettingsCommand.Execute(CommandParameter);
            SettingsClicked?.Invoke(this, e);
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (DeleteCommand?.CanExecute(CommandParameter) == true)
                DeleteCommand.Execute(CommandParameter);
            DeleteClicked?.Invoke(this, e);
        }
    }
}