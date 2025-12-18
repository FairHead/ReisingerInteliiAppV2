using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Threading;

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
    // ? FIX: Replace boolean flag with lock object to prevent MessageQueue barrier corruption
    private readonly object _gestureLock = new object();
    private int _inputBlockCount = 0; // counts active interactive presses from device controls
    private int _moveModeCount = 0; // counts devices in move mode

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

    // Listen for temporary pan blocking while interacting with device buttons
#pragma warning disable CS0618
    MessagingCenter.Subscribe<ReisingerIntelliApp_V4.Components.PlacedDeviceControl, bool>(this, "PanInputBlock", (sender, isPressed) =>
    {
        // ? FIX: Protect _inputBlockCount access with lock
        lock (_gestureLock)
        {
            _inputBlockCount = Math.Max(0, _inputBlockCount + (isPressed ? 1 : -1));
            System.Diagnostics.Debug.WriteLine($"[PanPinchContainer] Pan block count = {_inputBlockCount}");
        }
    });
    // Listen for global move mode state to block ALL pan gestures during device movement
    MessagingCenter.Subscribe<ReisingerIntelliApp_V4.Components.PlacedDeviceControl, bool>(this, "GlobalMoveMode", (sender, isInMoveMode) =>
    {
        lock (_gestureLock)
        {
            _moveModeCount = Math.Max(0, _moveModeCount + (isInMoveMode ? 1 : -1));
            System.Diagnostics.Debug.WriteLine($"[PanPinchContainer] Move mode count = {_moveModeCount}");
        }
    });
#pragma warning restore CS0618
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

    // ? FIX: Use lock instead of fire-and-forget Task.Run to prevent MessageQueue corruption
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Content is null) return;
        
        // Try to acquire lock - if already processing a gesture, skip this one
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_gestureLock, ref lockTaken);
            if (!lockTaken || _inputBlockCount > 0 || _moveModeCount > 0) return;

            _startScale = Content.Scale;
            _currentScale = _startScale;
            _panX = Content.TranslationX;
            _panY = Content.TranslationY;

            _currentScale = _currentScale < 2 ? 2 : 1;

            var point = e.GetPosition(sender as View);

            // Execute animation on UI thread with proper async/await
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Error in OnDoubleTapped animation: {ex.Message}");
                }
            });
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(_gestureLock);
        }
    }

    // ? FIX: Use lock instead of fire-and-forget Task.Run to prevent MessageQueue corruption
    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Try to acquire lock - if already processing a gesture, skip this one
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_gestureLock, ref lockTaken);
            if (!lockTaken || _inputBlockCount > 0 || _moveModeCount > 0) return;
            
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
                // Direct synchronous update - inline clamp logic to avoid async
                var transX = _panX + e.TotalX;
                var transY = _panY + e.TotalY;
                
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

                Content.TranslationX = transX;
                Content.TranslationY = transY;
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
        finally
        {
            if (lockTaken)
                Monitor.Exit(_gestureLock);
        }
    }

    // ? FIX: Use lock instead of fire-and-forget Task.Run to prevent MessageQueue corruption
    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (Content is null) return;
        
        // Try to acquire lock - if already processing a gesture, skip this one
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_gestureLock, ref lockTaken);
            if (!lockTaken || _inputBlockCount > 0 || _moveModeCount > 0) return;

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

                // Inline clamp from scale origin to avoid async
                double renderedX = Content.X + _panX;
                double deltaX = renderedX / Width;
                double deltaWidth = Width / (Content.Width * _startScale);
                double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                double renderedY = Content.Y + _panY;
                double deltaY = renderedY / Height;
                double deltaHeight = Height / (Content.Height * _startScale);
                double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

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

                Content.TranslationX = targetX;
                Content.TranslationY = targetY;
                Content.Scale = _currentScale;
            }

            if (e.Status == GestureStatus.Completed)
            {
                if (_currentScale < 1)
                {
                    // Execute animation on UI thread with proper async/await
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            var translateTask = TranslateToAsync(0, 0);
                            var scaleTask = ScaleToAsync(1);
                            await Task.WhenAll(translateTask, scaleTask);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"?? Error in OnPinchUpdated animation: {ex.Message}");
                        }
                    });
                }

                _panX = Content.TranslationX;
                _panY = Content.TranslationY;
                _isPanEnabled = true;
            }
            else if (e.Status == GestureStatus.Canceled)
            {
                Content.TranslationX = _panX;
                Content.Scale = _startScale;
                _isPanEnabled = true;
            }
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(_gestureLock);
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
