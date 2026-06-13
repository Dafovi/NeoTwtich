using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class SettingsStore
{
    private const string AppFolderName = "NeoTwitch";
    private const string LegacyAppFolderName = "LucesCanjeTwitch";
    private const int MaxTimestampedBackups = 20;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private bool _createdSessionBackup;

    public SettingsStore()
    {
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public string SettingsPath { get; } = BuildSettingsPath(AppFolderName);

    public string BackupDirectory { get; } = BuildBackupDirectory(AppFolderName);

    private string LegacySettingsPath { get; } = BuildSettingsPath(LegacyAppFolderName);

    public string? LastLoadError { get; private set; }

    public AppConfig Load()
    {
        LastLoadError = null;

        var loadPath = ResolveSettingsPathForLoad();
        if (loadPath is null)
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(loadPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? AppConfig.CreateDefault();
            return NormalizeConfig(config);
        }
        catch (Exception ex)
        {
            LastLoadError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo leer la configuracion: {loadPath}");
            return AppConfig.CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        var tempPath = Path.Combine(directory, "settings.tmp");

        File.WriteAllText(tempPath, json);

        BackupCurrentSettings(directory);

        File.Copy(tempPath, SettingsPath, overwrite: true);
        File.Delete(tempPath);
    }

    public void Export(AppConfig config, string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(destinationPath, json);
    }

    public AppConfig Import(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("El archivo no contiene una configuracion valida.");
        var normalized = NormalizeConfig(config);
        Save(normalized);
        return normalized;
    }

    private static string BuildSettingsPath(string appFolderName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appFolderName,
            "settings.json");
    }

    private static string BuildBackupDirectory(string appFolderName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appFolderName,
            "backups");
    }

    private void BackupCurrentSettings(string directory)
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);

        var latestBackupPath = Path.Combine(directory, "settings.backup.json");
        File.Copy(SettingsPath, latestBackupPath, overwrite: true);

        if (_createdSessionBackup)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var timestampedBackupPath = Path.Combine(BackupDirectory, $"settings-{timestamp}.json");
        File.Copy(SettingsPath, timestampedBackupPath, overwrite: true);
        _createdSessionBackup = true;
        PruneTimestampedBackups();
    }

    private void PruneTimestampedBackups()
    {
        var backups = Directory.GetFiles(BackupDirectory, "settings-*.json")
            .OrderByDescending(File.GetCreationTimeUtc)
            .Skip(MaxTimestampedBackups)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                File.Delete(backup);
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, $"No se pudo borrar backup antiguo: {backup}");
            }
        }
    }

    private string? ResolveSettingsPathForLoad()
    {
        if (File.Exists(SettingsPath))
        {
            return SettingsPath;
        }

        return File.Exists(LegacySettingsPath)
            ? LegacySettingsPath
            : null;
    }

    private static AppConfig NormalizeConfig(AppConfig config)
    {
        var defaults = AppConfig.CreateDefault();

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
        config.Obs.Port = Math.Clamp(config.Obs.Port, 1, 65535);
        config.BackgroundAlexaOnEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOnEventName, "luz_encendida");
        config.BackgroundAlexaOffEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOffEventName, "luz_apagada");
        config.SerialPort ??= "";
        config.ThemeMode = NormalizeThemeMode(config.ThemeMode);
        config.BaudRate = Math.Clamp(config.BaudRate, 300, 921600);
        config.AlertVolumePercent = Math.Clamp(config.AlertVolumePercent, 0, 100);
        config.AudioGroups = NormalizeAudioGroups(config.AudioGroups);
        config.AudioLibrary = NormalizeAudioLibrary(config.AudioLibrary);
        config.ImageGroups = NormalizeMediaGroups(config.ImageGroups, "Grupo de imagenes");
        config.ImageLibrary = NormalizeMediaLibrary(config.ImageLibrary);
        config.VideoGroups = NormalizeMediaGroups(config.VideoGroups, "Grupo de videos");
        config.VideoLibrary = NormalizeMediaLibrary(config.VideoLibrary);
        config.MaxQueuedSameRuleAlerts = Math.Clamp(config.MaxQueuedSameRuleAlerts, 0, 100);
        config.SameRuleQueueCooldownMs = Math.Clamp(config.SameRuleQueueCooldownMs, 0, 600000);
        config.MaxQueuedDifferentRuleAlerts = Math.Clamp(config.MaxQueuedDifferentRuleAlerts, 0, 100);
        config.DifferentRuleQueueCooldownMs = Math.Clamp(config.DifferentRuleQueueCooldownMs, 0, 600000);
        config.Rules = NormalizeRules(config.Rules, defaults.Rules);
        MigrateRuleAudioLibrary(config);
        config.LedStrips = NormalizeStrips(config.LedStrips, defaults.LedStrips);
        config.BackgroundTargetPins ??= "";
        config.BackgroundPattern = Enum.IsDefined(config.BackgroundPattern)
            ? config.BackgroundPattern
            : LightPattern.Solid;
        config.BackgroundPrimaryColor = LightCommand.NormalizeColor(config.BackgroundPrimaryColor);
        config.BackgroundSecondaryColor = LightCommand.NormalizeColor(config.BackgroundSecondaryColor);
        config.BackgroundTertiaryColor = LightCommand.NormalizeColor(config.BackgroundTertiaryColor);
        config.BackgroundBrightness = Math.Clamp(config.BackgroundBrightness, 0, 255);
        config.BackgroundCycleMs = Math.Clamp(config.BackgroundCycleMs, 10, 2000);
        config.BackgroundStepMs = Math.Clamp(config.BackgroundStepMs, 10, 5000);

        return config;
    }

    private static string NormalizeBackgroundAlexaEventName(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeThemeMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };
    }

    private static ObservableCollection<EventRule> NormalizeRules(
        ObservableCollection<EventRule>? rules,
        ObservableCollection<EventRule> defaults)
    {
        if (rules is null || rules.Count == 0)
        {
            return defaults;
        }

        foreach (var rule in rules)
        {
            rule.Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id;
            rule.Name ??= "";
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

    private static ObservableCollection<AudioGroupConfig> NormalizeAudioGroups(ObservableCollection<AudioGroupConfig>? groups)
    {
        groups ??= [];

        foreach (var group in groups)
        {
            group.Id = string.IsNullOrWhiteSpace(group.Id) ? Guid.NewGuid().ToString("N") : group.Id;
            group.Name = string.IsNullOrWhiteSpace(group.Name) ? "Grupo de audio" : group.Name.Trim();
        }

        return groups;
    }

    private static ObservableCollection<AudioAssetConfig> NormalizeAudioLibrary(ObservableCollection<AudioAssetConfig>? library)
    {
        library ??= [];

        foreach (var audio in library)
        {
            audio.Id = string.IsNullOrWhiteSpace(audio.Id) ? Guid.NewGuid().ToString("N") : audio.Id;
            audio.Name = string.IsNullOrWhiteSpace(audio.Name) ? Path.GetFileNameWithoutExtension(audio.FilePath ?? "") : audio.Name.Trim();
            audio.FilePath ??= "";
            audio.GroupId ??= "";
            audio.DurationMs = audio.DurationMs;
        }

        return library;
    }

    private static ObservableCollection<MediaGroupConfig> NormalizeMediaGroups(
        ObservableCollection<MediaGroupConfig>? groups,
        string fallbackName)
    {
        groups ??= [];

        foreach (var group in groups)
        {
            group.Id = string.IsNullOrWhiteSpace(group.Id) ? Guid.NewGuid().ToString("N") : group.Id;
            group.Name = string.IsNullOrWhiteSpace(group.Name) ? fallbackName : group.Name.Trim();
        }

        return groups;
    }

    private static ObservableCollection<MediaAssetConfig> NormalizeMediaLibrary(ObservableCollection<MediaAssetConfig>? library)
    {
        library ??= [];

        foreach (var asset in library)
        {
            asset.Id = string.IsNullOrWhiteSpace(asset.Id) ? Guid.NewGuid().ToString("N") : asset.Id;
            asset.Name = string.IsNullOrWhiteSpace(asset.Name) ? Path.GetFileNameWithoutExtension(asset.FilePath ?? "") : asset.Name.Trim();
            asset.FilePath ??= "";
            asset.GroupId ??= "";
            asset.DurationMs = asset.DurationMs;
            asset.Width = asset.Width;
            asset.Height = asset.Height;
        }

        return library;
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
