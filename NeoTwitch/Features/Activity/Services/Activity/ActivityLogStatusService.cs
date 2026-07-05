using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Activity;

public sealed record ActivityLogStatusMetadata(
    string Text,
    string AccentColor,
    string IconPath,
    bool IsImportant);

public static class ActivityLogStatusService
{
    public static ActivityLogStatusMetadata Build(string message, ActivityLogKind kind, string sourceKey)
    {
        var statusText = ResolveText(message, kind);
        return new ActivityLogStatusMetadata(
            statusText,
            ResolveAccentColor(statusText),
            ResolveIconPath(statusText, sourceKey),
            kind == ActivityLogKind.Important || !string.Equals(statusText, "OK", StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveText(string message, ActivityLogKind kind)
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

    public static string ResolveAccentColor(string statusText)
    {
        return statusText switch
        {
            "Error" => "#F43F5E",
            "Aviso" => "#FFB020",
            _ => "#22C55E"
        };
    }

    public static string ResolveIconPath(string statusText, string sourceKey)
    {
        return statusText switch
        {
            "Error" => "Assets/Icons/status_error.png",
            "Aviso" when string.Equals(sourceKey, "IMPORTANTE", StringComparison.OrdinalIgnoreCase) => "Assets/Icons/status_important.png",
            "Aviso" => "Assets/Icons/status_warning.png",
            _ => "Assets/Icons/status_ok.png"
        };
    }
}
