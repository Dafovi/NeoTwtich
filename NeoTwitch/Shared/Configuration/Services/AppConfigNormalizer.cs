using System.Collections.ObjectModel;
using System.IO;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Configuration;

public static class AppConfigNormalizer
{
    public static AppConfig Normalize(AppConfig config, IUiTextService text, Func<string>? idFactory = null)
    {
        var createId = idFactory ?? (() => Guid.NewGuid().ToString("N"));
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
        config.AudioGroups = LibraryConfigNormalizer.NormalizeGroups(config.AudioGroups, text.Get(UiTextKeys.ConfigurationFallbackAudioGroupName), createId);
        config.AudioLibrary = LibraryConfigNormalizer.NormalizeAssets(config.AudioLibrary, audio => audio.DurationMs = audio.DurationMs, createId);
        config.ImageGroups = LibraryConfigNormalizer.NormalizeGroups(config.ImageGroups, text.Get(UiTextKeys.ConfigurationFallbackImageGroupName), createId);
        config.ImageLibrary = LibraryConfigNormalizer.NormalizeAssets(config.ImageLibrary, asset =>
        {
            asset.DurationMs = asset.DurationMs;
            asset.Width = asset.Width;
            asset.Height = asset.Height;
        }, createId);
        config.VideoGroups = LibraryConfigNormalizer.NormalizeGroups(config.VideoGroups, text.Get(UiTextKeys.ConfigurationFallbackVideoGroupName), createId);
        config.VideoLibrary = LibraryConfigNormalizer.NormalizeAssets(config.VideoLibrary, asset =>
        {
            asset.DurationMs = asset.DurationMs;
            asset.Width = asset.Width;
            asset.Height = asset.Height;
        }, createId);
        config.RecentColors = NormalizeRecentColors(config.RecentColors);
        config.MaxQueuedSameRuleAlerts = Math.Clamp(config.MaxQueuedSameRuleAlerts, 0, ApplicationLimits.MaxQueuedAlerts);
        config.SameRuleQueueCooldownMs = Math.Clamp(config.SameRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs);
        config.MaxQueuedDifferentRuleAlerts = Math.Clamp(config.MaxQueuedDifferentRuleAlerts, 0, ApplicationLimits.MaxQueuedAlerts);
        config.DifferentRuleQueueCooldownMs = Math.Clamp(config.DifferentRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs);
        config.Rules = NormalizeRules(config.Rules, text.Get(UiTextKeys.ConfigurationFallbackRuleName), createId);
        MigrateRuleAudioLibrary(config, createId);
        var defaults = DefaultAppConfigFactory.Create(text);
        config.LedStrips = NormalizeStrips(config.LedStrips, defaults.LedStrips, text.Get(UiTextKeys.ConfigurationFallbackLedStripName), createId);
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

    private static ObservableCollection<EventRule> NormalizeRules(
        ObservableCollection<EventRule>? rules,
        string fallbackName,
        Func<string> idFactory)
    {
        if (rules is null || rules.Count == 0)
        {
            return [];
        }

        foreach (var rule in rules)
        {
            rule.Id = string.IsNullOrWhiteSpace(rule.Id) ? idFactory() : rule.Id;
            rule.Name = string.IsNullOrWhiteSpace(rule.Name)
                ? fallbackName
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
            MigrateLegacyObsMedia(rule);
            if (rule.UseVirtualLights && !rule.VirtualLightsToObs && !rule.VirtualLightsToScreen)
            {
                rule.VirtualLightsToObs = true;
            }

            rule.VirtualLightsScreenId ??= "";
            rule.VirtualLightsPattern = Enum.IsDefined(rule.VirtualLightsPattern) ? rule.VirtualLightsPattern : LightPattern.Pulse;
            rule.VirtualLightsPrimaryColor = LightCommand.NormalizeColor(string.IsNullOrWhiteSpace(rule.VirtualLightsPrimaryColor)
                ? "#14B8A6"
                : rule.VirtualLightsPrimaryColor);
            rule.VirtualLightsSecondaryColor = LightCommand.NormalizeColor(string.IsNullOrWhiteSpace(rule.VirtualLightsSecondaryColor)
                ? "#B56CFF"
                : rule.VirtualLightsSecondaryColor);
            rule.VirtualLightsTertiaryColor = LightCommand.NormalizeColor(string.IsNullOrWhiteSpace(rule.VirtualLightsTertiaryColor)
                ? "#FFFFFF"
                : rule.VirtualLightsTertiaryColor);
            rule.VirtualLightsBrightness = Math.Clamp(rule.VirtualLightsBrightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness);
            rule.VirtualLightsDurationMs = Math.Clamp(rule.VirtualLightsDurationMs, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs);
            rule.VirtualLightsCycleMs = Math.Clamp(rule.VirtualLightsCycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs);
            rule.VirtualLightsStepMs = Math.Clamp(rule.VirtualLightsStepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs);
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

    private static void MigrateLegacyObsMedia(EventRule rule)
    {
        rule.ObsMediaAssetId ??= "";
        rule.ObsMediaGroupId ??= "";
        rule.ObsImageAssetId ??= "";
        rule.ObsImageGroupId ??= "";
        rule.ObsVideoAssetId ??= "";
        rule.ObsVideoGroupId ??= "";

        if (!rule.SendObsMedia)
        {
            return;
        }

        if (rule.ObsMediaKind == ObsMediaKind.Image)
        {
            rule.SendObsImage = true;
            rule.ObsImageSourceMode = rule.ObsMediaSourceMode;
            if (string.IsNullOrWhiteSpace(rule.ObsImageAssetId))
            {
                rule.ObsImageAssetId = rule.ObsMediaAssetId;
            }

            if (string.IsNullOrWhiteSpace(rule.ObsImageGroupId))
            {
                rule.ObsImageGroupId = rule.ObsMediaGroupId;
            }

            if (rule.ObsImageDurationMs <= 0)
            {
                rule.ObsImageDurationMs = rule.ObsMediaDurationMs;
            }
        }
        else if (rule.ObsMediaKind == ObsMediaKind.Video)
        {
            rule.SendObsVideo = true;
            rule.ObsVideoSourceMode = rule.ObsMediaSourceMode;
            if (string.IsNullOrWhiteSpace(rule.ObsVideoAssetId))
            {
                rule.ObsVideoAssetId = rule.ObsMediaAssetId;
            }

            if (string.IsNullOrWhiteSpace(rule.ObsVideoGroupId))
            {
                rule.ObsVideoGroupId = rule.ObsMediaGroupId;
            }
        }

        rule.SendObsMedia = false;
        rule.ObsMediaAssetId = "";
        rule.ObsMediaGroupId = "";
    }

    private static void MigrateRuleAudioLibrary(AppConfig config, Func<string> idFactory)
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
                    Id = idFactory(),
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
        ObservableCollection<LedStripConfig> defaults,
        string fallbackName,
        Func<string> idFactory)
    {
        if (strips is null || strips.Count == 0)
        {
            return defaults;
        }

        foreach (var strip in strips)
        {
            strip.Id = string.IsNullOrWhiteSpace(strip.Id) ? idFactory() : strip.Id;
            strip.Name = string.IsNullOrWhiteSpace(strip.Name) ? fallbackName : strip.Name;
            strip.Pin = strip.Pin;
            strip.LedCount = strip.LedCount;
        }

        return strips;
    }
}
