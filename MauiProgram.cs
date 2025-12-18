using Microsoft.Extensions.Logging;
using ReisingerIntelliApp_V4.Helpers;
using CommunityToolkit.Maui;
using ReisingerIntelliApp_V4.Services;

namespace ReisingerIntelliApp_V4;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("SpaceMono-Regular.ttf", "SpaceMonoRegular");
                fonts.AddFont("SpaceMono-Bold.ttf", "SpaceMonoBold");
                fonts.AddFont("SpaceMono-Italic.ttf", "SpaceMonoItalic");
                fonts.AddFont("SpaceMono-BoldItalic.ttf", "SpaceMonoBoldItalic");
            });

        // Register services and ViewModels
        builder.Services.RegisterServices();




#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
