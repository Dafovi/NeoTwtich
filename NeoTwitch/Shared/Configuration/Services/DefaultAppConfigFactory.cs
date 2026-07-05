using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Configuration;

public static class DefaultAppConfigFactory
{
    public static AppConfig Create(IUiTextService text)
    {
        return new AppConfig
        {
            LedStrips =
            [
                new LedStripConfig
                {
                    Name = text.Get(UiTextKeys.ConfigurationDefaultLedStripName),
                    Pin = 6,
                    LedCount = 30
                }
            ],
            Rules =
            [
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultFollowRuleName),
                    eventKind: TwitchEventKind.Follow,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultFollowChatTemplate),
                    pattern: LightPattern.Pulse,
                    primaryColor: "#FF2D55",
                    secondaryColor: "#00D1FF",
                    tertiaryColor: "#FFFFFF",
                    brightness: 150,
                    durationMs: 4500,
                    cycleMs: 70,
                    stepMs: 120),
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultSubscriptionRuleName),
                    eventKind: TwitchEventKind.Subscription,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultSubscriptionChatTemplate),
                    pattern: LightPattern.Rainbow,
                    primaryColor: "#7C3AED",
                    secondaryColor: "#22C55E",
                    tertiaryColor: "#FFFFFF",
                    brightness: 160,
                    durationMs: 6500,
                    cycleMs: 45,
                    stepMs: 120),
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultRaidRuleName),
                    eventKind: TwitchEventKind.Raid,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultRaidChatTemplate),
                    pattern: LightPattern.Chase,
                    primaryColor: "#F97316",
                    secondaryColor: "#14B8A6",
                    tertiaryColor: "#FFFFFF",
                    brightness: 180,
                    durationMs: 8000,
                    cycleMs: 55,
                    stepMs: 120),
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultBitsRuleName),
                    eventKind: TwitchEventKind.Cheer,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultBitsChatTemplate),
                    pattern: LightPattern.Rave,
                    primaryColor: "#FACC15",
                    secondaryColor: "#EC4899",
                    tertiaryColor: "#00D1FF",
                    brightness: 170,
                    durationMs: 4500,
                    cycleMs: 45,
                    stepMs: 80,
                    minimumBits: 1),
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultChatCommandRuleName),
                    eventKind: TwitchEventKind.ChatCommand,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultChatCommandTemplate),
                    pattern: LightPattern.Rave,
                    primaryColor: "#FF2D55",
                    secondaryColor: "#00D1FF",
                    tertiaryColor: "#FFFFFF",
                    brightness: 170,
                    durationMs: 4500,
                    cycleMs: 45,
                    stepMs: 80,
                    chatCommand: "!baile"),
                CreateRule(
                    name: text.Get(UiTextKeys.ConfigurationDefaultRedemptionRuleName),
                    eventKind: TwitchEventKind.ChannelPointRedemption,
                    chatMessageTemplate: text.Get(UiTextKeys.ConfigurationDefaultRedemptionChatTemplate),
                    pattern: LightPattern.Sparkle,
                    primaryColor: "#FACC15",
                    secondaryColor: "#EC4899",
                    tertiaryColor: "#FFFFFF",
                    brightness: 150,
                    durationMs: 5500,
                    cycleMs: 80,
                    stepMs: 120)
            ]
        };
    }

    private static EventRule CreateRule(
        string name,
        TwitchEventKind eventKind,
        string chatMessageTemplate,
        LightPattern pattern,
        string primaryColor,
        string secondaryColor,
        string tertiaryColor,
        int brightness,
        int durationMs,
        int cycleMs,
        int stepMs,
        int minimumBits = 1,
        string customRewardTitle = "",
        string chatCommand = "")
    {
        return new EventRule
        {
            Name = name,
            IsEnabled = true,
            EventKind = eventKind,
            CustomRewardTitle = customRewardTitle,
            ChatCommand = chatCommand,
            MinimumBits = minimumBits,
            UseLights = false,
            PlayAudio = false,
            SendChatMessage = false,
            ChatMessageTemplate = chatMessageTemplate,
            Pattern = pattern,
            TargetPins = "",
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            TertiaryColor = tertiaryColor,
            Brightness = brightness,
            DurationMs = durationMs,
            CycleMs = cycleMs,
            StepMs = stepMs
        };
    }
}
