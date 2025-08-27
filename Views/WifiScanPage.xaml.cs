using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class WifiScanPage : ContentPage
{
    private WifiScanViewModel? _viewModel;

    public WifiScanPage(WifiScanViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        SetupFooterEvents();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Automatically scan for WiFi networks when page appears
        if (_viewModel != null)
        {
            await _viewModel.LoadNetworksOnAppearingAsync();
        }
    }

    private void SetupFooterEvents()
    {
        if (_viewModel != null)
        {
            // Footer.LeftSectionTapped += (s, e) => _viewModel.HomeCommand.Execute(null);
            Footer.CenterButtonTapped += (s, e) => _viewModel.RefreshNetworksCommand.Execute(null);
            // Footer.RightSectionTapped += (s, e) => _viewModel.SettingsCommand.Execute(null);
        }
    }
}
