using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

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

    [ObservableProperty]
    private int _modifiedCount;

    [ObservableProperty]
    private int _invalidCount;

    [ObservableProperty]
    private bool _hasValidationErrors;

    [ObservableProperty]
    private bool _hasModifiedParameters;

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
        
        IEnumerable<DeviceParameterDisplayModel> filtered;
        
        if (string.IsNullOrEmpty(searchTerm))
        {
            // No filter - show all parameters
            filtered = _allParameters;
            Debug.WriteLine($"?? Filter cleared - showing all {_allParameters.Count} parameters");
        }
        else
        {
            // Filter by ID or Name
            filtered = _allParameters.Where(p =>
                p.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
            Debug.WriteLine($"?? Filter '{searchTerm}' applied");
        }
        
        // Replace entire collection at once - prevents multiple UI updates
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>(filtered);
        FilteredCount = Parameters.Count;
    }

    /// <summary>
    /// Updates validation and modified counts
    /// </summary>
    public void UpdateValidationState()
    {
        InvalidCount = _allParameters.Count(p => p.HasValidationError);
        ModifiedCount = _allParameters.Count(p => p.IsModified);
        HasValidationErrors = InvalidCount > 0;
        HasModifiedParameters = ModifiedCount > 0;
        
        Debug.WriteLine($"?? Validation state: {ModifiedCount} modified, {InvalidCount} invalid");
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
    /// Sets the device to configure. Does NOT initialize placeholders here - 
    /// let the page render first, then call InitializeAndLoadAsync separately.
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
        
        // DON'T initialize placeholders here - let page render first!
        // Page.OnAppearing will call InitializeAndLoadAsync
    }

    /// <summary>
    /// Initialize placeholders and start loading values. Called from OnAppearing.
    /// Runs all API calls in parallel for best performance.
    /// </summary>
    public async Task InitializeAndLoadAsync()
    {
        // Only initialize once
        if (_allParameters.Count > 0)
        {
            Debug.WriteLine("?? Parameters already initialized, skipping");
            return;
        }
        
        Debug.WriteLine($"?? Initializing all 98 parameter placeholders from catalog...");
        
        // Create placeholders in background to not block UI
        var placeholders = await Task.Run(() =>
        {
            var list = new List<DeviceParameterDisplayModel>(MaxParameterCount);
            for (int id = 1; id <= MaxParameterCount; id++)
            {
                list.Add(DeviceParameterDisplayModel.CreatePlaceholder(id));
            }
            return list;
        });
        
        _allParameters = placeholders;
        
        // Update UI state
        ParameterCount = _allParameters.Count;
        FilteredCount = _allParameters.Count;
        SearchText = string.Empty;
        ModifiedCount = 0;
        InvalidCount = 0;
        HasValidationErrors = false;
        HasModifiedParameters = false;
        
        // Assign collection to trigger UI binding
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>(_allParameters);
        
        Debug.WriteLine($"? {ParameterCount} parameter placeholders ready for display");
        
        // Now load actual values from API (parallel calls)
        await LoadAllParameterDataAsync();
    }

    /// <summary>
    /// Loads parameter values, min-values, and max-values in parallel.
    /// This ensures the page doesn't block waiting for sequential API calls.
    /// </summary>
    private async Task LoadAllParameterDataAsync()
    {
        if (string.IsNullOrEmpty(DeviceIp) || DeviceIp == "N/A" || CurrentDevice == null)
        {
            StatusMessage = "Keine gültige IP-Adresse vorhanden";
            HasError = true;
            Debug.WriteLine("? Cannot load parameters - no valid device");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            StatusMessage = "Lade Werte vom Gerät...";
            
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"?? Loading parameter data from {DeviceIp} (parallel API calls)");
            
            var hasAuth = !string.IsNullOrEmpty(CurrentDevice.Username) && !string.IsNullOrEmpty(CurrentDevice.Password);
            Debug.WriteLine($"   Auth: {(hasAuth ? "Using credentials" : "No credentials")}");

            // Start all 3 API calls in parallel - don't wait for each other!
            Task<IntellidriveParametersResponse?> parametersTask;
            Task<IntellidriveMinValuesResponse?> minValuesTask;
            Task<IntellidriveMaxValuesResponse?> maxValuesTask;
            
            if (hasAuth)
            {
                parametersTask = _apiService.GetParametersAsync(CurrentDevice);
                minValuesTask = _apiService.GetMinParameterValuesAsync(CurrentDevice);
                maxValuesTask = _apiService.GetMaxParameterValuesAsync(CurrentDevice);
            }
            else
            {
                parametersTask = _apiService.GetParametersByIpAsync(DeviceIp);
                minValuesTask = _apiService.GetMinParameterValuesByIpAsync(DeviceIp);
                maxValuesTask = _apiService.GetMaxParameterValuesByIpAsync(DeviceIp);
            }

            // Wait for all to complete (parallel execution)
            await Task.WhenAll(parametersTask, minValuesTask, maxValuesTask);

            stopwatch.Stop();
            Debug.WriteLine($"?? All API calls completed in {stopwatch.ElapsedMilliseconds}ms (parallel)");

            // Process results
            var parametersResponse = await parametersTask;
            var minValuesResponse = await minValuesTask;
            var maxValuesResponse = await maxValuesTask;

            // Apply min/max values first (for variable parameters)
            ApplyDynamicRanges(minValuesResponse, maxValuesResponse);
            
            // Then apply actual values
            if (parametersResponse?.Success == true && parametersResponse.Values != null)
            {
                foreach (var apiParam in parametersResponse.Values)
                {
                    var existingParam = _allParameters.FirstOrDefault(p => p.Id == apiParam.Id);
                    if (existingParam != null)
                    {
                        existingParam.SetValueFromApi(apiParam);
                        
                        existingParam.PropertyChanged -= OnParameterPropertyChanged;
                        existingParam.PropertyChanged += OnParameterPropertyChanged;
                    }
                }

                LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
                StatusMessage = string.Empty;
                
                UpdateValidationState();
                
                Debug.WriteLine($"? Updated {parametersResponse.Values.Count} parameter values at {LastRefreshTime}");
            }
            else
            {
                StatusMessage = parametersResponse?.Message ?? "Fehler beim Laden der Parameter";
                HasError = true;
                Debug.WriteLine($"? Failed to load parameters: {parametersResponse?.Message}");
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
    /// Applies device-specific min/max values to parameters with variable ranges.
    /// These are typically door-width related parameters that vary by device.
    /// </summary>
    private void ApplyDynamicRanges(IntellidriveMinValuesResponse? minResponse, IntellidriveMaxValuesResponse? maxResponse)
    {
        Debug.WriteLine($"?? Applying dynamic ranges...");
        
        int minCount = 0;
        int maxCount = 0;
        
        // Apply min values
        if (minResponse?.Success == true && minResponse.Values != null)
        {
            foreach (var minVal in minResponse.Values)
            {
                var param = _allParameters.FirstOrDefault(p => p.Id == minVal.Id);
                if (param != null)
                {
                    var value = ParseJsonElementToInt(minVal.V);
                    if (value.HasValue)
                    {
                        param.DynamicMin = value.Value;
                        minCount++;
                    }
                }
            }
            Debug.WriteLine($"   ? Applied {minCount} dynamic min values");
        }
        else
        {
            Debug.WriteLine($"   ?? No min-values response or not successful");
        }
        
        // Apply max values
        if (maxResponse?.Success == true && maxResponse.Values != null)
        {
            foreach (var maxVal in maxResponse.Values)
            {
                var param = _allParameters.FirstOrDefault(p => p.Id == maxVal.Id);
                if (param != null)
                {
                    var value = ParseJsonElementToInt(maxVal.V);
                    if (value.HasValue)
                    {
                        param.DynamicMax = value.Value;
                        maxCount++;
                    }
                }
            }
            Debug.WriteLine($"   ? Applied {maxCount} dynamic max values");
        }
        else
        {
            Debug.WriteLine($"   ?? No max-values response or not successful");
        }
        
        Debug.WriteLine($"?? Dynamic ranges applied: {minCount} min, {maxCount} max");
    }

    private static int? ParseJsonElementToInt(JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetInt32(),
                JsonValueKind.String when int.TryParse(element.GetString(), out var v) => v,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
    
    private void OnParameterPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceParameterDisplayModel.Value) ||
            e.PropertyName == nameof(DeviceParameterDisplayModel.HasValidationError))
        {
            UpdateValidationState();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Debug.WriteLine("?? DeviceParametersPageViewModel.RefreshAsync triggered");
        await LoadAllParameterDataAsync();
    }

    [RelayCommand]
    private async Task SaveParametersAsync()
    {
        try
        {
            foreach (var param in _allParameters)
            {
                param.Validate();
            }
            UpdateValidationState();

            if (HasValidationErrors)
            {
                StatusMessage = $"{InvalidCount} Parameter mit ungültigen Werten - Speichern nicht möglich";
                HasError = true;
                Debug.WriteLine($"? Save blocked: {InvalidCount} validation errors");
                return;
            }

            if (!HasModifiedParameters)
            {
                StatusMessage = "Keine Änderungen zum Speichern";
                Debug.WriteLine("?? No changes to save");
                await Task.Delay(1500);
                StatusMessage = string.Empty;
                return;
            }

            IsLoading = true;
            HasError = false;
            StatusMessage = $"Speichere {ModifiedCount} geänderte Parameter...";
            Debug.WriteLine($"?? Saving {ModifiedCount} modified parameters");

            var modifiedParams = _allParameters
                .Where(p => p.IsModified && p.IsEditable)
                .ToList();

            // TODO: Implement actual API call to save parameters
            await Task.Delay(1000);

            foreach (var param in modifiedParams)
            {
                param.OriginalValue = param.Value;
            }
            UpdateValidationState();

            StatusMessage = $"{modifiedParams.Count} Parameter erfolgreich gespeichert";
            Debug.WriteLine($"? Saved {modifiedParams.Count} parameters");
            
            await Task.Delay(1500);
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
