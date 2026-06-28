using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Activity;

public static class ActivityLogPresentationService
{
    public static ActivityLogPresentation Build(string message, ActivityLogKind kind)
    {
        var sourceKey = ChooseSourceKey(message, kind);
        var statusText = ChooseStatusText(message, kind);
        var activityIconPath = ChooseActivityIconPath(message, kind, sourceKey);
        var title = BuildTitle(message, kind);

        return new ActivityLogPresentation(
            SourceKey: sourceKey,
            FilterKey: sourceKey,
            IsImportant: kind == ActivityLogKind.Important || !string.Equals(statusText, "OK", StringComparison.OrdinalIgnoreCase),
            SourceName: SourceDisplayName(sourceKey),
            Category: BuildCategory(message, kind),
            Title: title,
            Description: BuildDescription(message, title),
            AccentColor: ChooseAccentColor(message, kind),
            SourceAccentColor: ChooseSourceAccentColor(sourceKey),
            SourceIconPath: ChooseServiceIconPath(sourceKey),
            SourceIconKey: ChooseSourceIconKey(sourceKey),
            StatusText: statusText,
            StatusAccentColor: StatusAccent(statusText),
            StatusIconPath: ChooseStatusIconPath(statusText, sourceKey),
            ActivityIconPath: activityIconPath,
            ActivityIconUsesOriginalImage: IsServiceIconPath(activityIconPath),
            ActivityIconKey: ChooseIconKey(message, kind));
    }

    private static string ChooseSourceKey(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();

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

    private static string ChooseSourceAccentColor(string sourceKey)
    {
        return sourceKey.ToUpperInvariant() switch
        {
            "TWITCH" => "#9146FF",
            "ARDUINO" => "#00878F",
            "ALEXA" => "#2FB4E9",
            "AUDIO" => "#B56CFF",
            "OBS" => "#22C55E",
            "EVENTO" => "#22C55E",
            "SISTEMA" => "#94A3B8",
            "IMPORTANTE" => "#FFB020",
            _ => "#14B8A6"
        };
    }

    private static string SourceDisplayName(string sourceKey)
    {
        return sourceKey switch
        {
            "TWITCH" => "Twitch",
            "ARDUINO" => "Arduino",
            "ALEXA" => "Alexa",
            "AUDIO" => "Audio",
            "OBS" => "OBS",
            "EVENTO" => "Evento",
            "IMPORTANTE" => "Importante",
            _ => "Sistema"
        };
    }

    private static string BuildCategory(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();

        if (kind == ActivityLogKind.Event)
        {
            if (text.Contains("bits", StringComparison.Ordinal))
            {
                return "BITS";
            }

            if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
            {
                return "SUB";
            }

            if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
            {
                return "SEGUIDOR";
            }

            if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
            {
                return "CHAT";
            }

            if (text.Contains("raid", StringComparison.Ordinal))
            {
                return "RAID";
            }

            if (text.Contains("canje", StringComparison.Ordinal))
            {
                return "CANJE";
            }

            return "EVENTO";
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

    private static string BuildTitle(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();

        if (kind == ActivityLogKind.Event)
        {
            if (text.Contains("bits", StringComparison.Ordinal))
            {
                return "Bits recibidos";
            }

            if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
            {
                return "Suscripcion";
            }

            if (text.Contains("raid", StringComparison.Ordinal))
            {
                return "Raid recibida";
            }

            if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
            {
                return "Nuevo seguidor";
            }

            if (text.Contains("canje", StringComparison.Ordinal))
            {
                return "Canje activado";
            }

            if (text.Contains("comando", StringComparison.Ordinal) || text.Contains("chat", StringComparison.Ordinal))
            {
                return "Comando de chat";
            }

            if (text.Contains("prueba", StringComparison.Ordinal))
            {
                return "Prueba de alerta";
            }

            return "Alerta activada";
        }

        if (IsTwitchMessage(text, kind))
        {
            return kind == ActivityLogKind.Important ? "Aviso de Twitch" : "Twitch";
        }

        if (IsArduinoMessage(text, kind))
        {
            return kind == ActivityLogKind.Important ? "Aviso de Arduino" : "Arduino";
        }

        if (IsAlexaMessage(text, kind))
        {
            return text.Contains("fondo", StringComparison.Ordinal)
                ? "Rutina Alexa"
                : kind == ActivityLogKind.Important ? "Aviso de Alexa" : "Alexa";
        }

        if (IsAudioMessage(text, kind))
        {
            return kind == ActivityLogKind.Important ? "Aviso de audio" : "Audio";
        }

        if (IsObsMessage(text, kind))
        {
            return kind == ActivityLogKind.Important ? "Aviso de OBS" : "OBS";
        }

        if (kind == ActivityLogKind.Important)
        {
            return "Aviso importante";
        }

        if (text.StartsWith("fondo", StringComparison.Ordinal) || text.StartsWith("luces", StringComparison.Ordinal))
        {
            return "Luces";
        }

        if (text.StartsWith("configuracion", StringComparison.Ordinal))
        {
            return "Configuracion";
        }

        if (text.StartsWith("version", StringComparison.Ordinal))
        {
            return "Version";
        }

        if (text.StartsWith("simulador", StringComparison.Ordinal))
        {
            return "Simulador";
        }

        return "Sistema";
    }

    private static string BuildDescription(string message, string title)
    {
        var clean = message.Trim();
        var separator = clean.IndexOf(':', StringComparison.Ordinal);
        if (separator > 0 && separator < clean.Length - 1)
        {
            var prefix = clean[..separator].Trim();
            if (string.Equals(prefix, title, StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Twitch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Alexa", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Arduino", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Audio", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "OBS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Chat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Fondo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Luces", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Version", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Configuracion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(prefix, "Simulador", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[(separator + 1)..].Trim();
            }
        }

        return string.IsNullOrWhiteSpace(clean)
            ? message
            : clean;
    }

    private static string ChooseAccentColor(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();

        if (kind == ActivityLogKind.Event)
        {
            if (text.Contains("bits", StringComparison.Ordinal))
            {
                return "#37C7F3";
            }

            if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
            {
                return "#B56CFF";
            }

            if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
            {
                return "#14B8A6";
            }

            if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
            {
                return "#22C55E";
            }

            if (text.Contains("raid", StringComparison.Ordinal))
            {
                return "#F59E0B";
            }

            return "#00C7B7";
        }

        if (IsTwitchMessage(text, kind))
        {
            return "#9146FF";
        }

        if (IsArduinoMessage(text, kind))
        {
            return "#00878F";
        }

        if (IsAlexaMessage(text, kind))
        {
            return "#2FB4E9";
        }

        if (IsAudioMessage(text, kind))
        {
            return "#B56CFF";
        }

        if (IsObsMessage(text, kind))
        {
            return "#22C55E";
        }

        return kind == ActivityLogKind.Important ? "#FFB020" : "#AFA4CC";
    }

    private static string ChooseServiceIconPath(string sourceKey)
    {
        return sourceKey switch
        {
            "TWITCH" => "Assets/Icons/service_twitch.png",
            "ARDUINO" => "Assets/Icons/service_arduino.png",
            "ALEXA" => "Assets/Icons/service_alexa.png",
            "AUDIO" => "Assets/Icons/service_audio.png",
            "OBS" => "Assets/ServiceObs.png",
            _ => ""
        };
    }

    private static string ChooseActivityIconPath(string message, ActivityLogKind kind, string sourceKey)
    {
        var text = message.ToLowerInvariant();

        if (kind == ActivityLogKind.Event)
        {
            if (text.Contains("bits", StringComparison.Ordinal))
            {
                return "Assets/Icons/action_bits.png";
            }

            if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
            {
                return "Assets/Icons/action_subscription.png";
            }

            if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
            {
                return "Assets/Icons/action_follower.png";
            }

            if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
            {
                return "Assets/Icons/action_message.png";
            }

            return "Assets/Icons/activity_notification.png";
        }

        if (kind == ActivityLogKind.Important)
        {
            return "Assets/Icons/status_important.png";
        }

        return ChooseServiceIconPath(sourceKey);
    }

    private static bool IsServiceIconPath(string iconPath)
    {
        return iconPath.Contains("/service_", StringComparison.OrdinalIgnoreCase)
            || iconPath.Contains("\\service_", StringComparison.OrdinalIgnoreCase);
    }

    private static string ChooseSourceIconKey(string sourceKey)
    {
        return sourceKey switch
        {
            "EVENTO" => "Event",
            "IMPORTANTE" => "Warning",
            "SISTEMA" => "Settings",
            _ => "Activity"
        };
    }

    private static string ChooseStatusText(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();
        if (text.Contains("error", StringComparison.Ordinal)
            || text.Contains("fallo", StringComparison.Ordinal)
            || text.Contains("no pude", StringComparison.Ordinal)
            || text.Contains("no puedo", StringComparison.Ordinal)
            || text.Contains("no hay", StringComparison.Ordinal)
            || text.Contains("no encontre", StringComparison.Ordinal)
            || text.Contains("no se pudo", StringComparison.Ordinal)
            || text.Contains("tardo demasiado", StringComparison.Ordinal))
        {
            return "Error";
        }

        if (kind == ActivityLogKind.Important
            || text.Contains("advertencia", StringComparison.Ordinal)
            || text.Contains("aviso", StringComparison.Ordinal)
            || text.Contains("descart", StringComparison.Ordinal)
            || text.Contains("no coincide", StringComparison.Ordinal))
        {
            return "Aviso";
        }

        return "OK";
    }

    private static string StatusAccent(string statusText)
    {
        return statusText switch
        {
            "Error" => "#F43F5E",
            "Aviso" => "#FFB020",
            _ => "#22C55E"
        };
    }

    private static string ChooseStatusIconPath(string statusText, string filterKey)
    {
        return statusText switch
        {
            "Error" => "Assets/Icons/status_error.png",
            "Aviso" when string.Equals(filterKey, "IMPORTANTE", StringComparison.OrdinalIgnoreCase) => "Assets/Icons/status_important.png",
            "Aviso" => "Assets/Icons/status_warning.png",
            _ => "Assets/Icons/status_ok.png"
        };
    }

    private static string ChooseIconKey(string message, ActivityLogKind kind)
    {
        var text = message.ToLowerInvariant();

        if (kind == ActivityLogKind.Important)
        {
            return "Warning";
        }

        if (kind == ActivityLogKind.Event)
        {
            if (text.Contains("bits", StringComparison.Ordinal))
            {
                return "Bits";
            }

            if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
            {
                return "Star";
            }

            if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
            {
                return "Users";
            }

            if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
            {
                return "Chat";
            }

            if (text.Contains("raid", StringComparison.Ordinal))
            {
                return "Zap";
            }

            return "Event";
        }

        if (text.StartsWith("arduino", StringComparison.Ordinal))
        {
            return "Arduino";
        }

        if (text.StartsWith("fondo", StringComparison.Ordinal) || text.StartsWith("luces", StringComparison.Ordinal))
        {
            return "Sun";
        }

        return "Activity";
    }

    private static bool IsTwitchMessage(string text, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Twitch || text.StartsWith("twitch", StringComparison.Ordinal);
    }

    private static bool IsArduinoMessage(string text, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Arduino
            || text.StartsWith("arduino", StringComparison.Ordinal)
            || text.StartsWith("serial", StringComparison.Ordinal);
    }

    private static bool IsAlexaMessage(string text, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Alexa || text.StartsWith("alexa", StringComparison.Ordinal);
    }

    private static bool IsAudioMessage(string text, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Audio
            || text.Contains("audio", StringComparison.Ordinal)
            || text.Contains("sonido", StringComparison.Ordinal);
    }

    private static bool IsObsMessage(string text, ActivityLogKind kind)
    {
        return kind == ActivityLogKind.Obs
            || text.StartsWith("obs", StringComparison.Ordinal);
    }
}

public sealed record ActivityLogPresentation(
    string SourceKey,
    string FilterKey,
    bool IsImportant,
    string SourceName,
    string Category,
    string Title,
    string Description,
    string AccentColor,
    string SourceAccentColor,
    string SourceIconPath,
    string SourceIconKey,
    string StatusText,
    string StatusAccentColor,
    string StatusIconPath,
    string ActivityIconPath,
    bool ActivityIconUsesOriginalImage,
    string ActivityIconKey);
