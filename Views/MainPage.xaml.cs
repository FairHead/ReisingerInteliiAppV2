using Microsoft.Maui.Controls.Shapes;
using ReisingerIntelliApp_V4.ViewModels;
using ReisingerIntelliApp_V4.Helpers;
using ReisingerIntelliApp_V4.Services;
using System.Linq;

namespace ReisingerIntelliApp_V4.Views;

public partial class MainPage : ContentPage
{
    private MainPageViewModel? _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        SetupFooterEvents();
        SetupViewModelEvents();
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
                    });
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
                // Clear device selection when switching structures
                _viewModel.SelectedDeviceItem = null;
            }
            else if (_viewModel.CurrentActiveTab == "Levels")
            {
                // Select a level for later operations and highlight
                _viewModel.SelectedLevelName = item.Id;
                foreach (var it in _viewModel.DropdownItems)
                    it.IsSelected = it.Id == item.Id;
                // Clear device selection when switching levels
                _viewModel.SelectedDeviceItem = null;
            }
            else if (_viewModel.CurrentActiveTab == "WifiDev" || _viewModel.CurrentActiveTab == "LocalDev")
            {
                // Update selected device and highlight
                _viewModel.SelectedDeviceItem = item;
                foreach (var it in _viewModel.DropdownItems)
                    it.IsSelected = it.Id == item.Id;
            }
        }
        catch { }
    }

    /// <summary>
    /// Handles the "+" button click to add the selected device to the floor plan
    /// </summary>
    private async void OnAddDeviceButtonClicked(object sender, EventArgs e)
    {
        if (_viewModel?.SelectedDeviceItem == null || _viewModel?.StructuresVM == null) return;

        try
        {
            // For now, place the device at a default center position (0.5, 0.5)
            // TODO: Could be enhanced to let user tap on the floor plan to choose position
            var centerX = 0.5;
            var centerY = 0.5;

            bool success = false;

            if (_viewModel.CurrentActiveTab == "WifiDev")
            {
                // Find the saved device
                var deviceService = ServiceHelper.GetService<IDeviceService>();
                var savedDevices = await deviceService.GetSavedWifiDevicesAsync();
                var device = savedDevices.FirstOrDefault(d => d.DeviceId == _viewModel.SelectedDeviceItem.Id);
                if (device != null)
                {
                    success = await _viewModel.StructuresVM.AddSavedDevicePinAsync(device, centerX, centerY);
                }
            }
            else if (_viewModel.CurrentActiveTab == "LocalDev")
            {
                // Find the local device
                var deviceService = ServiceHelper.GetService<IDeviceService>();
                var localDevices = await deviceService.GetSavedLocalDevicesAsync();
                var device = localDevices.FirstOrDefault(d => d.DeviceId == _viewModel.SelectedDeviceItem.Id);
                if (device != null)
                {
                    success = await _viewModel.StructuresVM.AddLocalDevicePinAsync(device, centerX, centerY);
                }
            }

            if (success)
            {
                await DisplayAlert("Success", $"Added '{_viewModel.SelectedDeviceItem.Text.Split('\n')[0]}' to floor plan", "OK");
                // Clear selection after adding
                _viewModel.SelectedDeviceItem = null;
            }
            else
            {
                await DisplayAlert("Error", "Failed to add device to floor plan", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add device: {ex.Message}", "OK");
        }
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
}
