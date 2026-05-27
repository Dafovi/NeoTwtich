using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LucesCanjeTwitch.Models;

namespace LucesCanjeTwitch.Services;

public sealed class SettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SettingsStore()
    {
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LucesCanjeTwitch",
        "settings.json");

    public string? LastLoadError { get; private set; }

    public AppConfig Load()
    {
        LastLoadError = null;

        if (!File.Exists(SettingsPath))
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? AppConfig.CreateDefault();
            return NormalizeConfig(config);
        }
        catch (Exception ex)
        {
            LastLoadError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo leer la configuracion: {SettingsPath}");
            return AppConfig.CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        var tempPath = Path.Combine(directory, "settings.tmp");
        var backupPath = Path.Combine(directory, "settings.backup.json");

        File.WriteAllText(tempPath, json);

        if (File.Exists(SettingsPath))
        {
            File.Copy(SettingsPath, backupPath, overwrite: true);
        }

        File.Copy(tempPath, SettingsPath, overwrite: true);
        File.Delete(tempPath);
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
        config.BackgroundAlexaOnEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOnEventName, "luz_encendida");
        config.BackgroundAlexaOffEventName = NormalizeBackgroundAlexaEventName(config.BackgroundAlexaOffEventName, "luz_apagada");
        config.SerialPort ??= "";
        config.BaudRate = Math.Clamp(config.BaudRate, 300, 921600);
        config.Rules = NormalizeRules(config.Rules, defaults.Rules);
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
