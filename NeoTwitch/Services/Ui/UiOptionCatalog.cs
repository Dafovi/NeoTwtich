using NeoTwitch.Models;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch.Services.Ui;

public static class UiOptionCatalog
{
    public static IReadOnlyList<UiOption<TwitchEventKind>> EventOptions { get; } =
    [
        new("Nuevo seguidor", TwitchEventKind.Follow),
        new("Nueva suscripcion", TwitchEventKind.Subscription),
        new("Raid recibida", TwitchEventKind.Raid),
        new("Bits", TwitchEventKind.Cheer),
        new("Comando de chat", TwitchEventKind.ChatCommand),
        new("Canje de puntos", TwitchEventKind.ChannelPointRedemption),
        new("Prueba manual", TwitchEventKind.Test)
    ];

    public static IReadOnlyList<UiOption<string>> RuleCategoryOptions { get; } =
    [
        new("Todas las categorias", ""),
        new("Seguidores", nameof(TwitchEventKind.Follow)),
        new("Suscripciones", nameof(TwitchEventKind.Subscription)),
        new("Raids", nameof(TwitchEventKind.Raid)),
        new("Bits", nameof(TwitchEventKind.Cheer)),
        new("Comandos de chat", nameof(TwitchEventKind.ChatCommand)),
        new("Canjes de puntos", nameof(TwitchEventKind.ChannelPointRedemption))
    ];

    public static IReadOnlyList<UiOption<LightPattern>> PatternOptions { get; } =
    [
        new("Color fijo", LightPattern.Solid),
        new("Pulso", LightPattern.Pulse),
        new("Arcoiris", LightPattern.Rainbow),
        new("Carrera", LightPattern.Chase),
        new("Teatro", LightPattern.Theater),
        new("Destellos", LightPattern.Sparkle),
        new("Rave", LightPattern.Rave)
    ];

    public static IReadOnlyList<UiOption<string>> ThemeModeOptions { get; } =
    [
        new("Seguir Windows", "System"),
        new("Claro", "Light"),
        new("Oscuro", "Dark")
    ];

    public static IReadOnlyList<UiOption<ObsMediaKind>> ObsMediaKindOptions { get; } =
    [
        new("Imagen", ObsMediaKind.Image),
        new("Video", ObsMediaKind.Video)
    ];

    public static IReadOnlyList<UiOption<MediaSourceMode>> MediaSourceModeOptions { get; } =
    [
        new("Un archivo", MediaSourceMode.Single),
        new("Grupo aleatorio", MediaSourceMode.Group)
    ];
}
