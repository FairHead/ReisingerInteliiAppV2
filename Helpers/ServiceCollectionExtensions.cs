using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection; // For AddPolicyHandler extensions
using ReisingerIntelliApp_V4.Services;
using ReisingerIntelliApp_V4.ViewModels;
using ReisingerIntelliApp_V4.Views;

#if ANDROID
using ReisingerIntelliApp_V4.Platforms.Android;
#endif

namespace ReisingerIntelliApp_V4.Helpers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
    // Register HTTP Clients
    services.AddHttpClient(); // default
    // Named client for IntelliDrive API (Polly handlers can be added once extension methods available in this TFMs)
    services.AddHttpClient("intellidrive");
        
    // Register Services
    services.AddSingleton<IDeviceService, DeviceService>();
    services.AddSingleton<INavigationService, NavigationService>();
    services.AddSingleton<IAuthenticationService, AuthenticationService>();
    services.AddSingleton<IntellidriveApiService>();
    services.AddSingleton<IBuildingStorageService, BuildingStorageService>();
    services.AddSingleton<PdfConversionService>();
    services.AddSingleton<PdfStorageService>();
    // Removed API logic and HTTP client abstraction per user request
    services.AddSingleton<Microsoft.Maui.Networking.IConnectivity>(Connectivity.Current);
    services.AddSingleton<WiFiManagerService>();
    services.AddSingleton<PdfConversionService>();
    services.AddSingleton<PdfStorageService>();
    // ViewModel used inside PlacedDeviceControl for door toggling (must be resolvable via ServiceHelper)
    services.AddTransient<DeviceControlViewModel>();
    services.AddTransient<PlacedDeviceControlViewModel>();

    // Register ViewModels
    services.AddTransient<MainPageViewModel>();
    services.AddTransient<WifiScanViewModel>();
    services.AddTransient<WifiScanPageViewModel>();
    services.AddTransient<LocalDevicesScanPageViewModel>();
    services.AddTransient<SaveDevicePageViewModel>();
    services.AddTransient<SaveLocalDevicePageViewModel>();
    services.AddTransient<StructuresViewModel>();
    services.AddTransient<StructureEditorViewModel>();
    services.AddTransient<DeviceParametersPageViewModel>();

    // Register Pages
    services.AddTransient<MainPage>();
    services.AddTransient<WifiScanPage>();
    services.AddTransient<LocalDevicesScanPage>();
    services.AddTransient<SaveDevicePage>();
    services.AddTransient<SaveLocalDevicePage>();
    services.AddTransient<StructureEditorPage>();
    services.AddTransient<DeviceParametersPage>();

    return services;
    }
}
