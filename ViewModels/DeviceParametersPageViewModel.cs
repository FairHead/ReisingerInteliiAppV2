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
    /// Initializes the parameter list with all 98 parameters from catalog (no values yet).
    /// This allows immediate rendering while values load asynchronously.
    /// </summary>
    public void InitializeParameterPlaceholders()
    {
        System.Diagnostics.Debug.WriteLine($"?? Initializing all 98 parameter placeholders from catalog...");
        
        _allParameters.Clear();
        
        // Create all 98 parameters immediately from catalog metadata
        for (int id = 1; id <= MaxParameterCount; id++)
        {
            var placeholder = DeviceParameterDisplayModel.CreatePlaceholder(id);
            _allParameters.Add(placeholder);
        }
        
        ParameterCount = _allParameters.Count;
        FilteredCount = _allParameters.Count;
        SearchText = string.Empty;
        ModifiedCount = 0;
        InvalidCount = 0;
        HasValidationErrors = false;
        HasModifiedParameters = false;
        
        // Replace the entire collection at once instead of adding one by one
        // This prevents 98 individual UI updates
        Parameters = new ObservableCollection<DeviceParameterDisplayModel>(_allParameters);
        
        System.Diagnostics.Debug.WriteLine($"? {ParameterCount} parameter placeholders ready for display");
    }

    /// <summary>
    /// Loads device parameters from the device.
    /// Updates existing placeholder items with actual values.
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
                // Update existing placeholders with actual values
                foreach (var apiParam in response.Values)
                {
                    var existingParam = _allParameters.FirstOrDefault(p => p.Id == apiParam.Id);
                    if (existingParam != null)
                    {
                        existingParam.SetValueFromApi(apiParam);
                        
                        // Subscribe to property changes for validation updates (if not already)
                        existingParam.PropertyChanged -= OnParameterPropertyChanged;
                        existingParam.PropertyChanged += OnParameterPropertyChanged;
                    }
                }

                LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
                StatusMessage = string.Empty;
                
                UpdateValidationState();
                
                Debug.WriteLine($"? Updated {response.Values.Count} parameter values at {LastRefreshTime}");
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
    
    private void OnParameterPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceParameterDisplayModel.Value) ||
            e.PropertyName == nameof(DeviceParameterDisplayModel.HasValidationError))
        {
            UpdateValidationState();
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
        try
        {
            // Validate all parameters first
            foreach (var param in _allParameters)
            {
                param.Validate();
            }
            UpdateValidationState();

            // Block save if there are validation errors
            if (HasValidationErrors)
            {
                StatusMessage = $"{InvalidCount} Parameter mit ungültigen Werten - Speichern nicht möglich";
                HasError = true;
                Debug.WriteLine($"? Save blocked: {InvalidCount} validation errors");
                return;
            }

            // Check if there are any changes
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

            // Get only modified parameters
            var modifiedParams = _allParameters
                .Where(p => p.IsModified && p.IsEditable)
                .ToList();

            // TODO: Implement actual API call to save parameters
            // For now, simulate save
            await Task.Delay(1000);

            // Mark all as saved (update original values)
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
