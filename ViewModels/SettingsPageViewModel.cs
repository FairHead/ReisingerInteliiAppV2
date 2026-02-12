using ReisingerIntelliApp_V4.Services;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.ViewModels;

public class SettingsPageViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    
    public SettingsPageViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Title = "Einstellungen";
        
        Debug.WriteLine($"?? SettingsPageViewModel initialized - Current theme: {_settingsService.CurrentTheme}");
    }
    
    public bool IsDarkMode
    {
        get => _settingsService.CurrentTheme == AppTheme.Dark;
        set
        {
            if (value && _settingsService.CurrentTheme != AppTheme.Dark)
            {
                Debug.WriteLine("?? Switching to Dark Mode");
                _settingsService.SetTheme(AppTheme.Dark);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightMode));
            }
        }
    }
    
    public bool IsLightMode
    {
        get => _settingsService.CurrentTheme == AppTheme.Light;
        set
        {
            if (value && _settingsService.CurrentTheme != AppTheme.Light)
            {
                Debug.WriteLine("?? Switching to Light Mode");
                _settingsService.SetTheme(AppTheme.Light);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDarkMode));
            }
        }
    }
}
