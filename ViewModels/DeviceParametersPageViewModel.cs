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
    
    // All parameters (unfiltered) - contains ALL 98 parameters for API communication
    private List<DeviceParameterDisplayModel> _allParameters = new();

    public DeviceParametersPageViewModel(IntellidriveApiService apiService, IDeviceService deviceService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>();
        ParameterGroups = new ObservableCollection<ParameterGroupDisplayModel>();
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

    /// <summary>
    /// Flat list of visible parameters (for search/filter).
    /// Only contains parameters with a Category != None.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceParameterDisplayModel> _parameters = new();

    /// <summary>
    /// Grouped parameters by category for display in the UI.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ParameterGroupDisplayModel> _parameterGroups = new();

    /// <summary>
    /// Currently selected category for filtering. Null = show all.
    /// </summary>
    [ObservableProperty]
    private ParameterCategory? _selectedCategory;

    [ObservableProperty]
    private int _parameterCount;
    
    [ObservableProperty]
    private int _filteredCount;

    /// <summary>
    /// Number of visible parameters (with category).
    /// </summary>
    [ObservableProperty]
    private int _visibleParameterCount;

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
    /// Available categories for the category picker.
    /// </summary>
    public List<ParameterCategory> AvailableCategories { get; } = new()
    {
        ParameterCategory.Zeiten,
        ParameterCategory.Weiten,
        ParameterCategory.Tempo,
        ParameterCategory.IO,
        ParameterCategory.Basis
    };

    /// <summary>
    /// Called when search text changes - filters the parameter list
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Called when selected category changes - filters the parameter list
    /// </summary>
    partial void OnSelectedCategoryChanged(ParameterCategory? value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Applies the current search filter and category filter to the parameter list.
    /// Only shows parameters with Category != None.
    /// </summary>
    private void ApplyFilter()
    {
        var searchTerm = SearchText?.Trim() ?? string.Empty;
        IsSearchActive = !string.IsNullOrEmpty(searchTerm);
        
        // Start with only visible parameters (those with a category)
        var visibleParams = _allParameters.Where(p => p.Meta.Category != ParameterCategory.None);
        
        // Apply category filter
        if (SelectedCategory.HasValue)
        {
            visibleParams = visibleParams.Where(p => p.Meta.Category == SelectedCategory.Value);
        }
        
        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            visibleParams = visibleParams.Where(p =>
                p.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
            Debug.WriteLine($"🔍 Filter '{searchTerm}' applied");
        }
        
        // Update flat list
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>(visibleParams.OrderBy(p => p.Id));
        FilteredCount = Parameters.Count;
        
        // Update grouped view
        UpdateGroupedParameters();
        
        Debug.WriteLine($"📋 Showing {FilteredCount} of {VisibleParameterCount} visible parameters");
    }

    /// <summary>
    /// Updates the grouped parameter collections for category-based display.
    /// </summary>
    private void UpdateGroupedParameters()
    {
        var groups = new List<ParameterGroupDisplayModel>();
        
        foreach (var category in AvailableCategories)
        {
            // Get parameters for this category from the current filtered list
            var paramsInCategory = Parameters.Where(p => p.Meta.Category == category).ToList();
            
            if (paramsInCategory.Count > 0)
            {
                // Use the new constructor that takes category and items
                groups.Add(new ParameterGroupDisplayModel(category, paramsInCategory));
            }
        }
        
        ParameterGroups = new ObservableCollection<ParameterGroupDisplayModel>(groups);
    }

    /// <summary>
    /// Updates validation and modified counts.
    /// Only counts visible parameters for display, but validates all editable params.
    /// </summary>
    public void UpdateValidationState()
    {
        // Count only visible parameters for UI display
        var visibleParams = _allParameters.Where(p => p.Meta.Category != ParameterCategory.None);
        InvalidCount = visibleParams.Count(p => p.HasValidationError);
        ModifiedCount = visibleParams.Count(p => p.IsModified);
        HasValidationErrors = InvalidCount > 0;
        HasModifiedParameters = ModifiedCount > 0;
        
        Debug.WriteLine($"📊 Validation state: {ModifiedCount} modified, {InvalidCount} invalid (visible only)");
    }

    /// <summary>
    /// Clears the search filter
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
    }

    /// <summary>
    /// Selects a category for filtering.
    /// </summary>
    [RelayCommand]
    private void SelectCategory(ParameterCategory? category)
    {
        SelectedCategory = category;
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
        
        Debug.WriteLine($"📱 DeviceParametersPageViewModel.SetDevice:");
        Debug.WriteLine($"   Name: {DeviceName}");
        Debug.WriteLine($"   IP: {DeviceIp}");
        Debug.WriteLine($"   ID: {DeviceId}");
        Debug.WriteLine($"   SSID: {DeviceSsid}");
        Debug.WriteLine($"   Type: {ConnectionType}");
        Debug.WriteLine($"   Username: {device?.Username ?? "N/A"}");
        Debug.WriteLine($"   Has Password: {!string.IsNullOrEmpty(device?.Password)}");
    }

    /// <summary>
    /// Initialize placeholders and start loading values. Called from OnAppearing.
    /// Creates ALL 98 parameters internally but only shows visible ones in UI.
    /// </summary>
    public async Task InitializeAndLoadAsync()
    {
        // Only initialize once
        if (_allParameters.Count > 0)
        {
            Debug.WriteLine("📋 Parameters already initialized, skipping");
            return;
        }
        
        Debug.WriteLine($"📋 Initializing all 98 parameter placeholders from catalog...");
        
        // Create placeholders for ALL 98 parameters (needed for API communication)
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
        
        // Count visible parameters (with category)
        VisibleParameterCount = _allParameters.Count(p => p.Meta.Category != ParameterCategory.None);
        ParameterCount = _allParameters.Count; // Total count (98)
        
        // Reset state
        SearchText = string.Empty;
        SelectedCategory = null;
        ModifiedCount = 0;
        InvalidCount = 0;
        HasValidationErrors = false;
        HasModifiedParameters = false;
        
        // Apply initial filter (shows all visible parameters)
        ApplyFilter();
        
        Debug.WriteLine($"✅ {ParameterCount} total parameters, {VisibleParameterCount} visible in UI");
        
        // Now load actual values from API
        await LoadAllParameterDataAsync();
    }

    /// <summary>
    /// Loads parameter values, min-values, and max-values sequentially with delays.
    /// A 3-second delay between each API call prevents overloading the target chip.
    /// </summary>
    private async Task LoadAllParameterDataAsync()
    {
        if (string.IsNullOrEmpty(DeviceIp) || DeviceIp == "N/A" || CurrentDevice == null)
        {
            StatusMessage = "Keine gültige IP-Adresse vorhanden";
            HasError = true;
            Debug.WriteLine("❌ Cannot load parameters - no valid device");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"🔄 Loading parameter data from {DeviceIp} (sequential API calls with 3s delay)");
            
            var hasAuth = !string.IsNullOrEmpty(CurrentDevice.Username) && !string.IsNullOrEmpty(CurrentDevice.Password);
            Debug.WriteLine($"   Auth: {(hasAuth ? "Using credentials" : "No credentials")}");

            const int DelayBetweenCallsMs = 3000;

            // === Step 1: Load min-values ===
            StatusMessage = "Lade Minimalwerte...";
            Debug.WriteLine("   📡 Step 1/3: Fetching min-values...");
            
            IntellidriveMinValuesResponse? minValuesResponse;
            if (hasAuth)
            {
                minValuesResponse = await _apiService.GetMinParameterValuesAsync(CurrentDevice);
            }
            else
            {
                minValuesResponse = await _apiService.GetMinParameterValuesByIpAsync(DeviceIp);
            }
            Debug.WriteLine($"   ✅ Min-values received: {minValuesResponse?.Values?.Count ?? 0} values");

            // Wait 3 seconds before next call
            await Task.Delay(DelayBetweenCallsMs);

            // === Step 2: Load max-values ===
            StatusMessage = "Lade Maximalwerte...";
            Debug.WriteLine("   📡 Step 2/3: Fetching max-values...");
            
            IntellidriveMaxValuesResponse? maxValuesResponse;
            if (hasAuth)
            {
                maxValuesResponse = await _apiService.GetMaxParameterValuesAsync(CurrentDevice);
            }
            else
            {
                maxValuesResponse = await _apiService.GetMaxParameterValuesByIpAsync(DeviceIp);
            }
            Debug.WriteLine($"   ✅ Max-values received: {maxValuesResponse?.Values?.Count ?? 0} values");

            // Wait 3 seconds before next call
            await Task.Delay(DelayBetweenCallsMs);

            // === Step 3: Load parameter values ===
            StatusMessage = "Lade Parameterwerte...";
            Debug.WriteLine("   📡 Step 3/3: Fetching parameter values...");
            
            IntellidriveParametersResponse? parametersResponse;
            if (hasAuth)
            {
                parametersResponse = await _apiService.GetParametersAsync(CurrentDevice);
            }
            else
            {
                parametersResponse = await _apiService.GetParametersByIpAsync(DeviceIp);
            }
            Debug.WriteLine($"   ✅ Parameters received: {parametersResponse?.Values?.Count ?? 0} values");

            stopwatch.Stop();
            Debug.WriteLine($"⏱️ All API calls completed in {stopwatch.ElapsedMilliseconds}ms (sequential with delays)");

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
                
                Debug.WriteLine($"✅ Updated {parametersResponse.Values.Count} parameter values at {LastRefreshTime}");
            }
            else
            {
                StatusMessage = parametersResponse?.Message ?? "Fehler beim Laden der Parameter";
                HasError = true;
                Debug.WriteLine($"❌ Failed to load parameters: {parametersResponse?.Message}");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            StatusMessage = "Authentifizierung fehlgeschlagen - bitte Zugangsdaten prüfen";
            HasError = true;
            Debug.WriteLine($"❌ 401 Unauthorized - credentials may be incorrect");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
            HasError = true;
            Debug.WriteLine($"❌ Error loading parameters: {ex.Message}");
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
            // Validate all editable parameters before saving
            foreach (var param in _allParameters.Where(p => p.IsEditable))
            {
                param.Validate();
            }
            UpdateValidationState();

            if (HasValidationErrors)
            {
                StatusMessage = $"{InvalidCount} Parameter mit ungültigen Werten - Speichern nicht möglich";
                HasError = true;
                Debug.WriteLine($"❌ Save blocked: {InvalidCount} validation errors");
                return;
            }

            if (!HasModifiedParameters)
            {
                StatusMessage = "Keine Änderungen zum Speichern";
                Debug.WriteLine("ℹ️ No changes to save");
                await Task.Delay(1500);
                StatusMessage = string.Empty;
                return;
            }

            // Verify we have a valid device
            if (CurrentDevice == null || string.IsNullOrEmpty(DeviceIp) || DeviceIp == "N/A")
            {
                StatusMessage = "Kein Gerät verbunden";
                HasError = true;
                Debug.WriteLine("❌ No device connected");
                return;
            }

            IsLoading = true;
            HasError = false;
            StatusMessage = $"Speichere {ModifiedCount} geänderte Parameter...";
            Debug.WriteLine($"💾 Saving {ModifiedCount} modified parameters to {DeviceIp}");

            // Get all modified editable parameters (for tracking)
            var modifiedParams = _allParameters
                .Where(p => p.IsModified && p.IsEditable)
                .ToList();

            // Build the request with ALL 98 parameters (firmware expects all parameters ID 1-98)
            // Reserved parameters are sent with their current value (usually 0)
            var values = new List<IntellidriveParameterSetValue>();
            
            for (int id = 1; id <= MaxParameterCount; id++)
            {
                var param = _allParameters.FirstOrDefault(p => p.Id == id);
                uint numericValue = 0;
                
                if (param != null)
                {
                    // Handle date/time format parameters - convert "tt:mm:jj" or "hh:mm:ss" back to numeric
                    if (param.Meta.FormatType != ParameterFormatType.None)
                    {
                        numericValue = ConvertFormattedToNumeric(param.Value, param.Meta.FormatType);
                        Debug.WriteLine($"📅 Parameter {id} ({param.Name}): '{param.Value}' → {numericValue}");
                    }
                    else if (uint.TryParse(param.Value, out var parsed))
                    {
                        numericValue = parsed;
                    }
                    else
                    {
                        // For reserved/read-only parameters with empty or invalid values, use 0
                        numericValue = 0;
                        if (!param.Meta.IsReserved && !param.Meta.IsReadOnly)
                        {
                            Debug.WriteLine($"⚠️ Parameter {id} ({param.Name}): Cannot parse '{param.Value}' - using 0");
                        }
                    }
                }
                else
                {
                    // Parameter not in list (should not happen), use 0
                    Debug.WriteLine($"⚠️ Parameter {id}: Not found in list - using 0");
                }
                
                values.Add(new IntellidriveParameterSetValue
                {
                    Id = id,
                    V = numericValue
                });
            }

            var request = new IntellidriveSetParametersRequest
            {
                Success = true,
                Message = string.Empty,
                Values = values,
                Units = new List<object>()
            };

            Debug.WriteLine($"📤 Sending ALL {request.Values.Count} parameter values to device (ID 1-{MaxParameterCount})");

            // Call the API - use auth or direct IP based on device config
            IntellidriveSetParametersResponse? response;
            var hasAuth = !string.IsNullOrEmpty(CurrentDevice.Username) && 
                          !string.IsNullOrEmpty(CurrentDevice.Password);

            if (hasAuth)
            {
                Debug.WriteLine("   Using authenticated request");
                response = await _apiService.SetParametersAsync(CurrentDevice, request);
            }
            else
            {
                Debug.WriteLine("   Using direct IP request (no auth)");
                response = await _apiService.SetParametersByIpAsync(DeviceIp, request);
            }

            // Handle response
            if (response?.Success == true)
            {
                // Update original values to mark as saved
                foreach (var param in modifiedParams)
                {
                    param.OriginalValue = param.Value;
                }
                UpdateValidationState();

                StatusMessage = $"✅ {modifiedParams.Count} Parameter erfolgreich gespeichert";
                Debug.WriteLine($"✅ Successfully saved {modifiedParams.Count} parameters");
                
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            else
            {
                var errorMsg = response?.Message ?? "Unbekannter Fehler";
                StatusMessage = $"Fehler: {errorMsg}";
                HasError = true;
                Debug.WriteLine($"❌ Failed to save parameters: {errorMsg}");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            StatusMessage = "Authentifizierung fehlgeschlagen - bitte Zugangsdaten prüfen";
            HasError = true;
            Debug.WriteLine("❌ 401 Unauthorized when saving parameters");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            StatusMessage = "Tür ist geöffnet - Konfiguration nur bei geschlossener Tür möglich";
            HasError = true;
            Debug.WriteLine("❌ 403 Forbidden - Door is open, cannot save parameters");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Speichern: {ex.Message}";
            HasError = true;
            Debug.WriteLine($"❌ Error saving parameters: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Converts a formatted date/time string back to numeric value for API.
    /// Examples: 
    ///   "14:01:25" (hh:mm:ss) → 140125
    ///   "31:12:24" (tt:mm:jj) → 311224
    ///   "08.30.00" (hh.mm.ss) → 83000
    /// </summary>
    private static uint ConvertFormattedToNumeric(string formattedValue, ParameterFormatType formatType)
    {
        if (string.IsNullOrEmpty(formattedValue))
            return 0;

        // Remove separators (: or .) based on format type
        var separator = formatType == ParameterFormatType.TimeWithDot ? "." : ":";
        var numericString = formattedValue.Replace(separator, "");
        
        if (uint.TryParse(numericString, out var result))
        {
            Debug.WriteLine($"   Converted '{formattedValue}' → {result}");
            return result;
        }
        
        Debug.WriteLine($"⚠️ Could not convert '{formattedValue}' to numeric");
        return 0;
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
