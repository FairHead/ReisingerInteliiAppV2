# Data Model Analysis: Repository Audit Implementation

**Date**: 2025-01-11  
**Phase**: 1 - Design  
**Scope**: Data models, entities, and relationships for audit and enhancement features

## Current Data Models (Existing ✅)

### Core Application Models

#### DeviceModel
```csharp
// Location: Models/DeviceModel.cs
public class DeviceModel
{
    public string Name { get; set; }
    public string MacAddress { get; set; }  // For WiFi devices
    public string IpAddress { get; set; }   // For local network devices
    public AppDeviceType Type { get; set; }
    public DeviceStatus Status { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsConnected { get; set; }
}
```

#### PlacedDeviceModel
```csharp
// Location: Models/PlacedDeviceModel.cs
public class PlacedDeviceModel : DeviceModel
{
    public double RelativeX { get; set; }    // 0.0 to 1.0 on floor plan
    public double RelativeY { get; set; }    // 0.0 to 1.0 on floor plan
    public double Scale { get; set; }        // Device visual scale
    public string FloorId { get; set; }      // Associated floor
    public DateTime PlacedAt { get; set; }
    public bool IsSelected { get; set; }     // UI state
}
```

#### Building & Floor Models
```csharp
// Location: Models/Building.cs
public class Building
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public List<Floor> Floors { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

// Location: Models/Floor.cs  
public class Floor
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string BuildingId { get; set; }
    public string PdfPath { get; set; }      // Floor plan PDF
    public List<PlacedDeviceModel> Devices { get; set; }
    public int Level { get; set; }           // Floor number
    public bool IsActive { get; set; }
}
```

#### UI Models
```csharp
// Location: Models/TabItemModel.cs
public class TabItemModel
{
    public string Title { get; set; }
    public string IconSource { get; set; }
    public bool IsSelected { get; set; }
    public bool IsEnabled { get; set; }
}
```

### API & Network Models

#### IntellidriveApiModels
```csharp
// Location: Models/IntellidriveApiModels.cs
public class DeviceStatusResponse
{
    public bool IsOpen { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string ErrorMessage { get; set; }
}

public class DeviceParameterGroup
{
    public string GroupName { get; set; }
    public List<DeviceParameter> Parameters { get; set; }
}

public class DeviceParameter
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }        // int, float, bool, string
    public object MinValue { get; set; }
    public object MaxValue { get; set; }
    public bool IsReadOnly { get; set; }
    public string Description { get; set; }
}
```

#### Network Scanning Models
```csharp
// Location: Models/NetworkDataModel.cs
public class NetworkScanResult
{
    public List<LocalNetworkDeviceModel> Devices { get; set; }
    public DateTime ScanStarted { get; set; }
    public DateTime ScanCompleted { get; set; }
    public string NetworkRange { get; set; }
    public int DevicesFound { get; set; }
}

// Location: Models/LocalNetworkDeviceModel.cs
public class LocalNetworkDeviceModel : DeviceModel
{
    public string Hostname { get; set; }
    public int Port { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string NetworkInterface { get; set; }
}
```

## Enhanced Data Models (New 🆕)

### Enterprise & Configuration Models

#### AppSettings
```csharp
// New: Models/AppSettings.cs
public class AppSettings
{
    public Theme Theme { get; set; } = Theme.System;
    public FontSize FontSize { get; set; } = FontSize.Medium;
    public Language Language { get; set; } = Language.English;
    public bool CrashReportingEnabled { get; set; } = false;
    public bool TelemetryEnabled { get; set; } = false;
    public bool PerformanceMonitoringEnabled { get; set; } = false;
    public bool GraphicsViewEnabled { get; set; } = false;  // For 50+ devices
}

public enum Theme { Light, Dark, System }
public enum FontSize { Small, Medium, Large, ExtraLarge }
public enum Language { English, German }
```

#### Performance Models
```csharp
// New: Models/PerformanceMetrics.cs
public class PerformanceMetrics
{
    public double FrameRate { get; set; }
    public TimeSpan LoadTime { get; set; }
    public long MemoryUsage { get; set; }
    public int DeviceCount { get; set; }
    public DateTime Timestamp { get; set; }
    public string PageName { get; set; }
    public string Operation { get; set; }  // "pan", "zoom", "load", etc.
}

public class PerformanceBudget
{
    public double MinFrameRate { get; set; } = 55.0;
    public TimeSpan MaxLoadTime { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public long MaxMemoryUsage { get; set; } = 100 * 1024 * 1024; // 100MB
}
```

#### Logging Models
```csharp
// New: Models/LogEntry.cs
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
    public string Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public string Source { get; set; }      // File name or class
    public int? LineNumber { get; set; }
}

public class LogFilter
{
    public LogLevel? MinLevel { get; set; }
    public string Category { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string SearchText { get; set; }
}
```

### Error & State Management Models

#### Error Handling
```csharp
// New: Models/AppError.cs
public class AppError
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Message { get; set; }
    public ErrorSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
    public string StackTrace { get; set; }
    public Dictionary<string, object> Context { get; set; }
    public bool IsCritical { get; set; }
    public bool IsUserReported { get; set; }
}

public enum ErrorSeverity { Info, Warning, Error, Critical }
```

#### Device State Management
```csharp
// Enhanced: Models/DeviceState.cs
public class DeviceState
{
    public string DeviceId { get; set; }
    public DeviceStatus Status { get; set; }
    public bool IsOpen { get; set; }
    public DateTime LastUpdated { get; set; }
    public string LastError { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public int RetryCount { get; set; }
    public Color StatusColor => GetStatusColor();
    
    private Color GetStatusColor()
    {
        return Status switch
        {
            DeviceStatus.Connected => IsOpen ? Colors.Green : Colors.Red,
            DeviceStatus.Connecting => Colors.Blue,
            DeviceStatus.Error => Colors.Red,
            _ => Colors.Gray
        };
    }
}

public enum DeviceStatus 
{ 
    Unknown, 
    Scanning, 
    Found, 
    Connecting, 
    Connected, 
    Error, 
    Timeout 
}
```

## Data Validation Rules

### Input Validation
- **RelativeX, RelativeY**: Must be between 0.0 and 1.0
- **IP Addresses**: Must be valid IPv4 format
- **MAC Addresses**: Must be valid MAC format (XX:XX:XX:XX:XX:XX)
- **Device Names**: 1-50 characters, no special characters
- **File Paths**: Must exist and be readable

### Business Rules
- **Device Placement**: Cannot place device outside floor plan bounds
- **Device Scaling**: Scale factor between 0.1 and 3.0
- **Performance Metrics**: Frame rate must be positive, memory usage > 0
- **Log Retention**: Maximum 1000 entries in memory, older entries archived

## Data Relationships

### Entity Relationships
```
Building (1) ←→ (N) Floor
Floor (1) ←→ (N) PlacedDeviceModel
DeviceModel (1) ←→ (1) DeviceState
AppSettings (1) ←→ (1) User Session
LogEntry (N) ←→ (1) LogFilter
```

### Data Flow Patterns
1. **Device Scanning**: NetworkScanResult → DeviceModel → PlacedDeviceModel
2. **State Updates**: IntellidriveApiService → DeviceState → UI
3. **Performance Monitoring**: UI Events → PerformanceMetrics → Logging
4. **Settings**: AppSettings → Preferences API → Service Configuration

## Migration Considerations

### Existing Data Compatibility
- **Maintain backward compatibility** for Building/Floor/PlacedDevice models
- **Add optional properties** for new features
- **Provide default values** for enhanced properties
- **Version management** for data schema changes

### Performance Impact
- **Lazy loading** for large device collections
- **Caching strategies** for frequently accessed data
- **Memory management** for log entries and performance metrics
- **Batching updates** for state changes

## Validation & Testing Strategy

### Model Validation
- **Unit tests** for all data model classes
- **Property validation** tests (range checking, format validation)
- **Serialization tests** for API models
- **Performance tests** for large collections

### Integration Testing
- **Data persistence** round-trip tests
- **API contract** validation
- **Cross-platform** data compatibility
- **Memory usage** monitoring

---

**Phase 1 Status**: ✅ Data Model Analysis Complete  
**Next**: Create contracts/ directory and API contracts  
**Dependencies**: None - ready for contract generation
