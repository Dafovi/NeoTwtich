using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Services.Alerts;

public static class EventRulePresentationService
{
    public static string BuildDisplayLabel(EventRule rule, IUiTextService text)
    {
        var eventName = DisplayNameService.For(rule.EventKind, text);
        var label = string.IsNullOrWhiteSpace(rule.Name)
            ? eventName
            : $"{rule.Name} - {eventName}";

        return rule.EventKind switch
        {
            TwitchEventKind.Cheer => $"{label} >= {rule.MinimumBits} bits",
            TwitchEventKind.ChatCommand when !string.IsNullOrWhiteSpace(rule.ChatCommand) => $"{label} ({rule.ChatCommand})",
            _ => label
        };
    }

    public static string BuildStatusText(EventRule rule, IUiTextService text)
    {
        return rule.IsEnabled
            ? text.Get(UiTextKeys.RuleRowStatusActive)
            : text.Get(UiTextKeys.RuleRowStatusInactive);
    }

    public static string BuildStatusColor(EventRule rule)
    {
        return rule.IsEnabled ? "#22C55E" : UiAccentCatalog.Neutral;
    }

    public static string BuildEventIconPath(EventRule rule)
    {
        return rule.EventKind switch
        {
            TwitchEventKind.Follow => "/Assets/Icons/action_follower_teal.png",
            TwitchEventKind.Subscription => "/Assets/Icons/action_subscription_purple.png",
            TwitchEventKind.Cheer => "/Assets/Icons/action_bits_blue.png",
            TwitchEventKind.ChatCommand => "/Assets/Icons/action_message_green.png",
            TwitchEventKind.ChannelPointRedemption => "/Assets/Icons/activity_notification_lime.png",
            TwitchEventKind.Raid => "/Assets/Icons/activity_notification.png",
            _ => "/Assets/Icons/nav_rules.png"
        };
    }

    public static string BuildEventAccentColor(EventRule rule)
    {
        return UiAccentCatalog.ForEventKind(rule.EventKind);
    }

    public static string BuildActionsSummary(EventRule rule, IUiTextService text)
    {
        List<string> actions = [];
        if (rule.UseLights)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionLights));
        }

        if (rule.UseVirtualLights)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionVirtualLights));
        }

        if (rule.SendChatMessage)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionChat));
        }

        if (rule.PlayAudio)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionAudio));
        }

        if (rule.SendObsVideo)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionVideo));
        }

        if (rule.SendObsImage)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionImage));
        }

        if (rule.SendObsScene)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionObs));
        }

        if (rule.SendAlexaEvent)
        {
            actions.Add(text.Get(UiTextKeys.RuleRowActionAlexa));
        }

        return actions.Count == 0
            ? text.Get(UiTextKeys.RuleRowNoActions)
            : string.Join(" / ", actions);
    }

    public static string BuildLightsToolTip(EventRule rule, IUiTextService text)
    {
        return rule.LightsActionAvailable
            ? text.Get(UiTextKeys.RuleRowLightsActive)
            : text.Get(UiTextKeys.RuleRowLightsUnavailable);
    }

    public static string BuildVirtualLightsToolTip(EventRule rule, IUiTextService text)
    {
        return rule.VirtualLightsToObs || rule.VirtualLightsToScreen
            ? text.Get(UiTextKeys.RuleRowVirtualLightsActive)
            : text.Get(UiTextKeys.RuleRowVirtualLightsUnavailable);
    }

    public static string BuildAlexaToolTip(EventRule rule, IUiTextService text)
    {
        return rule.AlexaActionAvailable
            ? text.Get(UiTextKeys.RuleRowAlexaActive)
            : text.Get(UiTextKeys.RuleRowAlexaUnavailable);
    }

    public static string BuildObsToolTip(EventRule rule, IUiTextService text)
    {
        return rule.ObsActionAvailable
            ? text.Get(UiTextKeys.RuleRowObsActive)
            : text.Get(UiTextKeys.RuleRowObsUnavailable);
    }
}
