namespace NeoTwitch.Models;

public static class DisplayNames
{
    public static string For(TwitchEventKind kind) => kind switch
    {
        TwitchEventKind.Follow => "Nuevo seguidor",
        TwitchEventKind.Subscription => "Nueva suscripcion",
        TwitchEventKind.Raid => "Raid recibida",
        TwitchEventKind.Cheer => "Bits",
        TwitchEventKind.ChatCommand => "Comando de chat",
        TwitchEventKind.ChannelPointRedemption => "Canje de puntos",
        TwitchEventKind.Test => "Prueba manual",
        _ => kind.ToString()
    };

    public static string For(LightPattern pattern) => pattern switch
    {
        LightPattern.Solid => "Color fijo",
        LightPattern.Pulse => "Pulso",
        LightPattern.Rainbow => "Arcoiris",
        LightPattern.Chase => "Carrera",
        LightPattern.Theater => "Teatro",
        LightPattern.Sparkle => "Destellos",
        LightPattern.Rave => "Rave",
        _ => pattern.ToString()
    };
}
