using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch.Services.Ui;

public static class UiOptionCatalog
{
    public static IReadOnlyList<UiOption<TwitchEventKind>> EventOptions { get; } =
        CreateEventOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<string>> RuleCategoryOptions { get; } =
        CreateRuleCategoryOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<LightPattern>> PatternOptions { get; } =
        CreatePatternOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<string>> ThemeModeOptions { get; } =
        CreateThemeModeOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<ObsMediaKind>> ObsMediaKindOptions { get; } =
        CreateObsMediaKindOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<MediaSourceMode>> MediaSourceModeOptions { get; } =
        CreateMediaSourceModeOptions(UiTextService.CreateDefault());

    public static IReadOnlyList<UiOption<TwitchEventKind>> CreateEventOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionEventFollow), TwitchEventKind.Follow),
        new(text.Get(UiTextKeys.OptionEventSubscription), TwitchEventKind.Subscription),
        new(text.Get(UiTextKeys.OptionEventRaid), TwitchEventKind.Raid),
        new(text.Get(UiTextKeys.OptionEventCheer), TwitchEventKind.Cheer),
        new(text.Get(UiTextKeys.OptionEventChatCommand), TwitchEventKind.ChatCommand),
        new(text.Get(UiTextKeys.OptionEventChannelPointRedemption), TwitchEventKind.ChannelPointRedemption),
        new(text.Get(UiTextKeys.OptionEventTest), TwitchEventKind.Test)
    ];

    public static IReadOnlyList<UiOption<string>> CreateRuleCategoryOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionCategoryAll), ""),
        new(text.Get(UiTextKeys.OptionCategoryFollowers), nameof(TwitchEventKind.Follow)),
        new(text.Get(UiTextKeys.OptionCategorySubscriptions), nameof(TwitchEventKind.Subscription)),
        new(text.Get(UiTextKeys.OptionCategoryRaids), nameof(TwitchEventKind.Raid)),
        new(text.Get(UiTextKeys.OptionCategoryCheers), nameof(TwitchEventKind.Cheer)),
        new(text.Get(UiTextKeys.OptionCategoryChatCommands), nameof(TwitchEventKind.ChatCommand)),
        new(text.Get(UiTextKeys.OptionCategoryRedemptions), nameof(TwitchEventKind.ChannelPointRedemption))
    ];

    public static IReadOnlyList<UiOption<LightPattern>> CreatePatternOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionPatternSolid), LightPattern.Solid),
        new(text.Get(UiTextKeys.OptionPatternPulse), LightPattern.Pulse),
        new(text.Get(UiTextKeys.OptionPatternRainbow), LightPattern.Rainbow),
        new(text.Get(UiTextKeys.OptionPatternChase), LightPattern.Chase),
        new(text.Get(UiTextKeys.OptionPatternTheater), LightPattern.Theater),
        new(text.Get(UiTextKeys.OptionPatternSparkle), LightPattern.Sparkle),
        new(text.Get(UiTextKeys.OptionPatternRave), LightPattern.Rave)
    ];

    public static IReadOnlyList<UiOption<string>> CreateThemeModeOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionThemeSystem), "System"),
        new(text.Get(UiTextKeys.OptionThemeLight), "Light"),
        new(text.Get(UiTextKeys.OptionThemeDark), "Dark")
    ];

    public static IReadOnlyList<UiOption<ObsMediaKind>> CreateObsMediaKindOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionObsMediaImage), ObsMediaKind.Image),
        new(text.Get(UiTextKeys.OptionObsMediaVideo), ObsMediaKind.Video)
    ];

    public static IReadOnlyList<UiOption<MediaSourceMode>> CreateMediaSourceModeOptions(IUiTextService text) =>
    [
        new(text.Get(UiTextKeys.OptionMediaSourceSingle), MediaSourceMode.Single),
        new(text.Get(UiTextKeys.OptionMediaSourceGroup), MediaSourceMode.Group)
    ];
}
