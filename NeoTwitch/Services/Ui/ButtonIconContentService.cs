using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfBinding = System.Windows.Data.Binding;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace NeoTwitch.Services.Ui;

public static class ButtonIconContentService
{
    public static StackPanel CreateNavigationItem(string iconPath, string label)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(CreateTintedImageIcon(iconPath, 18));
        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        BindToAncestorForeground<TextBlock>(text, TextBlock.ForegroundProperty, typeof(WpfButton));
        panel.Children.Add(text);

        return panel;
    }

    public static bool TrySetButtonIcon(WpfButton button, string label)
    {
        if (!ButtonIconCatalog.TryGetIconKey(label, out var iconKey))
        {
            return false;
        }

        SetButtonIcon(button, label.Trim(), iconKey);
        return true;
    }

    public static void SetButtonIcon(WpfButton button, string label, string iconKey)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(CreateIconPath(IconPathCatalog.Get(iconKey), 15, 1.9));
        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        BindToAncestorForeground<TextBlock>(text, TextBlock.ForegroundProperty, typeof(WpfButton));
        panel.Children.Add(text);

        button.Content = panel;
    }

    private static Border CreateTintedImageIcon(string iconPath, double size)
    {
        var icon = new Border
        {
            Width = size,
            Height = size,
            Background = WpfBrushes.White,
            OpacityMask = new ImageBrush
            {
                ImageSource = PackImageLoader.Load(iconPath),
                Stretch = Stretch.Uniform
            }
        };

        BindToAncestorForeground<Border>(icon, Border.BackgroundProperty, typeof(WpfButton));
        return icon;
    }

    private static Path CreateIconPath(string data, double size, double strokeThickness)
    {
        var path = new Path
        {
            Data = Geometry.Parse(data),
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StrokeThickness = strokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };

        BindToAncestorForeground<Path>(path, Shape.StrokeProperty, typeof(WpfButton));
        return path;
    }

    private static void BindToAncestorForeground<TElement>(
        TElement element,
        DependencyProperty property,
        Type ancestorType)
        where TElement : FrameworkElement
    {
        element.SetBinding(
            property,
            new WpfBinding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, ancestorType, 1)
            });
    }
}
