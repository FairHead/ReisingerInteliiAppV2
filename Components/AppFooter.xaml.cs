using Microsoft.Maui.Controls;

namespace ReisingerIntelliApp_V4.Components;

public partial class AppFooter : ContentView
{
    // Left Section Properties
    public static readonly BindableProperty LeftIconProperty = BindableProperty.Create(
        nameof(LeftIcon), typeof(string), typeof(AppFooter), "home.svg", propertyChanged: OnLeftIconChanged);

    public static readonly BindableProperty LeftTextProperty = BindableProperty.Create(
        nameof(LeftText), typeof(string), typeof(AppFooter), "My Place", propertyChanged: OnLeftTextChanged);

    // Center Button Properties
    public static readonly BindableProperty CenterIconProperty = BindableProperty.Create(
        nameof(CenterIcon), typeof(string), typeof(AppFooter), "+", propertyChanged: OnCenterIconChanged);

    public static readonly BindableProperty CenterColorProperty = BindableProperty.Create(
        nameof(CenterColor), typeof(Color), typeof(AppFooter), Color.FromArgb("#007AFF"), propertyChanged: OnCenterColorChanged);

    // Right Section Properties
    public static readonly BindableProperty RightIconProperty = BindableProperty.Create(
        nameof(RightIcon), typeof(string), typeof(AppFooter), "settings.svg", propertyChanged: OnRightIconChanged);

    public static readonly BindableProperty RightTextProperty = BindableProperty.Create(
        nameof(RightText), typeof(string), typeof(AppFooter), "Preferences", propertyChanged: OnRightTextChanged);

    // Properties
    public string LeftIcon
    {
        get => (string)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    public string LeftText
    {
        get => (string)GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    public string CenterIcon
    {
        get => (string)GetValue(CenterIconProperty);
        set => SetValue(CenterIconProperty, value);
    }

    public Color CenterColor
    {
        get => (Color)GetValue(CenterColorProperty);
        set => SetValue(CenterColorProperty, value);
    }

    public string RightIcon
    {
        get => (string)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    public string RightText
    {
        get => (string)GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }

    // Events
    public event EventHandler? LeftSectionTapped;
    public event EventHandler? CenterButtonTapped;
    public event EventHandler? RightSectionTapped;

    public AppFooter()
    {
        InitializeComponent();
        SetupGestureRecognizers();
    }

    private void SetupGestureRecognizers()
    {
        var leftTap = new TapGestureRecognizer();
        leftTap.Tapped += (s, e) => LeftSectionTapped?.Invoke(this, EventArgs.Empty);
        LeftSection.GestureRecognizers.Add(leftTap);

        var centerTap = new TapGestureRecognizer();
        centerTap.Tapped += (s, e) => CenterButtonTapped?.Invoke(this, EventArgs.Empty);
        CenterButton.GestureRecognizers.Add(centerTap);

        var rightTap = new TapGestureRecognizer();
        rightTap.Tapped += (s, e) => RightSectionTapped?.Invoke(this, EventArgs.Empty);
        RightSection.GestureRecognizers.Add(rightTap);
    }

    // Property Changed Methods
    private static void OnLeftIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is string icon)
        {
            // Check if it's an image file (SVG, PNG, etc.)
            if (icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                // Use the Image control for SVG/image files
                footer.LeftIconImage.Source = ImageSource.FromFile(icon);
                footer.LeftIconImage.IsVisible = true;
                footer.LeftIconLabel.IsVisible = false;
            }
            else
            {
                // Use text/emoji for non-image icons
                footer.LeftIconLabel.Text = icon;
                footer.LeftIconLabel.IsVisible = true;
                footer.LeftIconImage.IsVisible = false;
            }
        }
    }

    private static void OnLeftTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is string text)
            footer.LeftTextLabel.Text = text;
    }

    private static void OnCenterIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is string icon)
        {
            // Check if it's an image file (SVG, PNG, etc.)
            if (icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                // Use the Image control for SVG/image files
                footer.CenterIconImage.Source = ImageSource.FromFile(icon);
                footer.CenterIconImage.IsVisible = true;
                footer.CenterIconLabel.IsVisible = false;
            }
            else
            {
                // Use text/emoji for non-image icons
                footer.CenterIconLabel.Text = icon;
                footer.CenterIconLabel.IsVisible = true;
                footer.CenterIconImage.IsVisible = false;
            }
        }
    }

    private static void OnCenterColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is Color color)
            footer.CenterButton.BackgroundColor = color;
    }

    private static void OnRightIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is string icon)
        {
            // Check if it's an image file (SVG, PNG, etc.)
            if (icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                icon.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                // Use the Image control for SVG/image files
                footer.RightIconImage.Source = ImageSource.FromFile(icon);
                footer.RightIconImage.IsVisible = true;
                footer.RightIconLabel.IsVisible = false;
            }
            else
            {
                // Use text/emoji for non-image icons
                footer.RightIconLabel.Text = icon;
                footer.RightIconLabel.IsVisible = true;
                footer.RightIconImage.IsVisible = false;
            }
        }
    }

    private static void OnRightTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppFooter footer && newValue is string text)
            footer.RightTextLabel.Text = text;
    }
}
