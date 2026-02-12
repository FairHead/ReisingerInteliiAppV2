using ReisingerIntelliApp_V4.Models;
using ReisingerIntelliApp_V4.ViewModels;
using System.Text.Json;

namespace ReisingerIntelliApp_V4.Views;

[QueryProperty(nameof(DeviceData), "deviceData")]
public partial class DeviceParametersPage : ContentPage
{
    private DeviceParametersPageViewModel? _viewModel;
    private string? _deviceData;
    private bool _parametersLoadStarted;

    public string? DeviceData
    {
        get => _deviceData;
        set
        {
            _deviceData = value;
            ProcessDeviceData();
        }
    }

    public DeviceParametersPage(DeviceParametersPageViewModel viewModel)
    {
        Console.WriteLine($"?? DeviceParametersPage constructor - START");
        
        // Set BindingContext BEFORE InitializeComponent
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        InitializeComponent();
        
        Console.WriteLine($"   ? ViewModel injected: {_viewModel != null}");
        Console.WriteLine($"   ? BindingContext set: {BindingContext != null}");
        
        SetupFooterEvents();
        
        Console.WriteLine($"?? DeviceParametersPage constructor - COMPLETE");
    }

    private void ProcessDeviceData()
    {
        if (string.IsNullOrEmpty(_deviceData) || _viewModel == null) return;
        
        try
        {
            var decoded = Uri.UnescapeDataString(_deviceData);
            Console.WriteLine($"?? DeviceParametersPage received data: {decoded}");
            
            using var doc = JsonDocument.Parse(decoded);
            var root = doc.RootElement;
            
            var deviceId = root.TryGetProperty("deviceId", out var idProp) ? idProp.GetString() : null;
            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var ip = root.TryGetProperty("ip", out var ipProp) ? ipProp.GetString() : null;
            var ssid = root.TryGetProperty("ssid", out var ssidProp) ? ssidProp.GetString() : null;
            var username = root.TryGetProperty("username", out var userProp) ? userProp.GetString() : null;
            var password = root.TryGetProperty("password", out var passProp) ? passProp.GetString() : null;
            
            // Create a DeviceModel with auth credentials for the ViewModel
            var device = new DeviceModel
            {
                DeviceId = deviceId ?? string.Empty,
                Name = name ?? "Unbekanntes Gerät",
                Ip = ip ?? string.Empty,
                IpAddress = ip ?? string.Empty,
                Ssid = ssid ?? string.Empty,
                Username = username ?? string.Empty,
                Password = password ?? string.Empty
            };
            
            // Only set device info - don't initialize parameters yet!
            // Let the page render first, then load in OnAppearing
            _viewModel.SetDevice(device);
            Console.WriteLine($"?? Device set: {device.Name} ({device.Ip}) with auth: {!string.IsNullOrEmpty(device.Username)}");

            // Warn user if credentials are missing
            if (string.IsNullOrWhiteSpace(device.Username) || string.IsNullOrWhiteSpace(device.Password))
            {
                Console.WriteLine("?? Device credentials missing — API calls will fail");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert(
                        "Zugangsdaten fehlen",
                        $"Für '{device.Name}' sind kein Benutzername/Passwort hinterlegt.\n\nParameter können nicht geladen werden. Bitte unter Geräte-Einstellungen die Zugangsdaten eingeben.",
                        "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error processing device data: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"??? DeviceParametersPage.OnAppearing - page is now visible");
        
        // Now that page is visible, initialize placeholders and start loading
        if (_viewModel != null && !_parametersLoadStarted)
        {
            _parametersLoadStarted = true;
            
            // Small delay to ensure page has rendered
            await Task.Delay(50);
            
            // Initialize placeholders and load all data (parallel API calls)
            await _viewModel.InitializeAndLoadAsync();
        }
    }

    private void SetupFooterEvents()
    {
        if (_viewModel != null)
        {
            Footer.CenterButtonTapped += (s, e) => _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DeviceParametersPage] OnBackButtonClicked - navigating back");
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Handle click on "Alle" category button - show all categories
    /// </summary>
    private void OnAllCategoriesClicked(object sender, EventArgs e)
    {
        if (_viewModel == null) return;
        
        System.Diagnostics.Debug.WriteLine("?? All categories selected");
        _viewModel.SelectedCategory = null;
        
        // Update button visuals
        UpdateCategoryButtonStyles(null);
    }

    /// <summary>
    /// Handle click on a specific category button
    /// </summary>
    private void OnCategoryClicked(object sender, EventArgs e)
    {
        if (_viewModel == null) return;
        if (sender is not Button button) return;
        
        var categoryName = button.CommandParameter?.ToString();
        if (string.IsNullOrEmpty(categoryName)) return;
        
        // Parse category from string
        if (Enum.TryParse<ParameterCategory>(categoryName, out var category))
        {
            System.Diagnostics.Debug.WriteLine($"?? Category selected: {category}");
            _viewModel.SelectedCategory = category;
            
            // Update button visuals
            UpdateCategoryButtonStyles(category);
        }
    }

    /// <summary>
    /// Updates the visual style of category buttons to highlight the selected one
    /// </summary>
    private void UpdateCategoryButtonStyles(ParameterCategory? selectedCategory)
    {
        // Find all category buttons in the ScrollView
        // This is a simple approach - for complex scenarios, consider using triggers or behaviors
        var scrollView = this.FindByName<ScrollView>("CategoryScrollView");
        // Button styles are currently handled via binding in XAML
        // Future enhancement: Add visual feedback for selected category
    }

    /// <summary>
    /// Handle tap on parameter value to edit it
    /// </summary>
    private async void OnParameterValueTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not DeviceParameterDisplayModel param)
            return;

        // Don't allow editing read-only or reserved parameters
        if (!param.IsEditable)
        {
            System.Diagnostics.Debug.WriteLine($"?? Parameter {param.Id} ({param.Name}) is read-only");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"?? Editing Parameter {param.Id} ({param.Name}), current value: {param.Value}");

        // Show prompt to edit value - now shows dynamic range if available
        var result = await DisplayPromptAsync(
            $"Parameter #{param.Id}",
            $"{param.Name}\nBereich: {param.RangeText}",
            "OK",
            "Abbrechen",
            placeholder: param.Value,
            initialValue: param.Value,
            keyboard: Keyboard.Numeric
        );

        if (result != null)
        {
            System.Diagnostics.Debug.WriteLine($"?? User entered: '{result}' for Parameter {param.Id}");
            param.Value = result;
            _viewModel?.UpdateValidationState();
        }
    }
}