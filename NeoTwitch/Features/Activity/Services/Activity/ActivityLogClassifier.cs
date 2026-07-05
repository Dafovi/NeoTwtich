using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Activity;

public static class ActivityLogClassifier
{
    public static ActivityLogKind Classify(string message)
    {
        var text = Normalize(message);
        if (LooksLikeEvent(text))
        {
            return ActivityLogKind.Event;
        }

        return ResolveSourceKey(message, ActivityLogKind.Info) switch
        {
            "TWITCH" => ActivityLogKind.Twitch,
            "ARDUINO" => ActivityLogKind.Arduino,
            "ALEXA" => ActivityLogKind.Alexa,
            "AUDIO" => ActivityLogKind.Audio,
            "OBS" => ActivityLogKind.Obs,
            _ => LooksImportant(message) ? ActivityLogKind.Important : ActivityLogKind.Info
        };
    }

    public static string ResolveSourceKey(string message, ActivityLogKind kind)
    {
        var text = Normalize(message);

        if (kind == ActivityLogKind.Event)
        {
            return "EVENTO";
        }

        if (IsTwitchMessage(text, kind)
            || text.StartsWith("chat", StringComparison.Ordinal)
            || text.Contains("autorizado", StringComparison.Ordinal)
            || text.Contains("escuchando eventos", StringComparison.Ordinal))
        {
            return "TWITCH";
        }

        if (IsArduinoMessage(text, kind)
            || text.StartsWith("fondo", StringComparison.Ordinal)
            || text.StartsWith("luces", StringComparison.Ordinal))
        {
            return "ARDUINO";
        }

        if (IsAlexaMessage(text, kind))
        {
            return "ALEXA";
        }

        if (IsAudioMessage(text, kind))
        {
            return "AUDIO";
        }

        if (IsObsMessage(text, kind))
        {
            return "OBS";
        }

        return "SISTEMA";
    }

    public static string ResolveCategory(string message, ActivityLogKind kind)
    {
        var text = Normalize(message);

        if (kind == ActivityLogKind.Event)
        {
            return ResolveEventCategory(text);
        }

        if (IsTwitchMessage(text, kind))
        {
            return "TWITCH";
        }

        if (IsArduinoMessage(text, kind))
        {
            return "ARDUINO";
        }

        if (IsAlexaMessage(text, kind))
        {
            return "ALEXA";
        }

        if (IsAudioMessage(text, kind))
        {
            return "AUDIO";
        }

        if (IsObsMessage(text, kind))
        {
            return "OBS";
        }

        return kind == ActivityLogKind.Important ? "IMPORTANTE" : "SISTEMA";
    }

    public static string Normalize(string message)
    {
        return message.ToLowerInvariant();
    }

    public static bool IsTwitchMessage(string normalizedText, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Twitch || normalizedText.StartsWith("twitch", StringComparison.Ordinal);
    }

    public static bool IsArduinoMessage(string normalizedText, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Arduino
            || normalizedText.StartsWith("arduino", StringComparison.Ordinal)
            || normalizedText.StartsWith("serial", StringComparison.Ordinal)
            || normalizedText.Contains("puerto com", StringComparison.Ordinal)
            || normalizedText.Contains("puertos com", StringComparison.Ordinal);
    }

    public static bool IsAlexaMessage(string normalizedText, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Alexa || normalizedText.StartsWith("alexa", StringComparison.Ordinal);
    }

    public static bool IsAudioMessage(string normalizedText, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Audio
            || normalizedText.Contains("audio", StringComparison.Ordinal)
            || normalizedText.Contains("sonido", StringComparison.Ordinal);
    }

    public static bool IsObsMessage(string normalizedText, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Obs
            || normalizedText.StartsWith("obs", StringComparison.Ordinal);
    }

    private static string ResolveEventCategory(string normalizedText)
    {
        if (normalizedText.Contains("bits", StringComparison.Ordinal))
        {
            return "BITS";
        }

        if (normalizedText.Contains("suscripcion", StringComparison.Ordinal) || normalizedText.Contains("suscribio", StringComparison.Ordinal))
        {
            return "SUB";
        }

        if (normalizedText.Contains("siguio", StringComparison.Ordinal) || normalizedText.Contains("seguidor", StringComparison.Ordinal))
        {
            return "SEGUIDOR";
        }

        if (normalizedText.Contains("chat", StringComparison.Ordinal) || normalizedText.Contains("comando", StringComparison.Ordinal))
        {
            return "CHAT";
        }

        if (normalizedText.Contains("raid", StringComparison.Ordinal))
        {
            return "RAID";
        }

        if (normalizedText.Contains("canje", StringComparison.Ordinal))
        {
            return "CANJE";
        }

        return "EVENTO";
    }

    private static bool LooksLikeEvent(string normalizedText)
    {
        return normalizedText.Contains("siguio", StringComparison.Ordinal)
            || normalizedText.Contains("suscribio", StringComparison.Ordinal)
            || normalizedText.Contains("raid", StringComparison.Ordinal)
            || normalizedText.Contains("bits", StringComparison.Ordinal)
            || normalizedText.Contains("canjeo", StringComparison.Ordinal)
            || normalizedText.StartsWith("prueba de", StringComparison.Ordinal);
    }

    private static bool LooksImportant(string message)
    {
        var text = Normalize(message);
        return text.Contains("error", StringComparison.Ordinal)
            || text.Contains("fallo", StringComparison.Ordinal)
            || text.Contains("no pude", StringComparison.Ordinal)
            || text.Contains("no puedo", StringComparison.Ordinal)
            || text.Contains("no hay", StringComparison.Ordinal)
            || text.Contains("no encontre", StringComparison.Ordinal)
            || text.Contains("no se pudo", StringComparison.Ordinal)
            || text.Contains("advertencia", StringComparison.Ordinal)
            || text.Contains("aviso", StringComparison.Ordinal);
    }
}
