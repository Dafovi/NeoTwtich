using System.Collections.ObjectModel;

namespace NeoTwitch.Models;

public sealed class AppConfig
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public ProtectedConfigurationSecrets ProtectedSecrets { get; set; } = new();
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
    public ObservableCollection<string> RecentColors { get; set; } = [];
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
}

public sealed class ProtectedConfigurationSecrets
{
    public string TwitchClientSecret { get; set; } = "";
    public string TwitchAccessToken { get; set; } = "";
    public string TwitchRefreshToken { get; set; } = "";
    public string AlexaAuthToken { get; set; } = "";
    public string ObsPassword { get; set; } = "";
}
