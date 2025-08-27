using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class SaveDevicePage : ContentPage
{
    private readonly SaveDevicePageViewModel _viewModel;

    public SaveDevicePage(SaveDevicePageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop network monitoring when leaving the page
        _viewModel?.Dispose();
    }
}
