using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfBinding = System.Windows.Data.Binding;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.Control;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfImage = System.Windows.Controls.Image;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfToolTip = System.Windows.Controls.ToolTip;

namespace NeoTwitch.Services.Ui;

public static class ButtonIconContentService
{
    private const string NavigationStatusFrameTag = "NavigationStatusFrame";
    private const string NavigationStatusIconTag = "NavigationStatusIcon";

    public static FrameworkElement CreateNavigationItem(string iconPath, string label)
    {
        var grid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = WpfHorizontalAlignment.Stretch
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = CreateTintedImageIcon(iconPath, 18);
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        BindToAncestorForeground<TextBlock>(text, TextBlock.ForegroundProperty, typeof(WpfButton));
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var statusFrame = new Border
        {
            Tag = NavigationStatusFrameTag,
            Width = 22,
            Height = 22,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = WpfHorizontalAlignment.Right,
            Background = UiBrushFactory.TranslucentBrushFrom("#94A3B8"),
            BorderBrush = UiBrushFactory.FrozenBrushFrom("#94A3B8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Visibility = Visibility.Collapsed,
        };

        var statusIcon = new WpfImage
        {
            Tag = NavigationStatusIconTag,
            Stretch = Stretch.Uniform
        };
        statusFrame.Child = statusIcon;
        Grid.SetColumn(statusFrame, 2);
        grid.Children.Add(statusFrame);

        return grid;
    }

    public static void SetNavigationStatus(
        WpfButton button,
        string iconPath,
        string tooltip,
        bool isVisible,
        string? defaultTooltip = null,
        string? accentColor = null)
    {
        if (button.Content is not DependencyObject content
            || FindChildByTag<Border>(content, NavigationStatusFrameTag) is not { } statusFrame
            || FindChildByTag<WpfImage>(content, NavigationStatusIconTag) is not { } statusIcon)
        {
            return;
        }

        statusFrame.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        button.ToolTip = isVisible ? CreateStatusToolTip(tooltip, accentColor) : defaultTooltip ?? button.ToolTip;
        if (!isVisible)
        {
            return;
        }

        var color = accentColor ?? "#94A3B8";
        statusFrame.Background = UiBrushFactory.TranslucentBrushFrom(color);
        statusFrame.BorderBrush = UiBrushFactory.FrozenBrushFrom(color);
        statusIcon.Source = PackImageLoader.Load(iconPath);
        statusFrame.ToolTip = button.ToolTip;
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

    private static WpfToolTip CreateStatusToolTip(string tooltip, string? accentColor)
    {
        var content = new WpfTextBlock
        {
            Text = tooltip,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 320
        };
        content.SetResourceReference(WpfTextBlock.ForegroundProperty, "ThemeTextBrush");

        var toolTip = new WpfToolTip
        {
            Content = content,
            BorderBrush = UiBrushFactory.FrozenBrushFrom(accentColor ?? "#94A3B8")
        };
        toolTip.SetResourceReference(WpfControl.BackgroundProperty, "ThemeSurfaceBrush");
        toolTip.SetResourceReference(WpfControl.ForegroundProperty, "ThemeTextBrush");
        return toolTip;
    }

    private static TElement? FindChildByTag<TElement>(DependencyObject parent, string tag)
        where TElement : FrameworkElement
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TElement element && Equals(element.Tag, tag))
            {
                return element;
            }

            var descendant = FindChildByTag<TElement>(child, tag);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
