namespace NeoTwitch.Services.Activity;

public sealed record ActivityLogSourceMetadata(
    string DisplayName,
    string AccentColor,
    string IconPath,
    string IconKey);

public static class ActivityLogSourceCatalog
{
    public static ActivityLogSourceMetadata Get(string sourceKey)
    {
        return sourceKey.ToUpperInvariant() switch
        {
            "TWITCH" => new("Twitch", "#9146FF", "Assets/Icons/service_twitch.png", "Activity"),
            "ARDUINO" => new("Arduino", "#00878F", "Assets/Icons/service_arduino.png", "Activity"),
            "ALEXA" => new("Alexa", "#2FB4E9", "Assets/Icons/service_alexa.png", "Activity"),
            "AUDIO" => new("Audio", "#B56CFF", "Assets/Icons/service_audio.png", "Activity"),
            "OBS" => new("OBS", "#22C55E", "Assets/ServiceObs.png", "Activity"),
            "EVENTO" => new("Evento", "#22C55E", "", "Event"),
            "IMPORTANTE" => new("Importante", "#FFB020", "", "Warning"),
            "SISTEMA" => new("Sistema", "#94A3B8", "", "Settings"),
            _ => new("Sistema", "#14B8A6", "", "Activity")
        };
    }
}
