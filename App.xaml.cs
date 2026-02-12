using System.Diagnostics;
using System.Text;
using Microsoft.Maui.Storage;

namespace ReisingerIntelliApp_V4;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Global exception handlers to capture crashes early at startup
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	// Note: MAUI Application doesn't expose a cross-platform UnhandledException event.
	// We rely on AppDomain and TaskScheduler hooks above.

		Debug.WriteLine("[App] Initialized. AppDataDirectory=" + FileSystem.AppDataDirectory);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		try
		{
			var ex = e.ExceptionObject as Exception;
			LogCrash("AppDomain.UnhandledException", ex);
		}
		catch { /* ignore */ }
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		try
		{
			LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
			e.SetObserved();
		}
		catch { /* ignore */ }
	}

    

	private static void LogCrash(string source, Exception? ex)
	{
		try
		{
			var msg = ex?.ToString() ?? "Unknown exception";
			var sb = new StringBuilder();
			sb.AppendLine($"[{DateTime.UtcNow:o}] {source}");
			sb.AppendLine(msg);

			var path = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
			File.AppendAllText(path, sb.ToString());
			Console.WriteLine($"💥 CRASH [{source}]: {msg}");

			// Show crash on screen so user can report it
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					var page = Current?.Windows.FirstOrDefault()?.Page;
					if (page != null)
					{
						var short_msg = ex?.Message ?? "Unknown";
						var type = ex?.GetType().Name ?? "?";
						await page.DisplayAlert(
							$"Crash: {type}",
							$"{source}\n\n{short_msg}\n\nStackTrace:\n{ex?.StackTrace?[..Math.Min(ex.StackTrace.Length, 500)]}",
							"OK");
					}
				}
				catch { }
			});
		}
		catch
		{
			// Swallow logging errors to avoid recursive crashes
		}
	}
}