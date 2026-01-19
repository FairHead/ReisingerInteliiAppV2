using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReisingerIntelliApp_V4.Models;

// Generic envelope used by many Intellidrive endpoints
public class IntellidriveApiEnvelope
{
    [JsonPropertyName("DeviceId")] public string DeviceId { get; set; } = string.Empty;
    [JsonPropertyName("Success")] public bool Success { get; set; }
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("LatestFirmware")] public bool LatestFirmware { get; set; }
    [JsonPropertyName("FirmwareVersion")] public string FirmwareVersion { get; set; } = string.Empty;
    [JsonPropertyName("Content")] public JsonElement? Content { get; set; }
}

// Parameters API response
public class IntellidriveParametersResponse
{
    [JsonPropertyName("Success")] public bool Success { get; set; }
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("Values")] public List<IntellidriveParameterValue>? Values { get; set; }
    [JsonPropertyName("Units")] public List<object>? Units { get; set; }
}

public class IntellidriveParameterValue
{
    [JsonPropertyName("Id")] public int Id { get; set; }
    // Some values can be numbers/strings/bools; keep as JsonElement and consumers can convert
    [JsonPropertyName("V")] public JsonElement V { get; set; }
}

/// <summary>
/// Response for /intellidrive/parameters/min-values endpoint
/// Contains device-specific minimum values for variable-range parameters
/// </summary>
public class IntellidriveMinValuesResponse
{
    [JsonPropertyName("Success")] public bool Success { get; set; }
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("Values")] public List<IntellidriveParameterValue>? Values { get; set; }
}

/// <summary>
/// Response for /intellidrive/parameters/max-values endpoint
/// Contains device-specific maximum values for variable-range parameters
/// </summary>
public class IntellidriveMaxValuesResponse
{
    [JsonPropertyName("Success")] public bool Success { get; set; }
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("Values")] public List<IntellidriveParameterValue>? Values { get; set; }
}

/// <summary>
/// Request model for POST /intellidrive/parameters/set endpoint.
/// Matches the expected JSON format from the IntelliDrive firmware:
/// {
///   "Success": true,
///   "Message": "",
///   "Values": [{"Id": 1, "V": 12345}, ...],
///   "Units": []
/// }
/// </summary>
public class IntellidriveSetParametersRequest
{
    [JsonPropertyName("Success")] public bool Success { get; set; } = true;
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("Values")] public List<IntellidriveParameterSetValue> Values { get; set; } = new();
    [JsonPropertyName("Units")] public List<object> Units { get; set; } = new();
}

/// <summary>
/// Parameter value item for set-parameters request.
/// Uses "V" as value key to match firmware expectation.
/// </summary>
public class IntellidriveParameterSetValue
{
    [JsonPropertyName("Id")] public int Id { get; set; }
    [JsonPropertyName("V")] public uint V { get; set; }
}

/// <summary>
/// Response from POST /intellidrive/parameters/set endpoint.
/// </summary>
public class IntellidriveSetParametersResponse
{
    [JsonPropertyName("Success")] public bool Success { get; set; }
    [JsonPropertyName("Message")] public string Message { get; set; } = string.Empty;
}
