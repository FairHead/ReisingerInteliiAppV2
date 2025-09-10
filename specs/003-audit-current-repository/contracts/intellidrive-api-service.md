# API Contracts: IntellidriveApiService Enhancement

**Contract Version**: 1.0  
**Date**: 2025-01-11  
**Scope**: Enhanced device control and state synchronization APIs

## Service Interface Contract

### IIntellidriveApiService
```csharp
public interface IIntellidriveApiService
{
    // Device Discovery & Health
    Task<bool> IsDeviceReachableAsync(string ipAddress, CancellationToken ct = default);
    Task<IntellidriveVersionResponse> GetVersionAsync(string ipAddress, CancellationToken ct = default);
    
    // Device Control (Enhanced)
    Task<DeviceControlResponse> OpenDeviceAsync(string ipAddress, string username, string password, CancellationToken ct = default);
    Task<DeviceControlResponse> CloseDeviceAsync(string ipAddress, string username, string password, CancellationToken ct = default);
    Task<DeviceStatusResponse> GetDeviceStatusAsync(string ipAddress, string username, string password, CancellationToken ct = default);
    
    // Device Parameters (New)
    Task<List<DeviceParameterGroup>> GetParameterGroupsAsync(string ipAddress, string username, string password, CancellationToken ct = default);
    Task<DeviceParameter> GetParameterAsync(string ipAddress, string username, string password, string parameterName, CancellationToken ct = default);
    Task<bool> SetParameterAsync(string ipAddress, string username, string password, string parameterName, object value, CancellationToken ct = default);
    
    // Batch Operations (New)
    Task<Dictionary<string, DeviceStatusResponse>> GetMultipleDeviceStatusAsync(IEnumerable<DeviceCredentials> devices, CancellationToken ct = default);
    Task<Dictionary<string, DeviceControlResponse>> ExecuteBatchCommandAsync(IEnumerable<DeviceCommand> commands, CancellationToken ct = default);
}
```

## Request/Response Contracts

### Device Control Operations

#### OpenDevice Request
```json
POST /api/control/open
Authorization: User {username}:{password}
Content-Type: application/json

{
    "command": "open",
    "timestamp": "2025-01-11T10:30:00Z",
    "requestId": "uuid-string"
}
```

#### DeviceControlResponse
```json
{
    "success": true,
    "status": "opened",
    "timestamp": "2025-01-11T10:30:01Z",
    "requestId": "uuid-string",
    "responseTime": 250,
    "errorMessage": null,
    "deviceState": {
        "isOpen": true,
        "lastAction": "open",
        "batteryLevel": 85,
        "signalStrength": -45
    }
}
```

### Device Status Query

#### GetStatus Request
```json
GET /api/status
Authorization: User {username}:{password}
Accept: application/json
```

#### DeviceStatusResponse
```json
{
    "deviceId": "device-mac-or-serial",
    "isOpen": true,
    "status": "connected",
    "timestamp": "2025-01-11T10:30:00Z",
    "batteryLevel": 85,
    "signalStrength": -45,
    "lastAction": "open",
    "lastActionTime": "2025-01-11T10:29:30Z",
    "firmwareVersion": "1.2.3",
    "errorCode": null,
    "errorMessage": null
}
```

### Device Parameters (New)

#### GetParameterGroups Request
```json
GET /api/parameters/groups
Authorization: User {username}:{password}
Accept: application/json
```

#### ParameterGroups Response
```json
{
    "groups": [
        {
            "groupName": "Motor Settings",
            "parameters": [
                {
                    "name": "motorSpeed",
                    "displayName": "Motor Speed",
                    "value": 1500,
                    "type": "int",
                    "minValue": 100,
                    "maxValue": 3000,
                    "unit": "rpm",
                    "isReadOnly": false,
                    "description": "Motor rotation speed in RPM"
                },
                {
                    "name": "motorDirection",
                    "displayName": "Motor Direction",
                    "value": "clockwise",
                    "type": "enum",
                    "allowedValues": ["clockwise", "counterclockwise"],
                    "isReadOnly": false,
                    "description": "Motor rotation direction"
                }
            ]
        },
        {
            "groupName": "Safety Settings",
            "parameters": [
                {
                    "name": "forceLimit",
                    "displayName": "Force Limit",
                    "value": 50.5,
                    "type": "float",
                    "minValue": 10.0,
                    "maxValue": 100.0,
                    "unit": "N",
                    "isReadOnly": false,
                    "description": "Maximum force before safety stop"
                }
            ]
        }
    ]
}
```

#### SetParameter Request
```json
PUT /api/parameters/{parameterName}
Authorization: User {username}:{password}
Content-Type: application/json

{
    "value": 2000,
    "validateRange": true,
    "timestamp": "2025-01-11T10:30:00Z"
}
```

#### SetParameter Response
```json
{
    "success": true,
    "parameterName": "motorSpeed",
    "oldValue": 1500,
    "newValue": 2000,
    "timestamp": "2025-01-11T10:30:01Z",
    "validationPassed": true,
    "errorMessage": null
}
```

## Error Response Contract

### Standard Error Response
```json
{
    "success": false,
    "errorCode": "DEVICE_UNREACHABLE",
    "errorMessage": "Device at 192.168.1.100 did not respond within timeout",
    "timestamp": "2025-01-11T10:30:00Z",
    "requestId": "uuid-string",
    "retryAfter": 5000,
    "supportContact": "support@reisinger.com"
}
```

### Error Codes
| Code | Description | User Action |
|------|-------------|-------------|
| `DEVICE_UNREACHABLE` | Network timeout or device offline | Check network connection |
| `INVALID_CREDENTIALS` | Username/password incorrect | Verify credentials |
| `PARAMETER_OUT_OF_RANGE` | Parameter value outside allowed range | Check min/max values |
| `DEVICE_BUSY` | Device is executing another command | Wait and retry |
| `FIRMWARE_ERROR` | Device firmware reported error | Contact support |
| `NETWORK_ERROR` | Network communication failed | Check connectivity |

## Batch Operations (New)

### BatchCommand Request
```json
POST /api/control/batch
Authorization: User {username}:{password}
Content-Type: application/json

{
    "commands": [
        {
            "deviceIp": "192.168.1.100",
            "command": "open",
            "credentials": {
                "username": "admin",
                "password": "password123"
            }
        },
        {
            "deviceIp": "192.168.1.101", 
            "command": "close",
            "credentials": {
                "username": "admin",
                "password": "password123"
            }
        }
    ],
    "maxConcurrency": 5,
    "timeout": 10000
}
```

### BatchCommand Response
```json
{
    "results": {
        "192.168.1.100": {
            "success": true,
            "status": "opened",
            "responseTime": 250
        },
        "192.168.1.101": {
            "success": false,
            "errorCode": "DEVICE_UNREACHABLE",
            "errorMessage": "Timeout after 10 seconds"
        }
    },
    "summary": {
        "totalCommands": 2,
        "successCount": 1,
        "failureCount": 1,
        "averageResponseTime": 250
    }
}
```

## Service Implementation Contract

### Authentication & Security
- **HTTPS Required**: All production API calls must use HTTPS
- **Credential Storage**: Use SecureStorage for device credentials
- **Token Expiry**: Handle authentication token refresh
- **Rate Limiting**: Respect device rate limits (max 10 requests/second)

### Error Handling
- **Timeout**: 10 seconds default, configurable per call
- **Retry Logic**: Exponential backoff for transient errors
- **Circuit Breaker**: Stop requests to failing devices temporarily
- **User Feedback**: Non-blocking error toasts for failures

### Performance Requirements
- **Response Time**: API calls complete within 500ms (95th percentile)
- **Concurrent Requests**: Support up to 50 simultaneous device connections
- **Memory Usage**: Minimal allocation during API calls
- **Caching**: Cache device status for 30 seconds to reduce network load

### Testing Contracts
- **Unit Tests**: Mock HttpClient for isolated testing
- **Integration Tests**: Real HTTP requests to mock server
- **Performance Tests**: Measure response times under load
- **Error Simulation**: Test all error scenarios and codes

---

**Contract Status**: ✅ COMPLETE  
**Version**: 1.0  
**Breaking Changes**: None (extends existing API)  
**Testing Required**: Unit, Integration, Performance
