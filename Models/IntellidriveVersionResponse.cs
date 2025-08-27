using System.Text.Json.Serialization;

namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Response model for Intellidrive version endpoint (/intellidrive/version)
/// </summary>
public class IntellidriveVersionResponse
{
    [JsonPropertyName("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("LatestFirmware")]
    public bool LatestFirmware { get; set; }

    [JsonPropertyName("FirmwareVersion")]
    public string FirmwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("Content")]
    public IntellidriveVersionContent? Content { get; set; }
}

/// <summary>
/// Content section of the Intellidrive version response
/// </summary>
public class IntellidriveVersionContent
{
    [JsonPropertyName("DEVICE_SERIALNO")]
    public string DeviceSerialNumber { get; set; } = string.Empty;
}
