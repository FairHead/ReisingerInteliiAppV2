using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class LocalDevicesScanPage : ContentPage
{
    private LocalDevicesScanPageViewModel? _viewModel;

    public LocalDevicesScanPage(LocalDevicesScanPageViewModel viewModel)
    {
        Console.WriteLine($"🏗️ LocalDevicesScanPage constructor - START");
        
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        Console.WriteLine($"   ✅ ViewModel injected: {_viewModel != null}");
        Console.WriteLine($"   ✅ BindingContext set: {BindingContext != null}");
        
        // Verify command availability after ViewModel setup
        if (_viewModel != null)
        {
            Console.WriteLine($"   🔧 AddDeviceCommand available: {_viewModel.AddDeviceCommand != null}");
            Console.WriteLine($"   🔧 StartLocalScanCommand available: {_viewModel.StartLocalScanCommand != null}");
            Console.WriteLine($"   📊 LocalDevices count: {_viewModel.LocalDevices?.Count ?? -1}");
        }
        
        Console.WriteLine($"🏗️ LocalDevicesScanPage constructor - COMPLETE");
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[BUTTON DEBUG] OnBackButtonClicked CLICKED - Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, Time: {DateTime.Now:HH:mm:ss.fff}");
        
        await Shell.Current.GoToAsync("..");
    }
}
