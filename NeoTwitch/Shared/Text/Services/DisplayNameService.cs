using NeoTwitch.Models;

namespace NeoTwitch.Services.Text;

public static class DisplayNameService
{
    public static string For(TwitchEventKind kind, IUiTextService text) => kind switch
    {
        TwitchEventKind.Follow => text.Get(UiTextKeys.OptionEventFollow),
        TwitchEventKind.Subscription => text.Get(UiTextKeys.OptionEventSubscription),
        TwitchEventKind.Raid => text.Get(UiTextKeys.OptionEventRaid),
        TwitchEventKind.Cheer => text.Get(UiTextKeys.OptionEventCheer),
        TwitchEventKind.ChatCommand => text.Get(UiTextKeys.OptionEventChatCommand),
        TwitchEventKind.ChannelPointRedemption => text.Get(UiTextKeys.OptionEventChannelPointRedemption),
        TwitchEventKind.Test => text.Get(UiTextKeys.OptionEventTest),
        _ => kind.ToString()
    };

    public static string For(LightPattern pattern, IUiTextService text) => pattern switch
    {
        LightPattern.Solid => text.Get(UiTextKeys.OptionPatternSolid),
        LightPattern.Pulse => text.Get(UiTextKeys.OptionPatternPulse),
        LightPattern.Rainbow => text.Get(UiTextKeys.OptionPatternRainbow),
        LightPattern.Chase => text.Get(UiTextKeys.OptionPatternChase),
        LightPattern.Theater => text.Get(UiTextKeys.OptionPatternTheater),
        LightPattern.Sparkle => text.Get(UiTextKeys.OptionPatternSparkle),
        LightPattern.Rave => text.Get(UiTextKeys.OptionPatternRave),
        _ => pattern.ToString()
    };
}
