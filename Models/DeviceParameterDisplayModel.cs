using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReisingerIntelliApp_V4.Models;

/// <summary>
/// Display model for a device parameter with name, value, metadata, and validation.
/// Uses ParameterCatalog as single source of truth for names, ranges, and rules.
/// </summary>
public partial class DeviceParameterDisplayModel : ObservableObject
{
    private ParameterMeta? _meta;
    
    /// <summary>
    /// Parameter ID (1-98)
    /// </summary>
    [ObservableProperty]
    private int _id;

    /// <summary>
    /// Display name for the parameter (from ParameterCatalog)
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Current value as string (editable in UI)
    /// </summary>
    [ObservableProperty]
    private string _value = string.Empty;

    /// <summary>
    /// Original value from device (for change detection)
    /// </summary>
    [ObservableProperty]
    private string _originalValue = string.Empty;

    /// <summary>
    /// Whether the value is currently being loaded from the device
    /// </summary>
    [ObservableProperty]
    private bool _isValueLoading = true;

    /// <summary>
    /// Validation error message (empty if valid)
    /// </summary>
    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Whether this parameter has a validation error
    /// </summary>
    [ObservableProperty]
    private bool _hasValidationError;

    /// <summary>
    /// Parameter metadata from catalog
    /// </summary>
    public ParameterMeta Meta => _meta ??= ParameterCatalog.GetMeta(Id);

    /// <summary>
    /// Range text for display (e.g., "0..200", "50..variabel")
    /// </summary>
    public string RangeText => Meta.RangeText;

    /// <summary>
    /// Unit text for display (e.g., "s", "mm", "mm/s")
    /// </summary>
    public string UnitText => string.IsNullOrEmpty(Meta.Unit) ? "-" : Meta.Unit;

    /// <summary>
    /// Default value text for display
    /// </summary>
    public string DefaultText => Meta.DefaultText;

    /// <summary>
    /// Whether this parameter is editable
    /// </summary>
    public bool IsEditable => !Meta.IsReadOnly && !Meta.IsReserved;

    /// <summary>
    /// Input type for UI (Numeric, Toggle, Picker, Format, ReadOnly)
    /// </summary>
    public ParameterInputType InputType => Meta.InputType;

    /// <summary>
    /// Picker options for Picker input type
    /// </summary>
    public Dictionary<int, string>? PickerOptions => Meta.PickerOptions;

    /// <summary>
    /// Whether this is a Toggle (0/1) parameter
    /// </summary>
    public bool IsToggle => Meta.InputType == ParameterInputType.Toggle;

    /// <summary>
    /// Whether this is a Picker parameter
    /// </summary>
    public bool IsPicker => Meta.InputType == ParameterInputType.Picker;

    /// <summary>
    /// Whether this is a numeric input parameter
    /// </summary>
    public bool IsNumeric => Meta.InputType == ParameterInputType.Numeric;

    /// <summary>
    /// Whether this is a format (date/time) parameter
    /// </summary>
    public bool IsFormat => Meta.InputType == ParameterInputType.Format;

    /// <summary>
    /// Whether this is a read-only parameter
    /// </summary>
    public bool IsReadOnly => Meta.InputType == ParameterInputType.ReadOnly;

    /// <summary>
    /// Toggle state for Toggle parameters
    /// </summary>
    public bool ToggleValue
    {
        get => Value == "1";
        set
        {
            Value = value ? "1" : "0";
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Selected picker value as int
    /// </summary>
    public int PickerValue
    {
        get => int.TryParse(Value, out var v) ? v : 0;
        set
        {
            Value = value.ToString();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether this parameter has been modified
    /// </summary>
    public bool IsModified => Value != OriginalValue;

    /// <summary>
    /// Whether this parameter is valid (no validation errors)
    /// </summary>
    public bool IsValid => !HasValidationError;

    /// <summary>
    /// Called when Value changes - validates the new value
    /// </summary>
    partial void OnValueChanged(string value)
    {
        System.Diagnostics.Debug.WriteLine($"?? Parameter {Id} ({Name}): Value changed to '{value}' (Original: '{_originalValue}', IsLoading: {_isValueLoading})");
        
        // Skip validation during initialization (when IsValueLoading is still true or being set)
        if (!_isValueLoading)
        {
            Validate();
        }
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ToggleValue));
        OnPropertyChanged(nameof(PickerValue));
    }

    /// <summary>
    /// Called when Id changes - refresh metadata
    /// </summary>
    partial void OnIdChanged(int value)
    {
        _meta = null; // Force refresh
        OnPropertyChanged(nameof(Meta));
        OnPropertyChanged(nameof(RangeText));
        OnPropertyChanged(nameof(UnitText));
        OnPropertyChanged(nameof(DefaultText));
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(InputType));
        OnPropertyChanged(nameof(PickerOptions));
        OnPropertyChanged(nameof(IsToggle));
        OnPropertyChanged(nameof(IsPicker));
        OnPropertyChanged(nameof(IsNumeric));
        OnPropertyChanged(nameof(IsFormat));
        OnPropertyChanged(nameof(IsReadOnly));
    }

    /// <summary>
    /// Validates the current value against the parameter metadata rules
    /// </summary>
    public void Validate()
    {
        var previousError = ValidationError;
        var previousHasError = HasValidationError;
        
        ValidationError = string.Empty;
        HasValidationError = false;

        // Skip validation for loading or read-only
        if (IsValueLoading || Meta.IsReadOnly || Meta.IsReserved)
        {
            return;
        }

        // Empty value check
        if (string.IsNullOrWhiteSpace(Value))
        {
            ValidationError = "Wert erforderlich";
            HasValidationError = true;
            LogValidationError("Empty value");
            return;
        }

        // Format validation (date/time)
        if (Meta.FormatType != ParameterFormatType.None)
        {
            ValidateFormat();
            if (HasValidationError)
            {
                LogValidationError("Format invalid");
            }
            return;
        }

        // Numeric validation
        if (!int.TryParse(Value, out var numValue))
        {
            ValidationError = "Nur Ganzzahlen erlaubt";
            HasValidationError = true;
            LogValidationError($"Not a number: '{Value}'");
            return;
        }

        // Range validation
        if (Meta.Min.HasValue && numValue < Meta.Min.Value)
        {
            ValidationError = $"Min: {Meta.Min}";
            HasValidationError = true;
            LogValidationError($"Below min: {numValue} < {Meta.Min}");
            return;
        }

        if (Meta.Max.HasValue && numValue > Meta.Max.Value)
        {
            ValidationError = $"Max: {Meta.Max}";
            HasValidationError = true;
            LogValidationError($"Above max: {numValue} > {Meta.Max}");
            return;
        }

        // Variable max: only check min, value must be integer (already validated above)
        if (Meta.IsVariableMax && Meta.Min.HasValue && numValue < Meta.Min.Value)
        {
            ValidationError = $"Min: {Meta.Min}";
            HasValidationError = true;
            LogValidationError($"Below variable min: {numValue} < {Meta.Min}");
            return;
        }
        
        // Log if validation error was cleared
        if (previousHasError && !HasValidationError)
        {
            System.Diagnostics.Debug.WriteLine($"? Parameter {Id} ({Name}): Validation error CLEARED. Value='{Value}' is now valid.");
        }
    }
    
    private void LogValidationError(string reason)
    {
        System.Diagnostics.Debug.WriteLine($"? VALIDATION ERROR - Parameter {Id} ({Name}):");
        System.Diagnostics.Debug.WriteLine($"   Current Value: '{Value}'");
        System.Diagnostics.Debug.WriteLine($"   Original Value: '{_originalValue}'");
        System.Diagnostics.Debug.WriteLine($"   Range: {Meta.Min}..{Meta.Max}");
        System.Diagnostics.Debug.WriteLine($"   Reason: {reason}");
        System.Diagnostics.Debug.WriteLine($"   Error Message: {ValidationError}");
    }

    private void ValidateFormat()
    {
        var pattern = Meta.FormatType switch
        {
            ParameterFormatType.Date => @"^\d{2}:\d{2}:\d{2}$",      // tt:mm:jj
            ParameterFormatType.Time => @"^\d{2}:\d{2}:\d{2}$",      // hh:mm:ss
            ParameterFormatType.TimeWithDot => @"^\d{2}\.\d{2}\.\d{2}$", // hh.mm.ss
            _ => null
        };

        if (pattern != null && !Regex.IsMatch(Value, pattern))
        {
            ValidationError = Meta.FormatType switch
            {
                ParameterFormatType.Date => "Format: tt:mm:jj",
                ParameterFormatType.Time => "Format: hh:mm:ss",
                ParameterFormatType.TimeWithDot => "Format: hh.mm.ss",
                _ => "Ungültiges Format"
            };
            HasValidationError = true;
        }
    }

    /// <summary>
    /// Creates a display model from an API parameter value
    /// </summary>
    public static DeviceParameterDisplayModel FromApiValue(IntellidriveParameterValue apiValue)
    {
        string valueStr;
        
        // Debug: Log the raw JsonElement details
        System.Diagnostics.Debug.WriteLine($"?? Parameter {apiValue.Id}: ValueKind={apiValue.V.ValueKind}, RawText={apiValue.V.GetRawText()}");
        
        try
        {
            valueStr = apiValue.V.ValueKind switch
            {
                JsonValueKind.Number => apiValue.V.GetRawText(),
                JsonValueKind.String => apiValue.V.GetString() ?? string.Empty,
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Undefined => string.Empty,
                _ => apiValue.V.GetRawText()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error parsing Parameter {apiValue.Id}: {ex.Message}");
            valueStr = "0";
        }

        var meta = ParameterCatalog.GetMeta(apiValue.Id);
        
        System.Diagnostics.Debug.WriteLine($"? Parameter {apiValue.Id} ({meta.Name}): Value='{valueStr}'");
        
        // Create model with IsValueLoading=true first, set values, then set IsValueLoading=false
        // This prevents validation from triggering during initialization
        var model = new DeviceParameterDisplayModel
        {
            _isValueLoading = true, // Keep loading state during init
            Id = apiValue.Id,
            Name = meta.Name,
        };
        
        // Set values directly to backing fields to avoid triggering OnValueChanged
        model._value = valueStr;
        model._originalValue = valueStr;
        
        // Now mark as loaded - this won't trigger validation since value didn't "change"
        model._isValueLoading = false;
        
        // Notify UI about the values
        model.OnPropertyChanged(nameof(Value));
        model.OnPropertyChanged(nameof(OriginalValue));
        model.OnPropertyChanged(nameof(IsValueLoading));
        model.OnPropertyChanged(nameof(IsModified));
        model.OnPropertyChanged(nameof(ToggleValue));
        model.OnPropertyChanged(nameof(PickerValue));
        
        return model;
    }

    /// <summary>
    /// Creates a placeholder model for immediate display while loading.
    /// Shows all static metadata from catalog, value is empty until API responds.
    /// </summary>
    public static DeviceParameterDisplayModel CreatePlaceholder(int id)
    {
        var meta = ParameterCatalog.GetMeta(id);
        
        var model = new DeviceParameterDisplayModel();
        
        // Set loading state first
        model._isValueLoading = true;
        
        // Set static data from catalog
        model._id = id;
        model._name = meta.Name;
        model._value = string.Empty;  // Will be filled when API responds
        model._originalValue = string.Empty;
        
        // Notify properties
        model.OnPropertyChanged(nameof(Id));
        model.OnPropertyChanged(nameof(Name));
        model.OnPropertyChanged(nameof(Value));
        model.OnPropertyChanged(nameof(OriginalValue));
        model.OnPropertyChanged(nameof(IsValueLoading));
        
        return model;
    }

    /// <summary>
    /// Updates this model with the actual value from the API.
    /// Called when API response arrives to fill in the value.
    /// </summary>
    public void SetValueFromApi(IntellidriveParameterValue apiValue)
    {
        string valueStr;
        
        try
        {
            valueStr = apiValue.V.ValueKind switch
            {
                JsonValueKind.Number => apiValue.V.GetRawText(),
                JsonValueKind.String => apiValue.V.GetString() ?? string.Empty,
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Undefined => string.Empty,
                _ => apiValue.V.GetRawText()
            };
        }
        catch
        {
            valueStr = "0";
        }

        System.Diagnostics.Debug.WriteLine($"?? Setting value for Parameter {Id} ({Name}): '{valueStr}'");
        
        // Set values directly to backing fields to avoid triggering validation during load
        _value = valueStr;
        _originalValue = valueStr;
        _isValueLoading = false;
        
        // Clear any validation errors
        _validationError = string.Empty;
        _hasValidationError = false;
        
        // Notify UI about all changes
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(OriginalValue));
        OnPropertyChanged(nameof(IsValueLoading));
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ToggleValue));
        OnPropertyChanged(nameof(PickerValue));
        OnPropertyChanged(nameof(ValidationError));
        OnPropertyChanged(nameof(HasValidationError));
    }
}
