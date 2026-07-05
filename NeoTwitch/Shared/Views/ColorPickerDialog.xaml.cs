using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch.Views;

public partial class ColorPickerDialog : Window
{
    private readonly WpfColor _currentColor;
    private readonly bool _darkMode;
    private bool _updatingFields;
    private double _hue;
    private double _saturation;
    private double _value;

    public ColorPickerDialog(string initialHex, bool darkMode, IEnumerable<string> recentColors)
    {
        InitializeComponent();
        _darkMode = darkMode;
        _currentColor = ColorConversionService.ParseColor(initialHex, WpfColor.FromRgb(152, 92, 246));
        SelectedColorHex = ColorConversionService.ToHex(_currentColor);
        ApplyDialogTheme();
        BuildRecentColors(recentColors);
        SetFromColor(_currentColor);
        CurrentColorPreview.Background = new SolidColorBrush(_currentColor);
    }

    public string SelectedColorHex { get; private set; }

    private void ApplyDialogTheme()
    {
        var background = _darkMode ? WpfColor.FromRgb(8, 17, 27) : WpfColor.FromRgb(248, 250, 252);
        var titleBar = _darkMode ? WpfColor.FromRgb(7, 18, 28) : WpfColor.FromRgb(248, 250, 252);
        var panel = _darkMode ? WpfColor.FromRgb(15, 30, 45) : WpfColor.FromRgb(255, 255, 255);
        var text = _darkMode ? WpfColor.FromRgb(240, 249, 255) : WpfColor.FromRgb(15, 23, 42);
        var muted = _darkMode ? WpfColor.FromRgb(177, 194, 214) : WpfColor.FromRgb(71, 85, 105);
        var border = _darkMode ? WpfColor.FromRgb(51, 65, 85) : WpfColor.FromRgb(203, 213, 225);

        var textBrush = new SolidColorBrush(text);
        var mutedBrush = new SolidColorBrush(muted);
        var borderBrush = new SolidColorBrush(border);
        var panelBrush = new SolidColorBrush(panel);
        var buttonBrush = new SolidColorBrush(_darkMode ? WpfColor.FromRgb(16, 32, 48) : WpfColor.FromRgb(238, 242, 246));
        var accentBrush = new SolidColorBrush(WpfColor.FromRgb(20, 184, 166));

        FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI");
        Resources["ColorPickerWindowBrush"] = new SolidColorBrush(background);
        Resources["ColorPickerTitleBarBrush"] = new SolidColorBrush(titleBar);
        Resources["ColorPickerInputBrush"] = panelBrush;
        Resources["ColorPickerButtonBrush"] = buttonBrush;
        Resources["ColorPickerBorderBrush"] = borderBrush;
        Resources["ColorPickerTextBrush"] = textBrush;
        Resources["ColorPickerMutedTextBrush"] = mutedBrush;
        Resources["ColorPickerAccentBrush"] = accentBrush;

        RootCard.Background = new SolidColorBrush(background);
        RootCard.BorderBrush = borderBrush;
        RootCard.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, textBrush);
        TitleBar.Background = new SolidColorBrush(titleBar);
        TitleBar.BorderBrush = borderBrush;
        Foreground = textBrush;
        Divider.Background = borderBrush;

        foreach (var textBox in VisualTreeTraversalService.FindChildren<WpfTextBox>(this))
        {
            textBox.Background = panelBrush;
            textBox.BorderBrush = borderBrush;
            textBox.Foreground = textBrush;
            textBox.CaretBrush = textBrush;
            textBox.FontFamily = FontFamily;
        }

        foreach (var button in VisualTreeTraversalService.FindChildren<WpfButton>(this))
        {
            button.Background = buttonBrush;
            button.BorderBrush = borderBrush;
            button.Foreground = textBrush;
            button.FontFamily = FontFamily;
        }

        foreach (var block in VisualTreeTraversalService.FindChildren<TextBlock>(this))
        {
            block.Foreground = textBrush;
            block.FontFamily = FontFamily;
        }

        CloseButton.Background = System.Windows.Media.Brushes.Transparent;
        CloseButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
        CloseButton.Foreground = mutedBrush;
        CloseButton.Padding = new Thickness(0);

        CancelDialogButton.Background = new SolidColorBrush(_darkMode ? WpfColor.FromRgb(10, 24, 38) : WpfColor.FromRgb(248, 250, 252));
        CancelDialogButton.BorderBrush = borderBrush;
        CancelDialogButton.Foreground = textBrush;
        CancelButtonText.Foreground = textBrush;

        ApplyColorButton.Background = accentBrush;
        ApplyColorButton.BorderBrush = new SolidColorBrush(WpfColor.FromRgb(45, 212, 191));
        ApplyColorButton.Foreground = System.Windows.Media.Brushes.White;
        ApplyButtonText.Foreground = System.Windows.Media.Brushes.White;
    }

    private void BuildRecentColors(IEnumerable<string> recentColors)
    {
        var colors = recentColors
            .Append("#985CF6")
            .Append("#6D00F5")
            .Append("#22D3EE")
            .Append("#22C55E")
            .Append("#FACC15")
            .Append("#FF7A18")
            .Append("#F43F5E")
            .Select(color => ColorConversionService.ToHex(ColorConversionService.ParseColor(color, Colors.Transparent)))
            .Where(color => color != "#000000")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ApplicationLimits.MaxRecentColors);

        foreach (var colorHex in colors)
        {
            var button = new WpfButton
            {
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 10, 0),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(ColorConversionService.ParseColor(colorHex, Colors.White)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(51, 65, 85)),
                ToolTip = colorHex,
                Tag = colorHex
            };

            button.Click += (_, _) =>
            {
                if (button.Tag is string hex)
                {
                    SetFromColor(ColorConversionService.ParseColor(hex, _currentColor));
                }
            };

            RecentColorsPanel.Children.Add(button);
        }
    }

    private void SetFromColor(WpfColor color)
    {
        var hsv = ColorConversionService.ToHsv(color);
        _hue = hsv.Hue;
        _saturation = hsv.Saturation;
        _value = hsv.Value;
        UpdateHueSurface();
        UpdateHueThumb();
        UpdateThumb();
        UpdateFields(color);
    }

    private void UpdateFromHsv()
    {
        var color = ColorConversionService.FromHsv(_hue, _saturation, _value);
        UpdateFields(color);
    }

    private void UpdateFields(WpfColor color)
    {
        _updatingFields = true;
        try
        {
            SelectedColorHex = ColorConversionService.ToHex(color);
            HexBox.Text = SelectedColorHex;
            RedBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
            GreenBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
            BlueBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
            NewColorPreview.Background = new SolidColorBrush(color);
        }
        finally
        {
            _updatingFields = false;
        }
    }

    private void UpdateHueSurface()
    {
        HueSurface.Fill = new SolidColorBrush(ColorConversionService.FromHsv(_hue, 1, 1));
    }

    private void UpdateThumb()
    {
        var left = _saturation * ColorCanvas.Width - ColorThumb.Width / 2;
        var top = (1 - _value) * ColorCanvas.Height - ColorThumb.Height / 2;
        Canvas.SetLeft(ColorThumb, Math.Clamp(left, 0, ColorCanvas.Width - ColorThumb.Width));
        Canvas.SetTop(ColorThumb, Math.Clamp(top, 0, ColorCanvas.Height - ColorThumb.Height));
    }

    private void UpdateHueThumb()
    {
        var top = (1 - _hue / 360d) * HueCanvas.Height - HueThumb.Height / 2;
        Canvas.SetTop(HueThumb, Math.Clamp(top, 0, HueCanvas.Height - HueThumb.Height));
    }

    private void ColorCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ColorCanvas.CaptureMouse();
        UpdateColorFromPointer(e.GetPosition(ColorCanvas));
    }

    private void ColorCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            ColorCanvas.ReleaseMouseCapture();
            return;
        }

        UpdateColorFromPointer(e.GetPosition(ColorCanvas));
    }

    private void UpdateColorFromPointer(WpfPoint point)
    {
        _saturation = Math.Clamp(point.X / ColorCanvas.Width, 0, 1);
        _value = Math.Clamp(1 - point.Y / ColorCanvas.Height, 0, 1);
        UpdateThumb();
        UpdateFromHsv();
    }

    private void HueCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        HueCanvas.CaptureMouse();
        UpdateHueFromPointer(e.GetPosition(HueCanvas));
    }

    private void HueCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            HueCanvas.ReleaseMouseCapture();
            return;
        }

        UpdateHueFromPointer(e.GetPosition(HueCanvas));
    }

    private void PickerCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ColorCanvas.ReleaseMouseCapture();
        HueCanvas.ReleaseMouseCapture();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
    }

    private void UpdateHueFromPointer(WpfPoint point)
    {
        _hue = (1 - Math.Clamp(point.Y / HueCanvas.Height, 0, 1)) * 360d;
        UpdateHueThumb();
        UpdateHueSurface();
        UpdateFromHsv();
    }

    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingFields)
        {
            return;
        }

        var text = HexBox.Text.Trim();
        if (!text.StartsWith('#'))
        {
            text = $"#{text}";
        }

        if (text.Length == 7)
        {
            SetFromColor(ColorConversionService.ParseColor(text, ColorConversionService.FromHsv(_hue, _saturation, _value)));
        }
    }

    private void RgbBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingFields)
        {
            return;
        }

        if (byte.TryParse(RedBox.Text, out var red)
            && byte.TryParse(GreenBox.Text, out var green)
            && byte.TryParse(BlueBox.Text, out var blue))
        {
            SetFromColor(WpfColor.FromRgb(red, green, blue));
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

}
