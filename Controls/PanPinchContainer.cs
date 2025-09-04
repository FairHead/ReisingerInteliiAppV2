using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace ReisingerIntelliApp_V4.Controls;

/// <summary>
/// ContentView that enables pinch-to-zoom, double-tap zoom, and panning for its child.
/// Ported from V3 implementation and adapted for .NET MAUI.
/// </summary>
public class PanPinchContainer : ContentView
{
    private readonly TapGestureRecognizer _doubleTapGestureRecognizer;
    private readonly PanGestureRecognizer _panGestureRecognizer;
    private readonly PinchGestureRecognizer _pinchGestureRecognizer;

    private double _currentScale = 1;
    private bool _isPanEnabled = true;
    private double _panX;
    private double _panY;
    private double _startScale = 1;
    private bool _isProcessingGesture = false;

    public PanPinchContainer()
    {
        _panGestureRecognizer = new PanGestureRecognizer();
        _panGestureRecognizer.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(_panGestureRecognizer);

        _pinchGestureRecognizer = new PinchGestureRecognizer();
        _pinchGestureRecognizer.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(_pinchGestureRecognizer);

        _doubleTapGestureRecognizer = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        _doubleTapGestureRecognizer.Tapped += OnDoubleTapped;
        GestureRecognizers.Add(_doubleTapGestureRecognizer);
    }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);

        if (child is View view)
        {
            view.HorizontalOptions = LayoutOptions.Center;
            view.VerticalOptions = LayoutOptions.Center;
        }
    }

    private async Task ClampTranslationAsync(double transX, double transY, bool animate = false)
    {
        if (Content is null) return;

        Content.AnchorX = 0;
        Content.AnchorY = 0;

        double contentWidth = Content.Width * _currentScale;
        double contentHeight = Content.Height * _currentScale;

        if (contentWidth <= Width)
        {
            transX = -(contentWidth - Content.Width) / 2;
        }
        else
        {
            double minBoundX = ((Width - Content.Width) / 2) + contentWidth - Width;
            double maxBoundX = (Width - Content.Width) / 2;
            transX = Math.Clamp(transX, -minBoundX, -maxBoundX);
        }

        if (contentHeight <= Height)
        {
            transY = -(contentHeight - Content.Height) / 2;
        }
        else
        {
            double minBoundY = ((Height - Content.Height) / 2) + contentHeight - Height;
            double maxBoundY = (Height - Content.Height) / 2;
            transY = Math.Clamp(transY, -minBoundY, -maxBoundY);
        }

        if (animate)
        {
            await TranslateToAsync(transX, transY);
        }
        else
        {
            Content.TranslationX = transX;
            Content.TranslationY = transY;
        }
    }

    private async Task ClampTranslationFromScaleOriginAsync(double originX, double originY, bool animate = false)
    {
        if (Content is null) return;

        double renderedX = Content.X + _panX;
        double deltaX = renderedX / Width;
        double deltaWidth = Width / (Content.Width * _startScale);
        originX = (originX - deltaX) * deltaWidth;

        double renderedY = Content.Y + _panY;
        double deltaY = renderedY / Height;
        double deltaHeight = Height / (Content.Height * _startScale);
        originY = (originY - deltaY) * deltaHeight;

        double targetX = _panX - (originX * Content.Width * (_currentScale - _startScale));
        double targetY = _panY - (originY * Content.Height * (_currentScale - _startScale));

        if (_currentScale > 1)
        {
            targetX = Math.Clamp(targetX, -Content.Width * (_currentScale - 1), 0);
            targetY = Math.Clamp(targetY, -Content.Height * (_currentScale - 1), 0);
        }
        else
        {
            targetX = (Width - (Content.Width * _currentScale)) / 2;
            targetY = Content.Height * (1 - _currentScale) / 2;
        }

        await ClampTranslationAsync(targetX, targetY, animate);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_isProcessingGesture || Content is null) return;
        
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                _isProcessingGesture = true;
                await DoubleTappedAsync(sender, e);
            }
            finally
            {
                _isProcessingGesture = false;
            }
        });
    }

    private async Task DoubleTappedAsync(object? sender, TappedEventArgs e)
    {
        if (Content is null) return;

        _startScale = Content.Scale;
        _currentScale = _startScale;
        _panX = Content.TranslationX;
        _panY = Content.TranslationY;

        _currentScale = _currentScale < 2 ? 2 : 1;

        var point = e.GetPosition(sender as View);

        var translateTask = Task.CompletedTask;
        if (point is not null)
        {
            translateTask = ClampTranslationFromScaleOriginAsync(point.Value.X / Width, point.Value.Y / Height, true);
        }

        var scaleTask = ScaleToAsync(_currentScale);
        await Task.WhenAll(translateTask, scaleTask);

        _panX = Content.TranslationX;
        _panY = Content.TranslationY;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_isProcessingGesture) return;
        
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                _isProcessingGesture = true;
                await OnPanUpdatedAsync(sender, e);
            }
            finally
            {
                _isProcessingGesture = false;
            }
        });
    }

    private async Task OnPanUpdatedAsync(object? sender, PanUpdatedEventArgs e)
    {
        if (!_isPanEnabled || Content is null)
            return;

        if (Content.Scale <= 1)
            return;

        if (e.StatusType == GestureStatus.Started)
        {
            _panX = Content.TranslationX;
            _panY = Content.TranslationY;
            Content.AnchorX = 0;
            Content.AnchorY = 0;
        }
        else if (e.StatusType == GestureStatus.Running)
        {
            await ClampTranslationAsync(_panX + e.TotalX, _panY + e.TotalY);
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            _panX = Content.TranslationX;
            _panY = Content.TranslationY;
        }
        else if (e.StatusType == GestureStatus.Canceled)
        {
            Content.TranslationX = _panX;
            Content.TranslationY = _panY;
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (_isProcessingGesture) return;
        
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                _isProcessingGesture = true;
                await OnPinchUpdatedAsync(sender, e);
            }
            finally
            {
                _isProcessingGesture = false;
            }
        });
    }

    private async Task OnPinchUpdatedAsync(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (Content is null) return;

        if (e.Status == GestureStatus.Started)
        {
            _isPanEnabled = false;
            _panX = Content.TranslationX;
            _panY = Content.TranslationY;
            _startScale = Content.Scale;
            Content.AnchorX = 0;
            Content.AnchorY = 0;
        }

        if (e.Status == GestureStatus.Running)
        {
            _currentScale += (e.Scale - 1) * _startScale;
            _currentScale = Math.Clamp(_currentScale, 0.5, 10);

            await ClampTranslationFromScaleOriginAsync(e.ScaleOrigin.X, e.ScaleOrigin.Y);

            Content.Scale = _currentScale;
        }

        if (e.Status == GestureStatus.Completed)
        {
            if (_currentScale < 1)
            {
                var translateTask = TranslateToAsync(0, 0);
                var scaleTask = ScaleToAsync(1);
                await Task.WhenAll(translateTask, scaleTask);
            }

            _panX = Content.TranslationX;
            _panY = Content.TranslationY;
            _isPanEnabled = true;
        }
        else if (e.Status == GestureStatus.Canceled)
        {
            Content.TranslationX = _panX;
            Content.TranslationY = _panY;
            Content.Scale = _startScale;
            _isPanEnabled = true;
        }
    }

    private async Task ScaleToAsync(double scale)
    {
        if (Content is null) return;
        await Content.ScaleTo(scale, 250, Easing.Linear);
        _currentScale = scale;
    }

    private async Task TranslateToAsync(double x, double y)
    {
        if (Content is null) return;
        await Content.TranslateTo(x, y, 250, Easing.Linear);
        _panX = x;
        _panY = y;
    }
}
