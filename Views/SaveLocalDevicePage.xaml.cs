using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class SaveLocalDevicePage : ContentPage
{
    private readonly SaveLocalDevicePageViewModel _vm;

    public SaveLocalDevicePage(SaveLocalDevicePageViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.StartOnlineStatusMonitoring();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopOnlineStatusMonitoring();
    }
}
