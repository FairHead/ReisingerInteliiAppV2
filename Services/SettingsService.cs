using System.Diagnostics;

namespace ReisingerIntelliApp_V4.Services;

public interface ISettingsService
{
    // Theme Settings
    AppTheme CurrentTheme { get; set; }
    bool IsDarkMode { get; }
    void SetTheme(AppTheme theme);
    
    // Font Settings
    double FontScale { get; set; }
    double BaseFontSize { get; }
    double GetScaledFontSize(double baseSize);
    
    // Language Settings
    string CurrentLanguage { get; set; }
    
    // Notification Settings
    bool NotificationsEnabled { get; set; }
    
    // Connection Settings
    int DefaultConnectionTimeout { get; set; }
    bool AutoReconnect { get; set; }
    
    // Device Settings
    bool ShowOfflineDevices { get; set; }
    bool AutoScanOnStartup { get; set; }
    
    // Reset
    void ResetToDefaults();
}

public class SettingsService : ISettingsService
{
    private const string ThemeKey = "app_theme";
    private const string FontScaleKey = "font_scale";
    private const string LanguageKey = "language";
    private const string NotificationsKey = "notifications_enabled";
    private const string ConnectionTimeoutKey = "connection_timeout";
    private const string AutoReconnectKey = "auto_reconnect";
    private const string ShowOfflineDevicesKey = "show_offline_devices";
    private const string AutoScanKey = "auto_scan_startup";
    
    // Default values
    private const double DefaultFontScale = 1.0;
    private const double DefaultBaseFontSize = 14.0;
    private const string DefaultLanguage = "de-DE";
    private const int DefaultTimeoutSeconds = 5;
    
    public AppTheme CurrentTheme
    {
        get
        {
            var themeString = Preferences.Get(ThemeKey, AppTheme.Dark.ToString());
            return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme : AppTheme.Dark;
        }
        set
        {
            Preferences.Set(ThemeKey, value.ToString());
            ApplyTheme(value);
            Debug.WriteLine($"?? Theme changed to: {value}");
        }
    }
    
    public bool IsDarkMode => CurrentTheme == AppTheme.Dark;
    
    public double FontScale
    {
        get => Preferences.Get(FontScaleKey, DefaultFontScale);
        set
        {
            var clampedValue = Math.Clamp(value, 0.8, 1.5);
            Preferences.Set(FontScaleKey, clampedValue);
            Debug.WriteLine($"?? Font scale changed to: {clampedValue}");
        }
    }
    
    public double BaseFontSize => DefaultBaseFontSize;
    
    public string CurrentLanguage
    {
        get => Preferences.Get(LanguageKey, DefaultLanguage);
        set
        {
            Preferences.Set(LanguageKey, value);
            Debug.WriteLine($"?? Language changed to: {value}");
        }
    }
    
    public bool NotificationsEnabled
    {
        get => Preferences.Get(NotificationsKey, true);
        set
        {
            Preferences.Set(NotificationsKey, value);
            Debug.WriteLine($"?? Notifications: {(value ? "Enabled" : "Disabled")}");
        }
    }
    
    public int DefaultConnectionTimeout
    {
        get => Preferences.Get(ConnectionTimeoutKey, DefaultTimeoutSeconds);
        set
        {
            var clampedValue = Math.Clamp(value, 3, 30);
            Preferences.Set(ConnectionTimeoutKey, clampedValue);
            Debug.WriteLine($"?? Connection timeout changed to: {clampedValue}s");
        }
    }
    
    public bool AutoReconnect
    {
        get => Preferences.Get(AutoReconnectKey, true);
        set
        {
            Preferences.Set(AutoReconnectKey, value);
            Debug.WriteLine($"?? Auto-reconnect: {(value ? "Enabled" : "Disabled")}");
        }
    }
    
    public bool ShowOfflineDevices
    {
        get => Preferences.Get(ShowOfflineDevicesKey, true);
        set
        {
            Preferences.Set(ShowOfflineDevicesKey, value);
            Debug.WriteLine($"??? Show offline devices: {(value ? "Enabled" : "Disabled")}");
        }
    }
    
    public bool AutoScanOnStartup
    {
        get => Preferences.Get(AutoScanKey, false);
        set
        {
            Preferences.Set(AutoScanKey, value);
            Debug.WriteLine($"?? Auto-scan on startup: {(value ? "Enabled" : "Disabled")}");
        }
    }
    
    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
    }
    
    public double GetScaledFontSize(double baseSize)
    {
        return baseSize * FontScale;
    }
    
    public void ResetToDefaults()
    {
        Debug.WriteLine("?? Resetting all settings to defaults");
        
        CurrentTheme = AppTheme.Dark;
        FontScale = DefaultFontScale;
        CurrentLanguage = DefaultLanguage;
        NotificationsEnabled = true;
        DefaultConnectionTimeout = DefaultTimeoutSeconds;
        AutoReconnect = true;
        ShowOfflineDevices = true;
        AutoScanOnStartup = false;
        
        Debug.WriteLine("? Settings reset complete");
    }
    
    private void ApplyTheme(AppTheme theme)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = theme;
        }
    }
    
    // Constructor - Apply saved theme on startup
    public SettingsService()
    {
        ApplyTheme(CurrentTheme);
    }
}
