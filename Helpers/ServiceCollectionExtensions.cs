using Microsoft.Extensions.Http;
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
        // Register HTTP Client
        services.AddHttpClient();
        
        // Register Services
        services.AddSingleton<IDeviceService, DeviceService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
    services.AddSingleton<IBuildingStorageService, BuildingStorageService>();
    services.AddSingleton<PdfConversionService>();
    services.AddSingleton<PdfStorageService>();
        services.AddSingleton<IntellidriveApiService>();
        services.AddSingleton<WiFiManagerService>();
    services.AddSingleton<PdfConversionService>();
    services.AddSingleton<PdfStorageService>();
    services.AddSingleton<FloorPlanService>();

        // Register ViewModels
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<WifiScanViewModel>();
        services.AddTransient<WifiScanPageViewModel>();
        services.AddTransient<LocalDevicesScanPageViewModel>();
        services.AddTransient<SaveDevicePageViewModel>();
    services.AddTransient<SaveLocalDevicePageViewModel>();
    services.AddTransient<StructuresViewModel>();
    services.AddTransient<StructureEditorViewModel>();

        // Register Pages
        services.AddTransient<MainPage>();
        services.AddTransient<WifiScanPage>();
        services.AddTransient<LocalDevicesScanPage>();
        services.AddTransient<SaveDevicePage>();
    services.AddTransient<SaveLocalDevicePage>();
    services.AddTransient<StructureEditorPage>();

        return services;
    }
}
