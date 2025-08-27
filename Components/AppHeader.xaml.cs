using Microsoft.Maui.Controls;

namespace ReisingerIntelliApp_V4.Components;

public partial class AppHeader : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), 
        typeof(string), 
        typeof(AppHeader), 
        "Reisinger App",
        propertyChanged: OnTitleChanged);

    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), 
        typeof(Color), 
        typeof(AppHeader), 
        Colors.White,
        propertyChanged: OnTitleColorChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), 
        typeof(double), 
        typeof(AppHeader), 
        20.0,
        propertyChanged: OnFontSizeChanged);

    public static readonly BindableProperty ShowBackButtonProperty = BindableProperty.Create(
        nameof(ShowBackButton), 
        typeof(bool), 
        typeof(AppHeader), 
        false,
        propertyChanged: OnShowBackButtonChanged);

    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(
        nameof(BackCommand), 
        typeof(System.Windows.Input.ICommand), 
        typeof(AppHeader), 
        null);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public System.Windows.Input.ICommand BackCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public AppHeader()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeader header && newValue is string title)
        {
            header.TitleLabel.Text = title;
        }
    }

    private static void OnTitleColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeader header && newValue is Color color)
        {
            header.TitleLabel.TextColor = color;
        }
    }

    private static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeader header && newValue is double fontSize)
        {
            header.TitleLabel.FontSize = fontSize;
        }
    }

    private static void OnShowBackButtonChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeader header && newValue is bool showBack)
        {
            header.BackButton.IsVisible = showBack;
        }
    }
}
