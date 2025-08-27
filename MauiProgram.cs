using Microsoft.Extensions.Logging;
using ReisingerIntelliApp_V4.Helpers;

namespace ReisingerIntelliApp_V4;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("SpaceMono-Regular.ttf", "SpaceMonoRegular");
				fonts.AddFont("SpaceMono-Bold.ttf", "SpaceMonoBold");
				fonts.AddFont("SpaceMono-Italic.ttf", "SpaceMonoItalic");
				fonts.AddFont("SpaceMono-BoldItalic.ttf", "SpaceMonoBoldItalic");
			});

		// Register services and ViewModels
		builder.Services.RegisterServices();
        Preferences.Default.Clear();              // clears app preferences
        SecureStorage.Remove("SavedDevices");

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
