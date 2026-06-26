using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Ui;
using WpfButton = System.Windows.Controls.Button;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ConfigureNavigationIcons()
    {
        NavSettingsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_panel.png", "Panel");
        NavConnectionsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_connections.png", "Conexiones");
        NavRulesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_rules.png", "Alertas");
        NavStripsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_lights.png", "Luces");
        NavAlexaButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_alexa.png", "Alexa");
        NavAudioButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_audio.png", "Audio");
        NavImagesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_images.png", "Imagenes");
        NavVideosButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_videos.png", "Videos");
        NavObsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_obs.png", "OBS");
        NavPreferencesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_settings.png", "Configuracion");
        NavActivityButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_activity.png", "Actividad");
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

    private void ConfigureActionIcons()
    {
        foreach (var button in VisualTreeTraversalService.FindChildren<WpfButton>(this))
        {
            if (IsColorButton(button) || button.Content is not string label)
            {
                continue;
            }

            ButtonIconContentService.TrySetButtonIcon(button, label);
        }
    }

    private static void SetButtonIcon(WpfButton button, string label, string iconKey)
    {
        ButtonIconContentService.SetButtonIcon(button, label, iconKey);
    }
}
