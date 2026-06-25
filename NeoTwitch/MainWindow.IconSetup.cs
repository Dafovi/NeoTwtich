using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using NeoTwitch.Services.Ui;
using WpfBinding = System.Windows.Data.Binding;
using WpfButton = System.Windows.Controls.Button;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ConfigureNavigationIcons()
    {
        NavSettingsButton.Content = CreateNavigationItem("Assets/Icons/nav_panel.png", "Panel");
        NavConnectionsButton.Content = CreateNavigationItem("Assets/Icons/nav_connections.png", "Conexiones");
        NavRulesButton.Content = CreateNavigationItem("Assets/Icons/nav_rules.png", "Alertas");
        NavStripsButton.Content = CreateNavigationItem("Assets/Icons/nav_lights.png", "Luces");
        NavAlexaButton.Content = CreateNavigationItem("Assets/Icons/nav_alexa.png", "Alexa");
        NavAudioButton.Content = CreateNavigationItem("Assets/Icons/nav_audio.png", "Audio");
        NavImagesButton.Content = CreateNavigationItem("Assets/Icons/nav_images.png", "Imagenes");
        NavVideosButton.Content = CreateNavigationItem("Assets/Icons/nav_videos.png", "Videos");
        NavObsButton.Content = CreateNavigationItem("Assets/Icons/nav_obs.png", "OBS");
        NavPreferencesButton.Content = CreateNavigationItem("Assets/Icons/nav_settings.png", "Configuracion");
        NavActivityButton.Content = CreateNavigationItem("Assets/Icons/nav_activity.png", "Actividad");
    }

    private void ArrangeAlertActionCards()
    {
        if (ObsActionCard.Parent is not StackPanel parent)
        {
            return;
        }

        var insertIndex = parent.Children.IndexOf(UseLightsActionCard);
        if (insertIndex < 0)
        {
            return;
        }

        var orderedCards = new UIElement[]
        {
            ObsActionCard,
            AudioActionCard,
            ChatActionCard,
            UseLightsActionCard,
            AlexaActionCard
        };

        foreach (var card in orderedCards)
        {
            parent.Children.Remove(card);
        }

        for (var index = 0; index < orderedCards.Length; index++)
        {
            parent.Children.Insert(insertIndex + index, orderedCards[index]);
        }
    }

    private static StackPanel CreateNavigationItem(string iconPath, string label)
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
        text.SetBinding(
            TextBlock.ForegroundProperty,
            new WpfBinding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(WpfButton), 1)
            });
        panel.Children.Add(text);
        return panel;
    }

    private static Border CreateTintedImageIcon(string iconPath, double size)
    {
        var icon = new Border
        {
            Width = size,
            Height = size,
            Background = System.Windows.Media.Brushes.White,
            OpacityMask = new ImageBrush
            {
                ImageSource = PackImageLoader.Load(iconPath),
                Stretch = Stretch.Uniform
            }
        };

        icon.SetBinding(
            Border.BackgroundProperty,
            new WpfBinding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(WpfButton), 1)
            });

        return icon;
    }

    private void ConfigureActionIcons()
    {
        foreach (var button in FindVisualChildren<WpfButton>(this))
        {
            if (IsColorButton(button) || button.Content is not string label)
            {
                continue;
            }

            if (ButtonIconCatalog.TryGetIconKey(label, out var iconKey))
            {
                SetButtonIcon(button, label.Trim(), iconKey);
            }
        }
    }

    private static void SetButtonIcon(WpfButton button, string label, string iconKey)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(CreateIconPath(IconPathCatalog.Get(iconKey), 15, 1.9));
        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        text.SetBinding(
            TextBlock.ForegroundProperty,
            new WpfBinding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(WpfButton), 1)
            });
        panel.Children.Add(text);

        button.Content = panel;
    }

    private static System.Windows.Shapes.Path CreateIconPath(string data, double size, double strokeThickness)
    {
        var path = new System.Windows.Shapes.Path
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

        path.SetBinding(
            Shape.StrokeProperty,
            new WpfBinding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(WpfButton), 1)
            });

        return path;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
