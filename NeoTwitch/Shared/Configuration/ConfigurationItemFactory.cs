using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public static class ConfigurationItemFactory
{
    private const int FirstSuggestedArduinoPin = 2;
    private const int DefaultArduinoPin = 6;
    private const int DefaultLedCount = 30;

    public static EventRule CreateRule(IUiTextService text, Func<string>? idFactory = null)
    {
        return new EventRule
        {
            Id = CreateId(idFactory),
            Name = text.Get(UiTextKeys.ConfigurationNewRuleName),
            EventKind = TwitchEventKind.Follow,
            MinimumBits = 1,
            UseLights = false,
            PlayAudio = false,
            SendChatMessage = false,
            ChatMessageTemplate = text.Get(UiTextKeys.ConfigurationNewRuleChatTemplate)
        };
    }

    public static LedStripConfig CreateLedStrip(
        IEnumerable<LedStripConfig> existingStrips,
        IUiTextService text,
        Func<string>? idFactory = null)
    {
        var nextPin = Enumerable
            .Range(FirstSuggestedArduinoPin, ApplicationLimits.MaxArduinoPin - FirstSuggestedArduinoPin + 1)
            .FirstOrDefault(pin => existingStrips.All(strip => strip.Pin != pin));

        return new LedStripConfig
        {
            Id = CreateId(idFactory),
            Name = text.Get(UiTextKeys.ConfigurationNewLedStripName),
            Pin = nextPin == 0 ? DefaultArduinoPin : nextPin,
            LedCount = DefaultLedCount
        };
    }

    public static LedStripConfig DuplicateLedStrip(
        LedStripConfig strip,
        IUiTextService text,
        Func<string>? idFactory = null)
    {
        return new LedStripConfig
        {
            Id = CreateId(idFactory),
            Name = $"{strip.Name} {text.Get(UiTextKeys.ConfigurationCopySuffix)}".Trim(),
            Pin = strip.Pin,
            LedCount = strip.LedCount
        };
    }

    private static string CreateId(Func<string>? idFactory)
    {
        return idFactory?.Invoke() ?? Guid.NewGuid().ToString("N");
    }
}
