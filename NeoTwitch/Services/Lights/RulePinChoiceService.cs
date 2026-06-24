using NeoTwitch.Models;

namespace NeoTwitch.Services.Lights;

public static class RulePinChoiceService
{
    public static RulePinChoices BuildChoices(IEnumerable<LedStripConfig> strips, string currentPinsText)
    {
        var currentPins = string.Join(", ", LightCommand.ParsePins(currentPinsText));
        var options = new List<RulePinChoice>
        {
            new("Todas las salidas", "")
        };

        foreach (var strip in strips.OrderBy(strip => strip.Pin))
        {
            var label = string.IsNullOrWhiteSpace(strip.Name)
                ? $"Pin {strip.Pin}"
                : $"{strip.Name} - Pin {strip.Pin}";
            options.Add(new RulePinChoice(label, strip.Pin.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(currentPins)
            && options.All(option => !string.Equals(option.Value, currentPins, StringComparison.OrdinalIgnoreCase)))
        {
            options.Add(new RulePinChoice($"Personalizado ({currentPins})", currentPins));
        }

        return new RulePinChoices(options, currentPins);
    }
}

public sealed record RulePinChoices(IReadOnlyList<RulePinChoice> Options, string CurrentPins);

public sealed record RulePinChoice(string Label, string Value);
