# Service Contracts: Core Application Services

**Contract Version**: 1.0  
**Date**: 2025-01-11  
**Scope**: Service interfaces for repository audit implementation

## Performance Monitoring Service

### IPerformanceMonitoringService
```csharp
public interface IPerformanceMonitoringService
{
    // Frame Rate Monitoring
    void StartFrameRateMonitoring(string context);
    void StopFrameRateMonitoring();
    Task<double> GetCurrentFrameRateAsync();
    
    // Memory Monitoring  
    Task<long> GetCurrentMemoryUsageAsync();
    void StartMemoryMonitoring();
    void StopMemoryMonitoring();
    
    // Load Time Tracking
    void StartLoadTimer(string operation);
    TimeSpan StopLoadTimer(string operation);
    
    // Performance Metrics
    Task<PerformanceMetrics> GetCurrentMetricsAsync();
    Task<List<PerformanceMetrics>> GetHistoryAsync(TimeSpan period);
    
    // Budget Validation
    Task<bool> ValidatePerformanceBudgetAsync(PerformanceBudget budget);
    event EventHandler<PerformanceBudgetViolationEventArgs> BudgetViolated;
}
```

## Logging Service

### ILoggingService
```csharp
public interface ILoggingService
{
    // Structured Logging
    void LogDebug(string message, object? data = null, [CallerMemberName] string? source = null);
    void LogInfo(string message, object? data = null, [CallerMemberName] string? source = null);
    void LogWarning(string message, object? data = null, [CallerMemberName] string? source = null);
    void LogError(string message, Exception? exception = null, object? data = null, [CallerMemberName] string? source = null);
    void LogCritical(string message, Exception? exception = null, object? data = null, [CallerMemberName] string? source = null);
    
    // Log Management
    Task<List<LogEntry>> GetLogsAsync(LogFilter? filter = null);
    Task<int> GetLogCountAsync(LogLevel? minLevel = null);
    Task ClearLogsAsync();
    Task<string> ExportLogsAsync(LogFilter? filter = null);
    
    // Configuration
    void SetMinimumLogLevel(LogLevel level);
    void EnableCategory(string category);
    void DisableCategory(string category);
}
```

## Settings & Preferences Service

### IPreferencesService
```csharp
public interface IPreferencesService
{
    // App Settings
    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);
    
    // Individual Settings
    Task<T> GetAsync<T>(string key, T defaultValue = default);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
    Task ClearAsync();
    
    // Settings Events
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    
    // Theme & Localization
    Task SetThemeAsync(Theme theme);
    Task SetLanguageAsync(Language language);
    Task SetFontSizeAsync(FontSize fontSize);
    
    // Performance Settings
    Task EnableGraphicsViewAsync(bool enabled);
    Task SetPerformanceMonitoringAsync(bool enabled);
}
```

## Error & Crash Reporting Service

### ICrashReportingService
```csharp
public interface ICrashReportingService
{
    // Crash Reporting
    Task InitializeAsync(bool userConsent);
    Task ReportCrashAsync(Exception exception, Dictionary<string, object>? context = null);
    Task ReportErrorAsync(AppError error);
    
    // User Consent
    Task<bool> HasUserConsentAsync();
    Task SetUserConsentAsync(bool hasConsent);
    Task ShowConsentDialogAsync();
    
    // Telemetry
    Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null);
    Task TrackPerformanceAsync(string operationName, TimeSpan duration);
    
    // Configuration
    Task SetCrashReportingEnabledAsync(bool enabled);
    Task SetTelemetryEnabledAsync(bool enabled);
}
```

## Device State Management Service

### IDeviceStateService
```csharp
public interface IDeviceStateService
{
    // State Management
    Task<DeviceState> GetDeviceStateAsync(string deviceId);
    Task UpdateDeviceStateAsync(DeviceState state);
    Task<Dictionary<string, DeviceState>> GetAllDeviceStatesAsync();
    
    // State Synchronization
    Task SyncDeviceStateAsync(string deviceId, string ipAddress, string username, string password);
    Task SyncAllDeviceStatesAsync();
    
    // State Events
    event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
    event EventHandler<DeviceErrorEventArgs> DeviceError;
    
    // Batch Operations
    Task<Dictionary<string, DeviceState>> SyncMultipleDevicesAsync(IEnumerable<DeviceCredentials> devices);
    
    // State Persistence
    Task SaveDeviceStatesAsync();
    Task LoadDeviceStatesAsync();
}
```

## UI State Service

### IUIStateService
```csharp
public interface IUIStateService
{
    // Dropdown Management
    Task<string?> GetOpenDropdownAsync();
    Task SetOpenDropdownAsync(string? dropdownId);
    Task CloseAllDropdownsAsync();
    
    // Floor Plan State
    Task<FloorPlanState> GetFloorPlanStateAsync();
    Task UpdateFloorPlanStateAsync(FloorPlanState state);
    
    // Navigation State
    Task<string> GetCurrentPageAsync();
    Task SetCurrentPageAsync(string pageName);
    Task<bool> CanNavigateBackAsync();
    
    // Loading States
    void ShowLoading(string operation);
    void HideLoading(string operation);
    Task<bool> IsLoadingAsync(string operation);
    
    // Error Display
    Task ShowErrorToastAsync(string message, TimeSpan? duration = null);
    Task ShowSuccessToastAsync(string message, TimeSpan? duration = null);
}
```

## Event Contracts

### Performance Events
```csharp
public class PerformanceBudgetViolationEventArgs : EventArgs
{
    public string Operation { get; set; }
    public PerformanceMetrics ActualMetrics { get; set; }
    public PerformanceBudget Budget { get; set; }
    public TimeSpan ViolationDuration { get; set; }
}
```

### Settings Events
```csharp
public class SettingsChangedEventArgs : EventArgs
{
    public string SettingKey { get; set; }
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Device State Events
```csharp
public class DeviceStateChangedEventArgs : EventArgs
{
    public string DeviceId { get; set; }
    public DeviceState OldState { get; set; }
    public DeviceState NewState { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DeviceErrorEventArgs : EventArgs
{
    public string DeviceId { get; set; }
    public string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public ErrorSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Service Registration Contract

### DI Registration Pattern
```csharp
// In Helpers/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        // Performance Services
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
        
        // Logging Services
        services.AddSingleton<ILoggingService, LoggingService>();
        
        // Settings Services
        services.AddSingleton<IPreferencesService, PreferencesService>();
        
        // Error Reporting
        services.AddSingleton<ICrashReportingService, CrashReportingService>();
        
        // State Management
        services.AddSingleton<IDeviceStateService, DeviceStateService>();
        services.AddTransient<IUIStateService, UIStateService>();
        
        return services;
    }
}
```

## Testing Contracts

### Service Mocking Interfaces
```csharp
// All services must be mockable for unit testing
public interface ITestablePerformanceMonitoringService : IPerformanceMonitoringService
{
    void SetMockFrameRate(double frameRate);
    void SetMockMemoryUsage(long memoryUsage);
    void TriggerBudgetViolation(PerformanceBudgetViolationEventArgs args);
}

public interface ITestableLoggingService : ILoggingService
{
    List<LogEntry> GetCapturedLogs();
    void ClearCapturedLogs();
}
```

### Contract Test Requirements
- **Unit Tests**: Each service interface method must have unit tests
- **Integration Tests**: Cross-service interaction testing
- **Performance Tests**: Service performance under load
- **Mock Implementation**: Full mock implementations for testing

---

**Contract Status**: ✅ COMPLETE  
**Dependencies**: Data models, error handling patterns  
**Testing Strategy**: Interface mocking, integration testing  
**Breaking Changes**: None (extends existing architecture)
