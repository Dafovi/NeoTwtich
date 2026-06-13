using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Activity;

public static class ActivityLogClassifier
{
    public static ActivityLogKind Classify(string message)
    {
        var text = message.ToLowerInvariant();

        if (text.StartsWith("twitch", StringComparison.Ordinal)
            || text.StartsWith("chat", StringComparison.Ordinal)
            || text.Contains("autorizado", StringComparison.Ordinal)
            || text.Contains("escuchando eventos", StringComparison.Ordinal))
        {
            return ActivityLogKind.Twitch;
        }

        if (text.StartsWith("alexa", StringComparison.Ordinal))
        {
            return ActivityLogKind.Alexa;
        }

        if (text.StartsWith("arduino", StringComparison.Ordinal)
            || text.StartsWith("serial", StringComparison.Ordinal)
            || text.StartsWith("fondo", StringComparison.Ordinal)
            || text.StartsWith("luces", StringComparison.Ordinal)
            || text.Contains("puerto com", StringComparison.Ordinal)
            || text.Contains("puertos com", StringComparison.Ordinal))
        {
            return ActivityLogKind.Arduino;
        }

        if (text.StartsWith("audio", StringComparison.Ordinal)
            || text.StartsWith("sonido", StringComparison.Ordinal))
        {
            return ActivityLogKind.Audio;
        }

        if (text.StartsWith("obs", StringComparison.Ordinal))
        {
            return ActivityLogKind.Obs;
        }

        if (text.Contains("siguio", StringComparison.Ordinal)
            || text.Contains("suscribio", StringComparison.Ordinal)
            || text.Contains("raid", StringComparison.Ordinal)
            || text.Contains("bits", StringComparison.Ordinal)
            || text.Contains("canjeo", StringComparison.Ordinal)
            || text.StartsWith("prueba de", StringComparison.Ordinal))
        {
            return ActivityLogKind.Event;
        }

        if (text.Contains("error", StringComparison.Ordinal)
            || text.Contains("fallo", StringComparison.Ordinal)
            || text.Contains("no pude", StringComparison.Ordinal)
            || text.Contains("no puedo", StringComparison.Ordinal)
            || text.Contains("no hay", StringComparison.Ordinal)
            || text.Contains("no encontre", StringComparison.Ordinal))
        {
            return ActivityLogKind.Important;
        }

        return ActivityLogKind.Info;
    }
}
