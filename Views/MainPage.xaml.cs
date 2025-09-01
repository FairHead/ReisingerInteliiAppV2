using Microsoft.Maui.Controls.Shapes;
using ReisingerIntelliApp_V4.ViewModels;
using ReisingerIntelliApp_V4.Helpers;
using ReisingerIntelliApp_V4.Services;
using ReisingerIntelliApp_V4.Models;
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

    // Device Pin Event Handlers
    private async void OnDoorControlRequested(object? sender, PlacedDeviceModel device)
    {
        if (_viewModel == null) return;

        try
        {
            // Create a DeviceModel for API call
            var deviceModel = new DeviceModel
            {
                DeviceId = device.DeviceId,
                Name = device.DeviceName,
                Ip = device.DeviceIp,
                Username = device.Username,
                Password = device.Password,
                Type = device.DeviceType == DeviceType.WifiDevice ? AppDeviceType.WifiDevice : AppDeviceType.LocalDevice
            };

            await _viewModel.ToggleDoorAsync(deviceModel);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Door control failed: {ex.Message}", "OK");
        }
    }

    private async void OnSettingsRequested(object? sender, PlacedDeviceModel device)
    {
        if (_viewModel == null) return;

        try
        {
            // Create a DeviceModel for navigation
            var deviceModel = new DeviceModel
            {
                DeviceId = device.DeviceId,
                Name = device.DeviceName,
                Ip = device.DeviceIp,
                Username = device.Username,
                Password = device.Password,
                Type = device.DeviceType == DeviceType.WifiDevice ? AppDeviceType.WifiDevice : AppDeviceType.LocalDevice
            };

            await _viewModel.OpenDeviceSettingsAsync(deviceModel);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Settings navigation failed: {ex.Message}", "OK");
        }
    }

    private async void OnDeviceDeleted(object? sender, PlacedDeviceModel device)
    {
        if (_viewModel?.StructuresVM?.SelectedLevel == null) return;

        try
        {
            // Save the updated floor plan
            await _viewModel.SaveCurrentFloorPlanAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Save failed: {ex.Message}", "OK");
        }
    }

    private async void OnDevicePositionChanged(object? sender, PlacedDeviceModel device)
    {
        if (_viewModel?.StructuresVM?.SelectedLevel == null) return;

        try
        {
            // Save the updated floor plan
            await _viewModel.SaveCurrentFloorPlanAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Save failed: {ex.Message}", "OK");
        }
    }

    private async void OnDevicePlacementRequested(object? sender, (double X, double Y) position)
    {
        if (_viewModel == null) return;

        try
        {
            // Complete device placement at the specified position
            await _viewModel.CompleteDevicePlacementAsync(position.X, position.Y);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Device placement failed: {ex.Message}", "OK");
        }
    }
}
