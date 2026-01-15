using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
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

    // Build absolute URL for a device IP and path
    private static string Url(string ip, string path) => $"http://{ip.TrimEnd('/')}/{path.TrimStart('/')}";

    private static AuthenticationHeaderValue BuildUserAuth(string username, string password)
        => new AuthenticationHeaderValue("User", $"{username}:{password}");

    private async Task<HttpResponseMessage> SendAuthedGetAsync(string ip, string path, string username, string password, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, Url(ip, path));
        req.Headers.Authorization = BuildUserAuth(username, password);
        req.Headers.Accept.ParseAdd("application/json");
        return await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task<HttpResponseMessage> SendAuthedPostAsync(string ip, string path, string username, string password, HttpContent? content = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, Url(ip, path));
        req.Headers.Authorization = BuildUserAuth(username, password);
        req.Headers.Accept.ParseAdd("application/json");
        if (content != null) req.Content = content;
        return await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
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

    /// <summary>
    /// Gets version information from device at specific IP address with timeout
    /// Used for local network scanning
    /// </summary>
    /// <param name="ipAddress">IP address of the device</param>
    /// <param name="timeoutSeconds">Timeout in seconds for the request</param>
    /// <returns>Version response or null if failed</returns>
    public async Task<IntellidriveVersionResponse?> GetVersionAsync(string ipAddress, int timeoutSeconds = 5)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var endpoint = $"http://{ipAddress}/intellidrive/version";
            
            Debug.WriteLine($"🔍 Scanning device at {ipAddress}");
            
            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var versionResponse = JsonSerializer.Deserialize<IntellidriveVersionResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                Debug.WriteLine($"✅ Device found at {ipAddress}: {versionResponse?.DeviceId}");
                return versionResponse;
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout is expected during scanning
        }
        catch (HttpRequestException)
        {
            // Connection refused is expected during scanning
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Error scanning {ipAddress}: {ex.Message}");
        }
        
        return null;
    }

    // ===== V3 Endpoints ported and ready for future use =====
    public async Task<string> GetSerialNumberAsync(DeviceModel device, CancellationToken ct = default)
    {
        var res = await SendAuthedGetAsync(device.Ip, "/intellidrive/serialnumber", device.Username, device.Password, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> BeepAsync(DeviceModel device, CancellationToken ct = default)
    {
        var res = await SendAuthedPostAsync(device.Ip, "/intellidrive/beep", device.Username, device.Password, null, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> RestartIntellidriveAsync(DeviceModel device, CancellationToken ct = default)
    {
        var res = await SendAuthedPostAsync(device.Ip, "/intellidrive/restart", device.Username, device.Password, null, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> RestartDriveAsync(DeviceModel device, CancellationToken ct = default)
    {
        var res = await SendAuthedPostAsync(device.Ip, "/intellidrive/restart/drive", device.Username, device.Password, null, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    // Door endpoints
    public Task<string> GetDoorStateAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedGetAsync(device.Ip, "/intellidrive/door/state", device.Username, device.Password, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> GetDoorPositionAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedGetAsync(device.Ip, "/intellidrive/door/position", device.Username, device.Password, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> OpenDoorAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/open", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> OpenDoorFullAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/open-full", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> OpenDoorShortAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/open-short", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> CloseDoorAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/open", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> ForceCloseDoorAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/force-close", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> LockDoorAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/lock", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> UnlockDoorAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedPostAsync(device.Ip, "/intellidrive/door/unlock", device.Username, device.Password, null, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    // Parameters - Simple GET without authentication (for direct device access)
    /// <summary>
    /// Gets device parameters directly via IP without authentication.
    /// Used for local network devices that don't require auth.
    /// </summary>
    public async Task<IntellidriveParametersResponse?> GetParametersByIpAsync(string ipAddress, int timeoutSeconds = 10, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            
            var endpoint = $"http://{ipAddress}/intellidrive/parameters";
            Debug.WriteLine($"🔧 Fetching parameters from: {endpoint}");
            
            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                Debug.WriteLine($"📥 Parameters response: {json.Substring(0, Math.Min(200, json.Length))}...");
                
                var parametersResponse = JsonSerializer.Deserialize<IntellidriveParametersResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (parametersResponse?.Success == true)
                {
                    Debug.WriteLine($"✅ Successfully fetched {parametersResponse.Values?.Count ?? 0} parameters");
                    return parametersResponse;
                }
                else
                {
                    Debug.WriteLine($"⚠️ Parameters response was not successful: {parametersResponse?.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"❌ HTTP error fetching parameters: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine($"⏰ Timeout fetching parameters from {ipAddress}");
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"❌ Network error fetching parameters: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error fetching parameters: {ex.Message}");
        }
        
        return null;
    }

    // Parameters
    public async Task<IntellidriveParametersResponse?> GetParametersAsync(DeviceModel device, CancellationToken ct = default)
    {
        var res = await SendAuthedGetAsync(device.Ip, "/intellidrive/parameters", device.Username, device.Password, ct);
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<IntellidriveParametersResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<string> SetParametersAsync(DeviceModel device, string parametersJson, CancellationToken ct = default)
    {
        var content = new StringContent(parametersJson, System.Text.Encoding.UTF8, "application/json");
        var res = await SendAuthedPostAsync(device.Ip, "/intellidrive/parameters/set", device.Username, device.Password, content, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public Task<string> GetMinParameterValuesAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedGetAsync(device.Ip, "/intellidrive/parameters/min-values", device.Username, device.Password, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();

    public Task<string> GetMaxParameterValuesAsync(DeviceModel device, CancellationToken ct = default)
        => SendAuthedGetAsync(device.Ip, "/intellidrive/parameters/max-values", device.Username, device.Password, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadAsStringAsync(ct)), ct)
            .Unwrap();
}
