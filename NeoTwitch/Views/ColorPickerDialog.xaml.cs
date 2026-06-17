using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
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
        _currentColor = ParseColor(initialHex, WpfColor.FromRgb(152, 92, 246));
        SelectedColorHex = ToHex(_currentColor);
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

        foreach (var textBox in FindVisualChildren<WpfTextBox>(this))
        {
            textBox.Background = panelBrush;
            textBox.BorderBrush = borderBrush;
            textBox.Foreground = textBrush;
            textBox.CaretBrush = textBrush;
            textBox.FontFamily = FontFamily;
        }

        foreach (var button in FindVisualChildren<WpfButton>(this))
        {
            button.Background = buttonBrush;
            button.BorderBrush = borderBrush;
            button.Foreground = textBrush;
            button.FontFamily = FontFamily;
        }

        foreach (var block in FindVisualChildren<TextBlock>(this))
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
            .Select(color => ToHex(ParseColor(color, Colors.Transparent)))
            .Where(color => color != "#000000")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(7);

        foreach (var colorHex in colors)
        {
            var button = new WpfButton
            {
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 10, 0),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(ParseColor(colorHex, Colors.White)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(51, 65, 85)),
                ToolTip = colorHex,
                Tag = colorHex
            };

            button.Click += (_, _) =>
            {
                if (button.Tag is string hex)
                {
                    SetFromColor(ParseColor(hex, _currentColor));
                }
            };

            RecentColorsPanel.Children.Add(button);
        }
    }

    private void SetFromColor(WpfColor color)
    {
        RgbToHsv(color, out _hue, out _saturation, out _value);
        UpdateHueSurface();
        UpdateHueThumb();
        UpdateThumb();
        UpdateFields(color);
    }

    private void UpdateFromHsv()
    {
        var color = HsvToRgb(_hue, _saturation, _value);
        UpdateFields(color);
    }

    private void UpdateFields(WpfColor color)
    {
        _updatingFields = true;
        try
        {
            SelectedColorHex = ToHex(color);
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
        HueSurface.Fill = new SolidColorBrush(HsvToRgb(_hue, 1, 1));
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
        var top = (_hue / 360d) * HueCanvas.Height - HueThumb.Height / 2;
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
        _hue = Math.Clamp(point.Y / HueCanvas.Height, 0, 1) * 360d;
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
            SetFromColor(ParseColor(text, HsvToRgb(_hue, _saturation, _value)));
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

    private static WpfColor ParseColor(string? value, WpfColor fallback)
    {
        try
        {
            var text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            if (!text.StartsWith('#'))
            {
                text = $"#{text}";
            }

            return (WpfColor)WpfColorConverter.ConvertFromString(text)!;
        }
        catch
        {
            return fallback;
        }
    }

    private static string ToHex(WpfColor color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static WpfColor HsvToRgb(double hue, double saturation, double value)
    {
        var chroma = value * saturation;
        var huePrime = (hue % 360) / 60.0;
        var x = chroma * (1 - Math.Abs(huePrime % 2 - 1));

        var (r1, g1, b1) = huePrime switch
        {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };

        var match = value - chroma;
        return WpfColor.FromRgb(
            (byte)Math.Round((r1 + match) * 255),
            (byte)Math.Round((g1 + match) * 255),
            (byte)Math.Round((b1 + match) * 255));
    }

    private static void RgbToHsv(WpfColor color, out double hue, out double saturation, out double value)
    {
        var red = color.R / 255d;
        var green = color.G / 255d;
        var blue = color.B / 255d;
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        hue = delta switch
        {
            0 => 0,
            _ when max == red => 60 * (((green - blue) / delta) % 6),
            _ when max == green => 60 * (((blue - red) / delta) + 2),
            _ => 60 * (((red - green) / delta) + 4)
        };

        if (hue < 0)
        {
            hue += 360;
        }

        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
