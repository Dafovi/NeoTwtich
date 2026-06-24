namespace NeoTwitch.Models;

public static class ApplicationLimits
{
    public const int MaxSettingsBackups = 20;
    public const int MaxRecentColors = 7;
    public const int RulePreviewLedDots = 24;

    public const int MinNetworkPort = 1;
    public const int MaxNetworkPort = 65535;
    public const int MinBaudRate = 300;
    public const int MaxBaudRate = 921600;
    public const int MinVolumePercent = 0;
    public const int MaxVolumePercent = 100;
    public const int MaxQueuedAlerts = 100;

    public const int MinArduinoPin = 0;
    public const int MaxArduinoPin = 53;
    public const int MinLedCount = 1;
    public const int MaxLedCount = 600;

    public const int MaxAudioDurationMs = 3_600_000;
    public const int MaxMediaDurationMs = 86_400_000;
    public const int MaxMediaDimensionPx = 100_000;
    public const int MinObsOverlayMediaSize = 32;
    public const int ObsOverlayPollMs = 250;

    public const int MinAlertDurationMs = 250;
    public const int MaxAlertDurationMs = 600000;
    public const int MaxLegacyAlertDurationMs = 60000;
    public const int MinCycleMs = 10;
    public const int MaxCycleMs = 2000;
    public const int MinStepMs = 10;
    public const int MaxStepMs = 5000;
    public const int MinBrightness = 0;
    public const int MaxBrightness = 255;
}
