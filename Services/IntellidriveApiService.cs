using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Services;

/// <summary>
/// Service for communicating with Intellidrive devices
/// Handles both WiFi devices (192.168.4.100) and local network devices (variable IP)
/// </summary>
public class IntellidriveApiService
{
    private readonly HttpClient _httpClient;
    private const string WifiDeviceDefaultIp = "192.168.4.100";
    private const int RequestTimeoutSeconds = 10;

    public IntellidriveApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
    }

    /// <summary>
    /// Tests if an Intellidrive device is reachable at the specified IP address
    /// </summary>
    /// <param name="ipAddress">IP address of the device (defaults to WiFi device IP if null)</param>
    /// <returns>Tuple indicating success and the device response or error message</returns>
    public async Task<(bool success, IntellidriveVersionResponse? response, string message)> TestIntellidriveConnectionAsync(string? ipAddress = null)
    {
        var targetIp = ipAddress ?? WifiDeviceDefaultIp;
        var endpoint = $"http://{targetIp}/intellidrive/version";
        
        Debug.WriteLine($"🔄 Testing Intellidrive connection to: {endpoint}");

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                Debug.WriteLine($"❌ HTTP Error: {errorMessage}");
                return (false, null, errorMessage);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"📥 Response received: {jsonContent}");

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                const string errorMessage = "Empty response from device";
                Debug.WriteLine($"❌ {errorMessage}");
                return (false, null, errorMessage);
            }

            try
            {
                var versionResponse = JsonSerializer.Deserialize<IntellidriveVersionResponse>(jsonContent);
                
                if (versionResponse == null)
                {
                    const string errorMessage = "Failed to deserialize device response";
                    Debug.WriteLine($"❌ {errorMessage}");
                    return (false, null, errorMessage);
                }

                if (!versionResponse.Success)
                {
                    var errorMessage = $"Device reported error: {versionResponse.Message}";
                    Debug.WriteLine($"❌ {errorMessage}");
                    return (false, versionResponse, errorMessage);
                }

                Debug.WriteLine($"✅ Intellidrive device found - DeviceId: {versionResponse.DeviceId}, Version: {versionResponse.Message}");
                return (true, versionResponse, "Intellidrive device successfully detected");
            }
            catch (JsonException jsonEx)
            {
                var errorMessage = $"Invalid JSON response: {jsonEx.Message}";
                Debug.WriteLine($"❌ {errorMessage}");
                return (false, null, errorMessage);
            }
        }
        catch (HttpRequestException httpEx)
        {
            var errorMessage = $"Network error: {httpEx.Message}";
            Debug.WriteLine($"❌ {errorMessage}");
            return (false, null, errorMessage);
        }
        catch (TaskCanceledException timeoutEx)
        {
            var errorMessage = timeoutEx.InnerException is TimeoutException 
                ? $"Request timeout after {RequestTimeoutSeconds} seconds"
                : "Request was cancelled";
            Debug.WriteLine($"❌ {errorMessage}");
            return (false, null, errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error: {ex.Message}";
            Debug.WriteLine($"❌ {errorMessage}");
            return (false, null, errorMessage);
        }
    }

    /// <summary>
    /// Tests if an Intellidrive WiFi device is reachable at the default WiFi IP (192.168.4.100)
    /// </summary>
    /// <returns>Tuple indicating success and the device response or error message</returns>
    public async Task<(bool success, IntellidriveVersionResponse? response, string message)> TestWifiIntellidriveConnectionAsync()
    {
        return await TestIntellidriveConnectionAsync(WifiDeviceDefaultIp);
    }

    /// <summary>
    /// Gets device version information from an Intellidrive device
    /// </summary>
    /// <param name="ipAddress">IP address of the device</param>
    /// <returns>Version response or null if failed</returns>
    public async Task<IntellidriveVersionResponse?> GetDeviceVersionAsync(string ipAddress)
    {
        var (success, response, _) = await TestIntellidriveConnectionAsync(ipAddress);
        return success ? response : null;
    }

    /// <summary>
    /// Gets device version information from a WiFi Intellidrive device
    /// </summary>
    /// <returns>Version response or null if failed</returns>
    public async Task<IntellidriveVersionResponse?> GetWifiDeviceVersionAsync()
    {
        return await GetDeviceVersionAsync(WifiDeviceDefaultIp);
    }

    // Future endpoints can be added here following the same pattern:
    // - /intellidrive/status
    // - /intellidrive/control
    // - /intellidrive/config
    // etc.
}
