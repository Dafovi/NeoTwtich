using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using WpfApplication = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace NeoTwitch.Services;

public sealed class VirtualLightsScreenOverlayService
{
    private VirtualLightsOverlayWindow? _window;

    public async Task ShowAsync(VirtualLightCommand command, VirtualScreenInfo screen)
    {
        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            _window ??= new VirtualLightsOverlayWindow();
            _window.Start(command, screen);
            if (!_window.IsVisible)
            {
                _window.Show();
            }
        });
    }

    public async Task HideAsync()
    {
        await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
        {
            _window?.Stop();
            _window?.Hide();
        });
    }
}

internal sealed class VirtualLightsOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private const double DotSpacing = 42d;
    private const double DotInset = 22d;
    private const int MinDots = 56;
    private const int MaxDots = 260;

    private readonly Canvas _canvas = new() { IsHitTestVisible = false };
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<Ellipse> _dots = [];
    private VirtualLightCommand _command = new(LightPattern.Solid, "#14B8A6", "#B56CFF", "#FFFFFF", 180, 2500, 400, 120);
    private int _step;
    private int _lastDotCount;

    public VirtualLightsOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = WpfBrushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Focusable = false;
        IsHitTestVisible = false;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Content = _canvas;

        SourceInitialized += (_, _) => MakeClickThrough();
        SizeChanged += (_, _) => EnsureDots();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, _) => RenderFrame();
    }

    public void Start(VirtualLightCommand command, VirtualScreenInfo screen)
    {
        _command = command;
        ApplyScreen(screen);
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Clamp(command.StepMs, 35, 220));
        EnsureDots(force: true);
        RenderFrame();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void ApplyScreen(VirtualScreenInfo screen)
    {
        Left = screen.Left;
        Top = screen.Top;
        Width = Math.Max(320, screen.Width);
        Height = Math.Max(240, screen.Height);
    }

    private void EnsureDots(bool force = false)
    {
        var width = Math.Max(0, ActualWidth > 0 ? ActualWidth : Width);
        var height = Math.Max(0, ActualHeight > 0 ? ActualHeight : Height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var perimeter = Math.Max(1d, (width + height - (DotInset * 4d)) * 2d);
        var dotCount = Math.Clamp((int)Math.Round(perimeter / DotSpacing), MinDots, MaxDots);
        if (!force && dotCount == _lastDotCount && _dots.Count == dotCount)
        {
            return;
        }

        _lastDotCount = dotCount;
        _dots.Clear();
        _canvas.Children.Clear();

        for (var i = 0; i < dotCount; i++)
        {
            var dot = new Ellipse
            {
                IsHitTestVisible = false,
                Fill = WpfBrushes.Transparent,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 0,
                    Opacity = 0.78
                }
            };
            _dots.Add(dot);
            _canvas.Children.Add(dot);
        }
    }

    private void RenderFrame()
    {
        EnsureDots();
        if (_dots.Count == 0)
        {
            return;
        }

        var brightness = Math.Clamp(_command.Brightness / 255d, 0.08, 1d);
        var primary = LedPreviewService.ParseColor(_command.PrimaryColor, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(_command.SecondaryColor, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(_command.TertiaryColor, "#FFFFFF");
        var frame = LedPreviewService.BuildFrame(_command.Pattern, _step++, _dots.Count, brightness, primary, secondary, tertiary, _random);
        var dotSize = 8d + (brightness * 10d);
        var glow = 12d + (brightness * 32d);
        var width = Math.Max(0, ActualWidth > 0 ? ActualWidth : Width);
        var height = Math.Max(0, ActualHeight > 0 ? ActualHeight : Height);
        var perimeter = Math.Max(1d, ((width - (DotInset * 2d)) + (height - (DotInset * 2d))) * 2d);

        for (var i = 0; i < _dots.Count; i++)
        {
            var color = frame[i];
            var dot = _dots[i];
            var point = PointOnPerimeter(i * (perimeter / _dots.Count), width, height, DotInset);
            dot.Width = dotSize;
            dot.Height = dotSize;
            dot.Fill = new SolidColorBrush(color) { Opacity = Math.Clamp(0.44d + (brightness * 0.56d), 0d, 1d) };
            Canvas.SetLeft(dot, point.X - (dotSize / 2d));
            Canvas.SetTop(dot, point.Y - (dotSize / 2d));

            if (dot.Effect is DropShadowEffect shadow)
            {
                shadow.Color = color;
                shadow.Opacity = Math.Clamp(0.55d + (brightness * 0.35d), 0d, 1d);
                shadow.BlurRadius = glow;
            }
        }
    }

    private static WpfPoint PointOnPerimeter(double distance, double width, double height, double inset)
    {
        var innerWidth = Math.Max(1d, width - (inset * 2d));
        var innerHeight = Math.Max(1d, height - (inset * 2d));
        var top = innerWidth;
        var right = innerHeight;
        var bottom = innerWidth;
        var left = innerHeight;
        var perimeter = top + right + bottom + left;
        var d = ((distance % perimeter) + perimeter) % perimeter;

        if (d < top)
        {
            return new WpfPoint(inset + d, inset);
        }

        d -= top;
        if (d < right)
        {
            return new WpfPoint(width - inset, inset + d);
        }

        d -= right;
        if (d < bottom)
        {
            return new WpfPoint(width - inset - d, height - inset);
        }

        d -= bottom;
        return new WpfPoint(inset, height - inset - Math.Min(d, left));
    }

    private void MakeClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(handle, GwlExStyle);
        _ = SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
