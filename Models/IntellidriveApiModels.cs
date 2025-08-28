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
    [JsonPropertyName("Values")] public List<IntellidriveParameterValue>? Values { get; set; }
}

public class IntellidriveParameterValue
{
    [JsonPropertyName("Id")] public int Id { get; set; }
    // Some values can be numbers/strings/bools; keep as JsonElement and consumers can convert
    [JsonPropertyName("V")] public JsonElement V { get; set; }
}
