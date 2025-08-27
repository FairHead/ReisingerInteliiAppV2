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
        services.AddSingleton<IntellidriveApiService>();
        services.AddSingleton<WiFiManagerService>();

        // Register ViewModels
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<WifiScanViewModel>();
        services.AddTransient<WifiScanPageViewModel>();
        services.AddTransient<LocalDevicesScanPageViewModel>();
        services.AddTransient<SaveDevicePageViewModel>();

        // Register Pages
        services.AddTransient<MainPage>();
        services.AddTransient<WifiScanPage>();
        services.AddTransient<SaveDevicePage>();

        return services;
    }
}
