using System.Windows.Input;

namespace ReisingerIntelliApp_V4.ViewModels;

public class WifiScanPageViewModel : BaseViewModel
{
    public WifiScanPageViewModel()
    {
        Title = "WiFi Scan";
        HomeCommand = new Command(OnHomeTapped);
        RefreshCommand = new Command(OnRefreshTapped);
        SettingsCommand = new Command(OnSettingsTapped);
        BackCommand = new Command(OnBackButtonClicked);
    }

    public ICommand HomeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand BackCommand { get; }

    private async void OnHomeTapped()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnRefreshTapped()
    {
        IsBusy = true;
        try
        {
            if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
                await page.DisplayAlert("Refresh", "Refreshing WiFi scan...", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnSettingsTapped()
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            await page.DisplayAlert("Settings", "Opening settings...", "OK");
    }

    private async void OnBackButtonClicked()
    {
        await Shell.Current.GoToAsync("..");
    }
}
