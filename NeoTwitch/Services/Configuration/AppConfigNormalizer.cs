using System.Collections.ObjectModel;
using System.IO;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;

namespace NeoTwitch.Services.Configuration;

public static class AppConfigNormalizer
{
    public static AppConfig Normalize(AppConfig config)
    {
        config.TwitchClientId ??= "";
        config.TwitchClientSecret ??= "";
        config.Token ??= new TwitchTokenInfo();
        config.Token.AccessToken ??= "";
        config.Token.RefreshToken ??= "";
        config.Token.Scopes ??= [];
        config.Channel ??= new TwitchChannelInfo();
        config.Channel.UserId ??= "";
        config.Channel.Login ??= "";
        config.Channel.DisplayName ??= "";
        config.Channel.ProfileImageUrl ??= "";
        config.Alexa ??= new AlexaIntegrationConfig();
        config.Alexa.RelayUrl ??= "";
        config.Alexa.AuthToken ??= "";
        config.Obs ??= new ObsIntegrationConfig();
        config.Obs.Host = string.IsNullOrWhiteSpace(config.Obs.Host) ? "127.0.0.1" : config.Obs.Host.Trim();
        config.Obs.Password ??= "";
        config.Obs.Port = Math.Clamp(config.Obs.Port, ApplicationLimits.MinNetworkPort, ApplicationLimits.MaxNetworkPort);
        config.BackgroundAlexaOnEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOnEventName, "luz_encendida");
        config.BackgroundAlexaOffEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOffEventName, "luz_apagada");
        config.SerialPort ??= "";
        config.ThemeMode = ThemeModeService.Normalize(config.ThemeMode);
        config.BaudRate = Math.Clamp(config.BaudRate, ApplicationLimits.MinBaudRate, ApplicationLimits.MaxBaudRate);
        config.AlertVolumePercent = Math.Clamp(config.AlertVolumePercent, ApplicationLimits.MinVolumePercent, ApplicationLimits.MaxVolumePercent);
        config.VideoVolumePercent = Math.Clamp(config.VideoVolumePercent, ApplicationLimits.MinVolumePercent, ApplicationLimits.MaxVolumePercent);
        config.AudioGroups = LibraryConfigNormalizer.NormalizeGroups(config.AudioGroups, "Grupo de audio");
        config.AudioLibrary = LibraryConfigNormalizer.NormalizeAssets(config.AudioLibrary, audio => audio.DurationMs = audio.DurationMs);
        config.ImageGroups = LibraryConfigNormalizer.NormalizeGroups(config.ImageGroups, "Grupo de imagenes");
        config.ImageLibrary = LibraryConfigNormalizer.NormalizeAssets(config.ImageLibrary, asset =>
        {
            asset.DurationMs = asset.DurationMs;
            asset.Width = asset.Width;
            asset.Height = asset.Height;
        });
        config.VideoGroups = LibraryConfigNormalizer.NormalizeGroups(config.VideoGroups, "Grupo de videos");
        config.VideoLibrary = LibraryConfigNormalizer.NormalizeAssets(config.VideoLibrary, asset =>
        {
            asset.DurationMs = asset.DurationMs;
            asset.Width = asset.Width;
            asset.Height = asset.Height;
        });
        config.RecentColors = NormalizeRecentColors(config.RecentColors);
        config.MaxQueuedSameRuleAlerts = Math.Clamp(config.MaxQueuedSameRuleAlerts, 0, ApplicationLimits.MaxQueuedAlerts);
        config.SameRuleQueueCooldownMs = Math.Clamp(config.SameRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs);
        config.MaxQueuedDifferentRuleAlerts = Math.Clamp(config.MaxQueuedDifferentRuleAlerts, 0, ApplicationLimits.MaxQueuedAlerts);
        config.DifferentRuleQueueCooldownMs = Math.Clamp(config.DifferentRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs);
        config.Rules = NormalizeRules(config.Rules);
        MigrateRuleAudioLibrary(config);
        var defaults = DefaultAppConfigFactory.Create();
        config.LedStrips = NormalizeStrips(config.LedStrips, defaults.LedStrips);
        config.BackgroundTargetPins ??= "";
        config.BackgroundPattern = Enum.IsDefined(config.BackgroundPattern)
            ? config.BackgroundPattern
            : LightPattern.Solid;
        config.BackgroundPrimaryColor = LightCommand.NormalizeColor(config.BackgroundPrimaryColor);
        config.BackgroundSecondaryColor = LightCommand.NormalizeColor(config.BackgroundSecondaryColor);
        config.BackgroundTertiaryColor = LightCommand.NormalizeColor(config.BackgroundTertiaryColor);
        config.BackgroundBrightness = Math.Clamp(config.BackgroundBrightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness);
        config.BackgroundCycleMs = Math.Clamp(config.BackgroundCycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs);
        config.BackgroundStepMs = Math.Clamp(config.BackgroundStepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs);

        return config;
    }

    private static string NormalizeBackgroundAlexaEventName(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static ObservableCollection<string> NormalizeRecentColors(ObservableCollection<string>? colors)
    {
        return new ObservableCollection<string>(
            (colors ?? [])
            .Select(LightCommand.NormalizeColor)
            .Where(color => !string.Equals(color, "#000000", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ApplicationLimits.MaxRecentColors));
    }

    private static ObservableCollection<EventRule> NormalizeRules(ObservableCollection<EventRule>? rules)
    {
        if (rules is null || rules.Count == 0)
        {
            return [];
        }

        foreach (var rule in rules)
        {
            rule.Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id;
            rule.Name = string.IsNullOrWhiteSpace(rule.Name)
                ? "Alerta sin nombre"
                : rule.Name.Trim();

            rule.CustomRewardTitle ??= "";
            rule.ChatCommand ??= "";
            rule.AudioPath ??= "";
            rule.AudioAssetId ??= "";
            rule.AudioGroupId ??= "";
            rule.AudioSourceMode = Enum.IsDefined(rule.AudioSourceMode) ? rule.AudioSourceMode : AudioSourceMode.Single;
            rule.ChatMessageTemplate ??= "";
            rule.AlexaEventName ??= "";
            rule.TargetPins ??= "";
            rule.PrimaryColor = LightCommand.NormalizeColor(rule.PrimaryColor);
            rule.SecondaryColor = LightCommand.NormalizeColor(rule.SecondaryColor);
            rule.TertiaryColor = LightCommand.NormalizeColor(rule.TertiaryColor);
            rule.Pattern = Enum.IsDefined(rule.Pattern) ? rule.Pattern : LightPattern.Pulse;
            rule.EventKind = Enum.IsDefined(rule.EventKind) ? rule.EventKind : TwitchEventKind.Follow;
            rule.MinimumBits = rule.MinimumBits;
            rule.Brightness = rule.Brightness;
            rule.DurationMs = rule.DurationMs;
            rule.CycleMs = rule.CycleMs;
            rule.StepMs = rule.StepMs;
        }

        return rules;
    }

    private static void MigrateRuleAudioLibrary(AppConfig config)
    {
        foreach (var rule in config.Rules)
        {
            rule.AudioAssetId ??= "";
            rule.AudioGroupId ??= "";

            if (rule.AudioSourceMode == AudioSourceMode.Group)
            {
                var groupExists = config.AudioGroups.Any(group => string.Equals(group.Id, rule.AudioGroupId, StringComparison.OrdinalIgnoreCase));
                if (groupExists)
                {
                    continue;
                }

                rule.AudioSourceMode = AudioSourceMode.Single;
                rule.AudioGroupId = "";
            }

            if (!string.IsNullOrWhiteSpace(rule.AudioAssetId)
                && config.AudioLibrary.Any(audio => string.Equals(audio.Id, rule.AudioAssetId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.AudioPath))
            {
                rule.AudioAssetId = "";
                continue;
            }

            var existing = config.AudioLibrary.FirstOrDefault(audio =>
                string.Equals(audio.FilePath, rule.AudioPath, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new AudioAssetConfig
                {
                    Name = Path.GetFileNameWithoutExtension(rule.AudioPath),
                    FilePath = rule.AudioPath
                };
                config.AudioLibrary.Add(existing);
            }

            rule.AudioSourceMode = AudioSourceMode.Single;
            rule.AudioAssetId = existing.Id;
        }
    }

    private static ObservableCollection<LedStripConfig> NormalizeStrips(
        ObservableCollection<LedStripConfig>? strips,
        ObservableCollection<LedStripConfig> defaults)
    {
        if (strips is null || strips.Count == 0)
        {
            return defaults;
        }

        foreach (var strip in strips)
        {
            strip.Id = string.IsNullOrWhiteSpace(strip.Id) ? Guid.NewGuid().ToString("N") : strip.Id;
            strip.Name = string.IsNullOrWhiteSpace(strip.Name) ? "Tira LED" : strip.Name;
            strip.Pin = strip.Pin;
            strip.LedCount = strip.LedCount;
        }

        return strips;
    }
}
