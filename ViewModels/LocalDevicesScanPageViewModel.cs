using System.Windows.Input;

namespace ReisingerIntelliApp_V4.ViewModels;

public class LocalDevicesScanPageViewModel : BaseViewModel
{
    public LocalDevicesScanPageViewModel()
    {
        Title = "Local Devices Scan";
        HomeCommand = new Command(OnHomeTapped);
        RefreshCommand = new Command(OnRefreshTapped);
        SettingsCommand = new Command(OnSettingsTapped);
        BackCommand = new Command(OnBackButtonClicked);
        ScanCommand = new Command(OnScanButtonClicked);
    }

    public ICommand HomeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ScanCommand { get; }

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
                await page.DisplayAlert("Refresh", "Refreshing local devices scan...", "OK");
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

    private async void OnScanButtonClicked()
    {
        IsBusy = true;
        try
        {
            if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
                await page.DisplayAlert("Scan", "Starting local devices scan...", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
