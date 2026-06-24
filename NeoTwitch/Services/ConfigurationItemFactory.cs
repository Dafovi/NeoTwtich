using NeoTwitch.Models;

namespace NeoTwitch.Services;

public static class ConfigurationItemFactory
{
    private const int FirstSuggestedArduinoPin = 2;
    private const int DefaultArduinoPin = 6;
    private const int DefaultLedCount = 30;

    public static EventRule CreateRule()
    {
        return new EventRule
        {
            Name = "Nueva regla",
            EventKind = TwitchEventKind.Follow,
            MinimumBits = 1,
            UseLights = false,
            PlayAudio = false,
            SendChatMessage = false,
            ChatMessageTemplate = "Gracias @{user}!"
        };
    }

    public static LedStripConfig CreateLedStrip(IEnumerable<LedStripConfig> existingStrips)
    {
        var nextPin = Enumerable
            .Range(FirstSuggestedArduinoPin, ApplicationLimits.MaxArduinoPin - FirstSuggestedArduinoPin + 1)
            .FirstOrDefault(pin => existingStrips.All(strip => strip.Pin != pin));

        return new LedStripConfig
        {
            Name = "Nueva tira",
            Pin = nextPin == 0 ? DefaultArduinoPin : nextPin,
            LedCount = DefaultLedCount
        };
    }
}
