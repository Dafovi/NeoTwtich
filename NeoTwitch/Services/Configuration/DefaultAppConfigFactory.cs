using System.Collections.ObjectModel;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Configuration;

public static class DefaultAppConfigFactory
{
    public static AppConfig Create()
    {
        return new AppConfig
        {
            LedStrips =
            [
                new LedStripConfig
                {
                    Name = "Arduino Tira led ws2812b",
                    Pin = 6,
                    LedCount = 30
                }
            ],
            Rules =
            [
                CreateRule(
                    name: "Seguidor",
                    eventKind: TwitchEventKind.Follow,
                    chatMessageTemplate: "Gracias @{user}!",
                    pattern: LightPattern.Pulse,
                    primaryColor: "#FF2D55",
                    secondaryColor: "#00D1FF",
                    tertiaryColor: "#FFFFFF",
                    brightness: 150,
                    durationMs: 4500,
                    cycleMs: 70,
                    stepMs: 120),
                CreateRule(
                    name: "Suscripcion",
                    eventKind: TwitchEventKind.Subscription,
                    chatMessageTemplate: "Gracias por la suscripcion @{user}!",
                    pattern: LightPattern.Rainbow,
                    primaryColor: "#7C3AED",
                    secondaryColor: "#22C55E",
                    tertiaryColor: "#FFFFFF",
                    brightness: 160,
                    durationMs: 6500,
                    cycleMs: 45,
                    stepMs: 120),
                CreateRule(
                    name: "Raid",
                    eventKind: TwitchEventKind.Raid,
                    chatMessageTemplate: "Gracias por la raid @{user}!",
                    pattern: LightPattern.Chase,
                    primaryColor: "#F97316",
                    secondaryColor: "#14B8A6",
                    tertiaryColor: "#FFFFFF",
                    brightness: 180,
                    durationMs: 8000,
                    cycleMs: 55,
                    stepMs: 120),
                CreateRule(
                    name: "Bits",
                    eventKind: TwitchEventKind.Cheer,
                    chatMessageTemplate: "Gracias por esos {bits} bits @{user}!",
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
                    name: "Comando chat",
                    eventKind: TwitchEventKind.ChatCommand,
                    chatMessageTemplate: "@{user} activo {message}",
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
                    name: "Canje personalizado",
                    eventKind: TwitchEventKind.ChannelPointRedemption,
                    chatMessageTemplate: "Gracias por el canje @{user}!",
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
