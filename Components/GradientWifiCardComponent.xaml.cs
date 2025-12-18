using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Windows.Input;

namespace ReisingerIntelliApp_V4.Components
{
    public partial class GradientWifiCardComponent : ContentView
    {
        // Commands exposed so parent DataTemplate can bind application commands
        public static readonly BindableProperty AddToFloorPlanCommandProperty =
            BindableProperty.Create(
                nameof(AddToFloorPlanCommand), 
                typeof(ICommand), 
                typeof(GradientWifiCardComponent), 
                default(ICommand),
                propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty ShowDeviceOptionsCommandProperty =
            BindableProperty.Create(
                nameof(ShowDeviceOptionsCommand), 
                typeof(ICommand), 
                typeof(GradientWifiCardComponent), 
                default(ICommand),
                propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty DeleteCommandProperty =
            BindableProperty.Create(
                nameof(DeleteCommand), 
                typeof(ICommand), 
                typeof(GradientWifiCardComponent), 
                default(ICommand),
                propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty SettingsCommandProperty =
            BindableProperty.Create(
                nameof(SettingsCommand), 
                typeof(ICommand), 
                typeof(GradientWifiCardComponent), 
                default(ICommand),
                propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(
                nameof(CommandParameter), 
                typeof(object), 
                typeof(GradientWifiCardComponent), 
                null,
                propertyChanged: OnCommandParameterChanged);

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

        // Internal Relay Commands for XAML binding
        public ICommand InternalAddToFloorPlanCommand { get; }
        public ICommand InternalSettingsCommand { get; }
        public ICommand InternalDeleteCommand { get; }

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

        // Events - ALWAYS fire these so code-behind can handle them
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
            Debug.WriteLine($"?? [GradientCard] Constructor START for DeviceName: {DeviceName}");
            
            // ? FIX: Create relay commands that ALWAYS fire events, regardless of whether Commands are bound
            InternalAddToFloorPlanCommand = new Command(() =>
            {
                Debug.WriteLine($"?? [GradientCard] InternalAddToFloorPlanCommand triggered for: {DeviceName}");
                
                // ALWAYS fire the event first - code-behind will handle it
                Debug.WriteLine($"   ?? Firing MonitorClicked event...");
                MonitorClicked?.Invoke(this, EventArgs.Empty);
                
                // Then try to execute the command if it's bound
                if (AddToFloorPlanCommand != null && AddToFloorPlanCommand.CanExecute(CommandParameter))
                {
                    Debug.WriteLine($"   ? Also executing AddToFloorPlanCommand");
                    AddToFloorPlanCommand.Execute(CommandParameter);
                }
                else
                {
                    Debug.WriteLine($"   ?? Command not bound, relying on event handling");
                }
            });

            InternalSettingsCommand = new Command(() =>
            {
                Debug.WriteLine($"?? [GradientCard] InternalSettingsCommand triggered for: {DeviceName}");
                
                // ALWAYS fire the event first
                Debug.WriteLine($"   ?? Firing SettingsClicked event...");
                SettingsClicked?.Invoke(this, EventArgs.Empty);
                
                // Then try to execute the command if it's bound
                if (SettingsCommand != null && SettingsCommand.CanExecute(CommandParameter))
                {
                    Debug.WriteLine($"   ? Also executing SettingsCommand");
                    SettingsCommand.Execute(CommandParameter);
                }
                else
                {
                    Debug.WriteLine($"   ?? Command not bound, relying on event handling");
                }
            });

            InternalDeleteCommand = new Command(() =>
            {
                Debug.WriteLine($"?? [GradientCard] InternalDeleteCommand triggered for: {DeviceName}");
                
                // ALWAYS fire the event first
                Debug.WriteLine($"   ?? Firing DeleteClicked event...");
                DeleteClicked?.Invoke(this, EventArgs.Empty);
                
                // Then try to execute the command if it's bound
                if (DeleteCommand != null && DeleteCommand.CanExecute(CommandParameter))
                {
                    Debug.WriteLine($"   ? Also executing DeleteCommand");
                    DeleteCommand.Execute(CommandParameter);
                }
                else
                {
                    Debug.WriteLine($"   ?? Command not bound, relying on event handling");
                }
            });

            InitializeComponent();
            UpdateConnectionStatus();
            UpdateSelectionState();
            
            Debug.WriteLine($"?? [GradientCard] Constructor END for DeviceName: {DeviceName}");
        }

        // Property changed handlers for command bindings
        private static void OnCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GradientWifiCardComponent card)
            {
                Debug.WriteLine($"?? [GradientCard] Command property changed for '{card.DeviceName}'");
                Debug.WriteLine($"   ?? New: {(newValue == null ? "NULL" : "NOT NULL ?")}");
                
                if (newValue != null)
                {
                    Debug.WriteLine($"   ? Command binding successful");
                }
                else
                {
                    Debug.WriteLine($"   ?? Command is NULL - will use event handling instead");
                }
            }
        }

        private static void OnCommandParameterChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GradientWifiCardComponent card)
            {
                Debug.WriteLine($"?? [GradientCard] CommandParameter changed for '{card.DeviceName}'");
                Debug.WriteLine($"   ?? New: {newValue?.GetType().Name ?? "null"}");
            }
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

        // Event Handlers (kept for backward compatibility, but events are now fired in Internal commands)
        private void OnMonitorClicked(object sender, EventArgs e)
        {
            // This might be called from other sources - also fire event
            MonitorClicked?.Invoke(this, e);
            if (AddToFloorPlanCommand?.CanExecute(CommandParameter) == true)
                AddToFloorPlanCommand.Execute(CommandParameter);
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            SettingsClicked?.Invoke(this, e);
            if (SettingsCommand?.CanExecute(CommandParameter) == true)
                SettingsCommand.Execute(CommandParameter);
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            DeleteClicked?.Invoke(this, e);
            if (DeleteCommand?.CanExecute(CommandParameter) == true)
                DeleteCommand.Execute(CommandParameter);
        }
    }
}