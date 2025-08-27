using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class LocalDevicesScanPage : ContentPage
{
    private LocalDevicesScanPageViewModel? _viewModel;

    public LocalDevicesScanPage()
    {
        InitializeComponent();
        _viewModel = BindingContext as LocalDevicesScanPageViewModel;
        SetupFooterEvents();
    }

    private void SetupFooterEvents()
    {
        if (_viewModel != null)
        {
            Footer.LeftSectionTapped += (s, e) => _viewModel.HomeCommand.Execute(null);
            Footer.CenterButtonTapped += (s, e) => _viewModel.RefreshCommand.Execute(null);
            Footer.RightSectionTapped += (s, e) => _viewModel.SettingsCommand.Execute(null);
        }
    }

    private void OnIpEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        // Validate IP format - can be implemented later
    }
}
