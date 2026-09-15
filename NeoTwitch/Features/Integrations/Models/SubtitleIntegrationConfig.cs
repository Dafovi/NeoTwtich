namespace NeoTwitch.Models;

public sealed class SubtitleIntegrationConfig
{
    public string Scene { get; set; } = "";
    public string SourceLanguage { get; set; } = "es-CO";
    public string TargetLanguage { get; set; } = "en";
    public bool Translate { get; set; } = true;
    public bool ShowOriginal { get; set; } = true;
    public bool PublishToChat { get; set; }
    public int FontSize { get; set; } = 36;
    public string Color { get; set; } = "#FFFFFF";
    public int DurationSeconds { get; set; } = 8;
    public string IgnoredUsers { get; set; } = "nightbot,streamelements,streamlabs";
    public bool PlaceAtTop { get; set; }
    // Runtime opt-in only: never starts chat translation when restoring settings.
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Running { get; set; }

    public SubtitleIntegrationConfig Snapshot() => new()
    {
        Scene = Scene ?? "", SourceLanguage = SourceLanguage ?? "es-CO", TargetLanguage = TargetLanguage ?? "en",
        Translate = Translate, ShowOriginal = ShowOriginal, PublishToChat = PublishToChat,
        FontSize = Math.Clamp(FontSize, 16, 100),
        Color = System.Text.RegularExpressions.Regex.IsMatch(Color ?? "", "^#[0-9a-fA-F]{6}$") ? Color! : "#FFFFFF",
        DurationSeconds = Math.Clamp(DurationSeconds, 2, 60), IgnoredUsers = IgnoredUsers ?? "", PlaceAtTop = PlaceAtTop
    };
}
