namespace NeoTwitch.Services.Activity;

public static class ActivityLogEventPresentationService
{
    public static ActivityLogEventPresentation Build(string message)
    {
        var text = message.ToLowerInvariant();

        if (text.Contains("bits", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Bits recibidos",
                "#37C7F3",
                "Assets/Icons/action_bits.png",
                "Bits");
        }

        if (text.Contains("suscripcion", StringComparison.Ordinal)
            || text.Contains("suscribio", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Suscripcion",
                "#B56CFF",
                "Assets/Icons/action_subscription.png",
                "Star");
        }

        if (text.Contains("raid", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Raid recibida",
                "#F59E0B",
                "Assets/Icons/activity_notification.png",
                "Zap");
        }

        if (text.Contains("siguio", StringComparison.Ordinal)
            || text.Contains("seguidor", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Nuevo seguidor",
                "#14B8A6",
                "Assets/Icons/action_follower.png",
                "Users");
        }

        if (text.Contains("canje", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Canje activado",
                "#00C7B7",
                "Assets/Icons/activity_notification.png",
                "Event");
        }

        if (text.Contains("comando", StringComparison.Ordinal)
            || text.Contains("chat", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Comando de chat",
                "#22C55E",
                "Assets/Icons/action_message.png",
                "Chat");
        }

        if (text.Contains("prueba", StringComparison.Ordinal))
        {
            return new ActivityLogEventPresentation(
                "Prueba de alerta",
                "#00C7B7",
                "Assets/Icons/activity_notification.png",
                "Event");
        }

        return new ActivityLogEventPresentation(
            "Alerta activada",
            "#00C7B7",
            "Assets/Icons/activity_notification.png",
            "Event");
    }
}

public sealed record ActivityLogEventPresentation(
    string Title,
    string AccentColor,
    string ActivityIconPath,
    string ActivityIconKey);
