using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ReisingerIntelliApp_V4.ViewModels;

/// <summary>
/// ViewModel for the Device Parameters Page.
/// Handles loading, displaying, and saving device parameters.
/// </summary>
public partial class DeviceParametersPageViewModel : ObservableObject
{
    private readonly IntellidriveApiService _apiService;
    private readonly IDeviceService _deviceService;
    
    // Standard parameter count for Intellidrive devices
    private const int MaxParameterCount = 98;
    
    // All parameters (unfiltered)
    private List<DeviceParameterDisplayModel> _allParameters = new();

    public DeviceParametersPageViewModel(IntellidriveApiService apiService, IDeviceService deviceService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>();
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private DeviceModel? _currentDevice;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private string _deviceIp = string.Empty;

    [ObservableProperty]
    private string _deviceId = string.Empty;

    [ObservableProperty]
    private string _deviceSsid = string.Empty;

    [ObservableProperty]
    private string _connectionType = string.Empty;

    [ObservableProperty]
    private bool _hasDeviceInfo;

    [ObservableProperty]
    private ObservableCollection<DeviceParameterDisplayModel> _parameters = new();

    [ObservableProperty]
    private int _parameterCount;
    
    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private string _lastRefreshTime = string.Empty;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _isSearchActive;

    /// <summary>
    /// Called when search text changes - filters the parameter list
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Applies the current search filter to the parameter list
    /// </summary>
    private void ApplyFilter()
    {
        var searchTerm = SearchText?.Trim() ?? string.Empty;
        IsSearchActive = !string.IsNullOrEmpty(searchTerm);
        
        if (string.IsNullOrEmpty(searchTerm))
        {
            // No filter - show all parameters
            Parameters.Clear();
            foreach (var param in _allParameters)
            {
                Parameters.Add(param);
            }
            FilteredCount = _allParameters.Count;
            Debug.WriteLine($"?? Filter cleared - showing all {FilteredCount} parameters");
        }
        else
        {
            // Filter by ID or Name
            var filtered = _allParameters.Where(p =>
                p.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            Parameters.Clear();
            foreach (var param in filtered)
            {
                Parameters.Add(param);
            }
            FilteredCount = filtered.Count;
            Debug.WriteLine($"?? Filter '{searchTerm}' - showing {FilteredCount}/{_allParameters.Count} parameters");
        }
    }

    /// <summary>
    /// Clears the search filter
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    /// <summary>
    /// Initializes the parameter list with placeholders for immediate display.
    /// With virtualization, we only need a few visible items initially.
    /// </summary>
    public void InitializeParameterPlaceholders()
    {
        // With proper CollectionView virtualization, we don't need all 98 placeholders upfront
        // The UI only renders visible items (~10-15 on screen)
        // We'll add placeholders on-demand or skip entirely and show empty state until API responds
        
        Debug.WriteLine($"?? Parameter list initialized (virtualized - no upfront placeholders needed)");
        _allParameters.Clear();
        Parameters.Clear();
        ParameterCount = 0;
        FilteredCount = 0;
        SearchText = string.Empty;
    }

    /// <summary>
    /// Loads device parameters from the device.
    /// Uses authenticated API call if credentials are available.
    /// Updates existing placeholder items instead of recreating the list.
    /// </summary>
    public async Task LoadParametersAsync()
    {
        if (string.IsNullOrEmpty(DeviceIp) || DeviceIp == "N/A")
        {
            StatusMessage = "Keine gültige IP-Adresse vorhanden";
            HasError = true;
            Debug.WriteLine("? Cannot load parameters - no valid IP address");
            return;
        }

        if (CurrentDevice == null)
        {
            StatusMessage = "Kein Gerät ausgewählt";
            HasError = true;
            Debug.WriteLine("? Cannot load parameters - no device set");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            StatusMessage = "Lade Werte vom Gerät...";
            
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"?? Loading parameter values from {DeviceIp}");
            Debug.WriteLine($"   Auth: Username='{CurrentDevice.Username}', HasPassword={!string.IsNullOrEmpty(CurrentDevice.Password)}");

            IntellidriveParametersResponse? response;

            // Use authenticated call if credentials are available
            if (!string.IsNullOrEmpty(CurrentDevice.Username) && !string.IsNullOrEmpty(CurrentDevice.Password))
            {
                Debug.WriteLine("?? Using authenticated API call");
                response = await _apiService.GetParametersAsync(CurrentDevice);
            }
            else
            {
                Debug.WriteLine("?? Using unauthenticated API call (no credentials)");
                response = await _apiService.GetParametersByIpAsync(DeviceIp);
            }

            stopwatch.Stop();
            Debug.WriteLine($"?? API call completed in {stopwatch.ElapsedMilliseconds}ms");

            if (response?.Success == true && response.Values != null)
            {
                // Store all parameters
                _allParameters.Clear();
                foreach (var apiParam in response.Values.OrderBy(p => p.Id))
                {
                    _allParameters.Add(DeviceParameterDisplayModel.FromApiValue(apiParam));
                }

                ParameterCount = _allParameters.Count;
                LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
                StatusMessage = string.Empty;
                
                // Apply current filter (or show all if no filter)
                ApplyFilter();
                
                Debug.WriteLine($"? Loaded {ParameterCount} parameter values at {LastRefreshTime}");
            }
            else
            {
                StatusMessage = response?.Message ?? "Fehler beim Laden der Parameter";
                HasError = true;
                Debug.WriteLine($"? Failed to load parameters - response was null or not successful: {response?.Message}");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            StatusMessage = "Authentifizierung fehlgeschlagen - bitte Zugangsdaten prüfen";
            HasError = true;
            Debug.WriteLine($"? 401 Unauthorized - credentials may be incorrect");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
            HasError = true;
            Debug.WriteLine($"? Error loading parameters: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sets the device to configure and initializes placeholders.
    /// </summary>
    public void SetDevice(DeviceModel device)
    {
        CurrentDevice = device;
        DeviceName = device?.Name ?? "Unbekanntes Gerät";
        DeviceIp = device?.Ip ?? device?.IpAddress ?? "N/A";
        DeviceId = device?.DeviceId ?? "N/A";
        DeviceSsid = device?.Ssid ?? string.Empty;
        ConnectionType = !string.IsNullOrEmpty(device?.Ssid) ? "WiFi" : "Lokal";
        HasDeviceInfo = device != null;
        
        Debug.WriteLine($"?? DeviceParametersPageViewModel.SetDevice:");
        Debug.WriteLine($"   Name: {DeviceName}");
        Debug.WriteLine($"   IP: {DeviceIp}");
        Debug.WriteLine($"   ID: {DeviceId}");
        Debug.WriteLine($"   SSID: {DeviceSsid}");
        Debug.WriteLine($"   Type: {ConnectionType}");
        Debug.WriteLine($"   Username: {device?.Username ?? "N/A"}");
        Debug.WriteLine($"   Has Password: {!string.IsNullOrEmpty(device?.Password)}");
        
        // Initialize placeholders immediately so UI can render
        InitializeParameterPlaceholders();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Debug.WriteLine("?? DeviceParametersPageViewModel.RefreshAsync triggered");
        await LoadParametersAsync();
    }

    [RelayCommand]
    private async Task SaveParametersAsync()
    {
        // TODO: Implement parameter saving
        try
        {
            IsLoading = true;
            StatusMessage = "Speichern wird noch implementiert...";
            Debug.WriteLine("?? DeviceParametersPageViewModel.SaveParametersAsync - NOT YET IMPLEMENTED");

            await Task.Delay(1000);

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Speichern: {ex.Message}";
            HasError = true;
            Debug.WriteLine($"? Error saving parameters: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
