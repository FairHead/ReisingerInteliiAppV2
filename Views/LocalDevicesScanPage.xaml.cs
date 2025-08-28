using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class LocalDevicesScanPage : ContentPage
{
    private LocalDevicesScanPageViewModel? _viewModel;

    public LocalDevicesScanPage(LocalDevicesScanPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
