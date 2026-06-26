using NeoTwitch.Models;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch.Services.Configuration;

public sealed record GlobalSettingsFormValues(
    string TwitchClientId,
    string TwitchClientSecret,
    string SerialPort,
    string BaudRate,
    bool ArduinoEnabled,
    bool AutoConnectTwitch,
    bool AutoConnectArduino,
    bool StartHidden,
    bool StartWithWindows,
    string ThemeMode,
    bool CloseToTray,
    double AlertVolumePercent,
    double VideoVolumePercent,
    string MaxQueuedSameRuleAlerts,
    string SameRuleQueueCooldownMs,
    string MaxQueuedDifferentRuleAlerts,
    string DifferentRuleQueueCooldownMs,
    bool AlexaEnabled,
    string AlexaRelayUrl,
    string AlexaAuthToken,
    bool ObsEnabled,
    string ObsHost,
    string ObsPort,
    string ObsPassword,
    bool ObsAutoReconnect,
    string ObsOverlayWidth,
    string ObsOverlayHeight,
    string ObsOverlayMediaWidth,
    string ObsOverlayMediaHeight,
    string ObsOverlayPositionMode,
    string ObsOverlayX,
    string ObsOverlayY);

public static class GlobalSettingsFormService
{
    public static void Apply(AppConfig config, GlobalSettingsFormValues values)
    {
        config.TwitchClientId = values.TwitchClientId.Trim();
        config.TwitchClientSecret = values.TwitchClientSecret.Trim();
        config.SerialPort = ParsePort(values.SerialPort);
        config.BaudRate = ParseInt(values.BaudRate, 115200, 300, 921600);
        config.ArduinoEnabled = values.ArduinoEnabled;
        config.AutoConnectTwitch = values.AutoConnectTwitch;
        config.AutoConnectArduino = values.AutoConnectArduino;
        config.StartHidden = values.StartHidden;
        config.StartWithWindows = values.StartWithWindows;
        config.ThemeMode = ThemeModeService.Normalize(values.ThemeMode);
        config.DarkMode = ThemeModeService.ResolveDarkMode(config.ThemeMode);
        config.CloseToTray = values.CloseToTray;
        config.AlertVolumePercent = (int)Math.Round(values.AlertVolumePercent);
        config.VideoVolumePercent = (int)Math.Round(values.VideoVolumePercent);
        config.MaxQueuedSameRuleAlerts = ParseInt(values.MaxQueuedSameRuleAlerts, 1, 0, 100);
        config.SameRuleQueueCooldownMs = ParseInt(values.SameRuleQueueCooldownMs, 0, 0, 600000);
        config.MaxQueuedDifferentRuleAlerts = ParseInt(values.MaxQueuedDifferentRuleAlerts, 3, 0, 100);
        config.DifferentRuleQueueCooldownMs = ParseInt(values.DifferentRuleQueueCooldownMs, 0, 0, 600000);
        config.Alexa.Enabled = values.AlexaEnabled;
        config.Alexa.RelayUrl = values.AlexaRelayUrl.Trim();
        config.Alexa.AuthToken = values.AlexaAuthToken.Trim();
        config.Obs.Enabled = values.ObsEnabled;
        config.Obs.Host = string.IsNullOrWhiteSpace(values.ObsHost) ? "127.0.0.1" : values.ObsHost.Trim();
        config.Obs.Port = ParseInt(values.ObsPort, 4455, 1, 65535);
        config.Obs.Password = values.ObsPassword;
        config.Obs.AutoReconnect = values.ObsAutoReconnect;
        config.Obs.OverlayWidth = ParseInt(values.ObsOverlayWidth, 1920, 320, 7680);
        config.Obs.OverlayHeight = ParseInt(values.ObsOverlayHeight, 1080, 180, 4320);
        config.Obs.OverlayMediaWidth = ParseInt(values.ObsOverlayMediaWidth, 720, 32, 7680);
        config.Obs.OverlayMediaHeight = ParseInt(values.ObsOverlayMediaHeight, 420, 32, 4320);
        config.Obs.OverlayPositionMode = string.IsNullOrWhiteSpace(values.ObsOverlayPositionMode)
            ? "Center"
            : values.ObsOverlayPositionMode;
        config.Obs.OverlayX = ParseInt(values.ObsOverlayX, 0, 0, 7680);
        config.Obs.OverlayY = ParseInt(values.ObsOverlayY, 0, 0, 4320);
    }
}
