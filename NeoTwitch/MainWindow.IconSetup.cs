using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using static NeoTwitch.Services.Ui.ThemeElementClassifier;
using WpfButton = System.Windows.Controls.Button;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ConfigureNavigationIcons()
    {
        NavSettingsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_panel.png", _text.Get(UiTextKeys.NavPanel));
        NavConnectionsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_connections.png", _text.Get(UiTextKeys.NavConnections));
        NavRulesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_rules.png", _text.Get(UiTextKeys.NavAlerts));
        NavStripsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_lights.png", _text.Get(UiTextKeys.NavLights));
        NavAlexaButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_alexa.png", _text.Get(UiTextKeys.NavAlexa));
        NavAudioButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_audio.png", _text.Get(UiTextKeys.NavAudio));
        NavImagesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_images.png", _text.Get(UiTextKeys.NavImages));
        NavVideosButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_videos.png", _text.Get(UiTextKeys.NavVideos));
        NavObsButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_obs.png", _text.Get(UiTextKeys.NavObs));
        NavPreferencesButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_settings.png", _text.Get(UiTextKeys.NavConfiguration));
        NavActivityButton.Content = ButtonIconContentService.CreateNavigationItem("Assets/Icons/nav_activity.png", _text.Get(UiTextKeys.NavActivity));
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
