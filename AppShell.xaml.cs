using ReisingerIntelliApp_V4.Views;

namespace ReisingerIntelliApp_V4;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register navigation routes for scan pages
		Routing.RegisterRoute("wifiscan", typeof(WifiScanPage));
		Routing.RegisterRoute("localscan", typeof(LocalDevicesScanPage));
		Routing.RegisterRoute("savedevice", typeof(SaveDevicePage));
	}
}
