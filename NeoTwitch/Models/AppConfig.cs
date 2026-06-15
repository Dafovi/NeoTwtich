using System.Collections.ObjectModel;

namespace NeoTwitch.Models;

public sealed class AppConfig
{
    public string TwitchClientId { get; set; } = "";
    public string TwitchClientSecret { get; set; } = "";
    public TwitchTokenInfo Token { get; set; } = new();
    public TwitchChannelInfo Channel { get; set; } = new();
    public AlexaIntegrationConfig Alexa { get; set; } = new();
    public ObsIntegrationConfig Obs { get; set; } = new();
    public ObservableCollection<EventRule> Rules { get; set; } = [];
    public string SerialPort { get; set; } = "";
    public ObservableCollection<LedStripConfig> LedStrips { get; set; } = [];
    public int BaudRate { get; set; } = 115200;
    public bool ArduinoEnabled { get; set; }
    public bool AutoConnectTwitch { get; set; } = true;
    public bool AutoConnectArduino { get; set; }
    public bool StartHidden { get; set; }
    public bool StartWithWindows { get; set; }
    public string ThemeMode { get; set; } = "System";
    public bool DarkMode { get; set; }
    public bool CloseToTray { get; set; } = true;
    public int AlertVolumePercent { get; set; } = 100;
    public int VideoVolumePercent { get; set; } = 100;
    public ObservableCollection<AudioAssetConfig> AudioLibrary { get; set; } = [];
    public ObservableCollection<AudioGroupConfig> AudioGroups { get; set; } = [];
    public ObservableCollection<MediaAssetConfig> ImageLibrary { get; set; } = [];
    public ObservableCollection<MediaGroupConfig> ImageGroups { get; set; } = [];
    public ObservableCollection<MediaAssetConfig> VideoLibrary { get; set; } = [];
    public ObservableCollection<MediaGroupConfig> VideoGroups { get; set; } = [];
    public int MaxQueuedSameRuleAlerts { get; set; } = 1;
    public int SameRuleQueueCooldownMs { get; set; }
    public int MaxQueuedDifferentRuleAlerts { get; set; } = 3;
    public int DifferentRuleQueueCooldownMs { get; set; }
    public bool BackgroundEnabled { get; set; }
    public bool BackgroundAlexaEnabled { get; set; }
    public bool BackgroundAlexaTurnOffAfterEvent { get; set; }
    public string BackgroundAlexaOnEventName { get; set; } = "luz_encendida";
    public string BackgroundAlexaOffEventName { get; set; } = "luz_apagada";
    public string BackgroundTargetPins { get; set; } = "";
    public LightPattern BackgroundPattern { get; set; } = LightPattern.Solid;
    public string BackgroundPrimaryColor { get; set; } = "#141414";
    public string BackgroundSecondaryColor { get; set; } = "#216869";
    public string BackgroundTertiaryColor { get; set; } = "#FACC15";
    public int BackgroundBrightness { get; set; } = 40;
    public int BackgroundCycleMs { get; set; } = 120;
    public int BackgroundStepMs { get; set; } = 400;

    public static AppConfig CreateDefault()
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
                new EventRule
                {
                    Name = "Seguidor",
                    EventKind = TwitchEventKind.Follow,
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "Gracias @{user}!",
                    Pattern = LightPattern.Pulse,
                    TargetPins = "",
                    PrimaryColor = "#FF2D55",
                    SecondaryColor = "#00D1FF",
                    TertiaryColor = "#FFFFFF",
                    Brightness = 150,
                    DurationMs = 4500,
                    CycleMs = 70,
                    StepMs = 120
                },
                new EventRule
                {
                    Name = "Suscripcion",
                    EventKind = TwitchEventKind.Subscription,
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "Gracias por la suscripcion @{user}!",
                    Pattern = LightPattern.Rainbow,
                    TargetPins = "",
                    PrimaryColor = "#7C3AED",
                    SecondaryColor = "#22C55E",
                    TertiaryColor = "#FFFFFF",
                    Brightness = 160,
                    DurationMs = 6500,
                    CycleMs = 45,
                    StepMs = 120
                },
                new EventRule
                {
                    Name = "Raid",
                    EventKind = TwitchEventKind.Raid,
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "Gracias por la raid @{user}!",
                    Pattern = LightPattern.Chase,
                    TargetPins = "",
                    PrimaryColor = "#F97316",
                    SecondaryColor = "#14B8A6",
                    TertiaryColor = "#FFFFFF",
                    Brightness = 180,
                    DurationMs = 8000,
                    CycleMs = 55,
                    StepMs = 120
                },
                new EventRule
                {
                    Name = "Bits",
                    EventKind = TwitchEventKind.Cheer,
                    MinimumBits = 1,
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "Gracias por esos {bits} bits @{user}!",
                    Pattern = LightPattern.Rave,
                    TargetPins = "",
                    PrimaryColor = "#FACC15",
                    SecondaryColor = "#EC4899",
                    TertiaryColor = "#00D1FF",
                    Brightness = 170,
                    DurationMs = 4500,
                    CycleMs = 45,
                    StepMs = 80
                },
                new EventRule
                {
                    Name = "Comando chat",
                    EventKind = TwitchEventKind.ChatCommand,
                    ChatCommand = "!baile",
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "@{user} activo {message}",
                    Pattern = LightPattern.Rave,
                    TargetPins = "",
                    PrimaryColor = "#FF2D55",
                    SecondaryColor = "#00D1FF",
                    TertiaryColor = "#FFFFFF",
                    Brightness = 170,
                    DurationMs = 4500,
                    CycleMs = 45,
                    StepMs = 80
                },
                new EventRule
                {
                    Name = "Canje personalizado",
                    EventKind = TwitchEventKind.ChannelPointRedemption,
                    CustomRewardTitle = "",
                    UseLights = false,
                    PlayAudio = false,
                    SendChatMessage = false,
                    ChatMessageTemplate = "Gracias por el canje @{user}!",
                    Pattern = LightPattern.Sparkle,
                    TargetPins = "",
                    PrimaryColor = "#FACC15",
                    SecondaryColor = "#EC4899",
                    TertiaryColor = "#FFFFFF",
                    Brightness = 150,
                    DurationMs = 5500,
                    CycleMs = 80,
                    StepMs = 120
                }
            ]
        };
    }
}
