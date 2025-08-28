using ReisingerIntelliApp_V4.Models;
using System.Text;

namespace ReisingerIntelliApp_V4.Services;

public interface IAuthenticationService
{
    Task<bool> TestUserAuthAsync(string ipAddress, string username, string password);
    Task<string> GetRequestNoAuthForWifiAsync(string ipAddress);
    Task<bool> ConnectToWifiNetworkAsync(string ssid, string password);
    Task<bool> IsNetworkReachableAsync(string ipAddress);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly int _defaultTimeoutSeconds = 10; // Default timeout (matching V3)

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(_defaultTimeoutSeconds);
    }

    public async Task<bool> TestUserAuthAsync(string ipAddress, string username, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Testing authentication for {username} on {ipAddress}");
            
            // Use the intellidrive/beep endpoint for authentication test
            var endpoint = $"http://{ipAddress}/intellidrive/beep";
            
            // Build per-request message with auth header to avoid side effects on the shared client
            var authValue = $"{username}:{password}"; // Device expects scheme 'User' with value 'username:password'
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("User", authValue);
            request.Headers.Accept.Clear();
            request.Headers.Accept.ParseAdd("application/json");

            System.Diagnostics.Debug.WriteLine($"🔄 Sending GET request to: {endpoint}");
            System.Diagnostics.Debug.WriteLine($"🔄 Authorization: User {username}:******");

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            System.Diagnostics.Debug.WriteLine($"📶 Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📄 Response content: {responseContent}");
                
                // Check if the response contains the expected JSON structure
                // Expected: {"DeviceId":"9039fb45-4f49-48c8-aac6-3c179876cb7d","Success":true,"Message":"Beeped","LatestFirmware":true,"FirmwareVersion":"","Content":{"DEVICE_ID":""}}
                if (!string.IsNullOrEmpty(responseContent) && 
                    responseContent.Contains("\"Success\":true") && 
                    responseContent.Contains("\"DeviceId\"") &&
                    responseContent.Contains("\"Message\":\"Beeped\""))
                {
                    System.Diagnostics.Debug.WriteLine("✅ Authentication successful - Valid JSON response with Success=true and Beeped message received");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Authentication failed - Invalid JSON response: {responseContent}");
                    return false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Authentication failed with status: {response.StatusCode}");
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Authentication test timed out for {ipAddress}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Authentication test failed for {ipAddress}: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetRequestNoAuthForWifiAsync(string ipAddress)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Testing basic connectivity to {ipAddress}");
            
            // Use the same endpoint pattern as V3 for WiFi AP
            var endpoint = $"http://{ipAddress}/intellidrive/version";
            
            System.Diagnostics.Debug.WriteLine($"🔄 Sending GET request (no auth) to: {endpoint}");
            
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Device reachable: {content}");
                return content;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Device not reachable, status: {response.StatusCode}");
                return string.Empty;
            }
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"❌ No-auth request timed out for {ipAddress}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ No-auth request failed for {ipAddress}: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<bool> ConnectToWifiNetworkAsync(string ssid, string password)
    {
        try
        {
            // Platform-specific WiFi connection logic would go here
            // For now, simulate connection
            await Task.Delay(3000);
            
            System.Diagnostics.Debug.WriteLine($"✅ Connected to WiFi network: {ssid}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to connect to WiFi {ssid}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsNetworkReachableAsync(string ipAddress)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Checking network reachability for {ipAddress}");
            
            // Use a simple ping-like endpoint check with very short timeout for responsive UI
            var endpoint = $"http://{ipAddress}/intellidrive/version";
            
            // Set a very short timeout for this quick check (1.5 seconds for faster response)
            var originalTimeout = _httpClient.Timeout;
            _httpClient.Timeout = TimeSpan.FromSeconds(1.5);
            
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                var isReachable = response.IsSuccessStatusCode;
                System.Diagnostics.Debug.WriteLine($"📶 Network {ipAddress} reachable: {isReachable} (response: {response.StatusCode})");
                return isReachable;
            }
            finally
            {
                // Always restore original timeout
                _httpClient.Timeout = originalTimeout;
            }
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Network reachability check timed out for {ipAddress} (1.5s)");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Network reachability check failed for {ipAddress}: {ex.Message}");
            return false;
        }
    }
}
