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
			var sb = new StringBuilder();
			sb.AppendLine($"[{DateTime.UtcNow:o}] {source}");
			if (ex != null)
			{
				sb.AppendLine(ex.ToString());
			}
			else
			{
				sb.AppendLine("Unknown exception");
			}

			var path = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
			File.AppendAllText(path, sb.ToString());
			Debug.WriteLine($"[Crash] {source}: {(ex?.Message ?? "Unknown")} → {path}");
		}
		catch
		{
			// Swallow logging errors to avoid recursive crashes
		}
	}
}