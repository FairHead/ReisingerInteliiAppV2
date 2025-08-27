using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Services;

public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateBackAsync();
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
}

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
            await page.DisplayAlert(title, message, cancel);
    }
}
