using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public static class EventRuleMatcherService
{
    public static EventRule[] ResolveMatches(IEnumerable<EventRule> rules, TwitchEvent twitchEvent)
    {
        var matchingRules = rules
            .Where(rule => rule.Matches(twitchEvent))
            .ToArray();

        if (twitchEvent.Kind != TwitchEventKind.Cheer || matchingRules.Length == 0)
        {
            return matchingRules;
        }

        var highestThreshold = matchingRules.Max(rule => rule.MinimumBits);
        return matchingRules
            .Where(rule => rule.MinimumBits == highestThreshold)
            .ToArray();
    }
}
