using System.Collections.ObjectModel;

namespace LucesCanjeTwitch.Models;

public sealed class AppConfig
{
    public string TwitchClientId { get; set; } = "";
    public TwitchTokenInfo Token { get; set; } = new();
    public TwitchChannelInfo Channel { get; set; } = new();
    public ObservableCollection<EventRule> Rules { get; set; } = [];
    public string SerialPort { get; set; } = "";
    public ObservableCollection<LedStripConfig> LedStrips { get; set; } = [];
    public int BaudRate { get; set; } = 115200;
    public bool AutoConnectTwitch { get; set; } = true;
    public bool AutoConnectArduino { get; set; } = true;
    public bool StartHidden { get; set; }
    public bool DarkMode { get; set; }
    public bool BackgroundEnabled { get; set; }
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
                    Name = "Principal",
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
                    UseLights = true,
                    PlayAudio = false,
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
                    UseLights = true,
                    PlayAudio = true,
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
                    UseLights = true,
                    PlayAudio = true,
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
                    Name = "Canje personalizado",
                    EventKind = TwitchEventKind.ChannelPointRedemption,
                    CustomRewardTitle = "",
                    UseLights = true,
                    PlayAudio = true,
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
