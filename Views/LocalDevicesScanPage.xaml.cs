using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class LocalDevicesScanPage : ContentPage
{
    private LocalDevicesScanPageViewModel? _viewModel;

    public LocalDevicesScanPage(LocalDevicesScanPageViewModel viewModel)
    {
        Console.WriteLine($"🏗️ LocalDevicesScanPage constructor - START");
        
        // ⭐ CRITICAL FIX: Set BindingContext BEFORE InitializeComponent
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        InitializeComponent();
        
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

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // ⭐ CRITICAL FIX: Wire up CollectionView to manually set commands when items are created
        if (LocalDevicesCollectionView != null && _viewModel != null)
        {
            // Explicitly set the BindingContext for the CollectionView to ensure command binding works
            LocalDevicesCollectionView.BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine("✅ OnHandlerChanged: LocalDevicesCollectionView.BindingContext explicitly set to ViewModel");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ OnHandlerChanged: LocalDevicesCollectionView or _viewModel is NULL!");
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[BUTTON DEBUG] OnBackButtonClicked CLICKED - Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, Time: {DateTime.Now:HH:mm:ss.fff}");
        
        await Shell.Current.GoToAsync("..");
    }

    private void OnAddDeviceClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🟢 [LocalDevicesScanPage] OnAddDeviceClicked TRIGGERED");
            
            // Get the button and its CommandParameter (which contains the LocalNetworkDeviceModel)
            if (sender is Button button && button.CommandParameter is Models.LocalNetworkDeviceModel device)
            {
                System.Diagnostics.Debug.WriteLine($"   📱 Device: {device.DisplayName} ({device.IpAddress})");
                
                // Execute the AddDeviceCommand from the ViewModel
                if (_viewModel?.AddDeviceCommand?.CanExecute(device) == true)
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ Executing AddDeviceCommand");
                    _viewModel.AddDeviceCommand.Execute(device);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   ❌ AddDeviceCommand cannot execute or is NULL");
                    var vmStatus = _viewModel == null ? "NULL" : "SET";
                    var cmdStatus = _viewModel?.AddDeviceCommand == null ? "NULL" : "SET";
                    System.Diagnostics.Debug.WriteLine($"      - _viewModel: {vmStatus}");
                    System.Diagnostics.Debug.WriteLine($"      - AddDeviceCommand: {cmdStatus}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"   ❌ CommandParameter is NULL or not a LocalNetworkDeviceModel");
                var senderType = sender?.GetType().Name ?? "null";
                var paramType = (sender as Button)?.CommandParameter?.GetType().Name ?? "null";
                System.Diagnostics.Debug.WriteLine($"      - sender type: {senderType}");
                System.Diagnostics.Debug.WriteLine($"      - CommandParameter type: {paramType}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in OnAddDeviceClicked: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }
}
