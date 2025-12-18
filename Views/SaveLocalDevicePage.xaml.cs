using ReisingerIntelliApp_V4.ViewModels;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.Views;

public partial class SaveLocalDevicePage : ContentPage
{
    private readonly SaveLocalDevicePageViewModel _vm;

    public SaveLocalDevicePage(SaveLocalDevicePageViewModel vm)
    {
        Debug.WriteLine("?? [SaveLocalDevicePage] Constructor called");
        InitializeComponent();
        Debug.WriteLine("?? [SaveLocalDevicePage] InitializeComponent completed");
        _vm = vm;
        BindingContext = _vm;
        Debug.WriteLine($"?? [SaveLocalDevicePage] BindingContext set to ViewModel: {_vm != null}");
    }

    protected override void OnAppearing()
    {
        Debug.WriteLine("?? [SaveLocalDevicePage] OnAppearing called");
        base.OnAppearing();
        _vm.StartOnlineStatusMonitoring();
        Debug.WriteLine("?? [SaveLocalDevicePage] OnAppearing completed");
    }

    protected override void OnDisappearing()
    {
        Debug.WriteLine("?? [SaveLocalDevicePage] OnDisappearing called");
        base.OnDisappearing();
        _vm.StopOnlineStatusMonitoring();
        Debug.WriteLine("?? [SaveLocalDevicePage] OnDisappearing completed");
    }
}
