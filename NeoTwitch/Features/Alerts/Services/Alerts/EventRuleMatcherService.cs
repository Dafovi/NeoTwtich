using NeoTwitch.Models;
using NeoTwitch.Services.Streaming;

namespace NeoTwitch.Services.Alerts;

public static class EventRuleMatcherService
{
    public static bool Matches(EventRule rule, StreamEvent streamEvent)
    {
        if (!rule.IsEnabled
            || !RulePlatformSelectionService.Includes(rule, streamEvent.Platform)
            || rule.EventKind != ToTwitchCompatibilityKind(streamEvent.Kind))
        {
            return false;
        }

        if (streamEvent.Kind == StreamEventKind.PlatformCurrency)
        {
            return streamEvent.ContributionUnits is int units && units >= rule.MinimumBits;
        }

        if (streamEvent.Kind == StreamEventKind.ChatCommand)
        {
            return MatchesChatCommand(streamEvent.Message, rule.ChatCommand);
        }

        return streamEvent.Kind != StreamEventKind.RewardRedemption
            || string.IsNullOrWhiteSpace(rule.CustomRewardTitle)
            || string.Equals(rule.CustomRewardTitle.Trim(), streamEvent.RewardTitle?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static EventRule[] ResolveMatches(IEnumerable<EventRule> rules, StreamEvent streamEvent)
    {
        var matchingRules = rules
            .Where(rule => Matches(rule, streamEvent))
            .ToArray();

        if (streamEvent.Kind != StreamEventKind.PlatformCurrency || matchingRules.Length == 0)
        {
            return matchingRules;
        }

        var highestThreshold = matchingRules.Max(rule => rule.MinimumBits);
        return matchingRules
            .Where(rule => rule.MinimumBits == highestThreshold)
            .ToArray();
    }

    public static bool Matches(EventRule rule, TwitchEvent twitchEvent)
    {
        if (!rule.IsEnabled || rule.EventKind != twitchEvent.Kind)
        {
            return false;
        }

        if (rule.EventKind == TwitchEventKind.Cheer)
        {
            return twitchEvent.Bits is int bits && bits >= rule.MinimumBits;
        }

        if (rule.EventKind == TwitchEventKind.ChatCommand)
        {
            return MatchesChatCommand(twitchEvent.Message, rule.ChatCommand);
        }

        if (rule.EventKind != TwitchEventKind.ChannelPointRedemption)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(rule.CustomRewardTitle)
            || string.Equals(rule.CustomRewardTitle.Trim(), twitchEvent.RewardTitle?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static EventRule[] ResolveMatches(IEnumerable<EventRule> rules, TwitchEvent twitchEvent)
    {
        var matchingRules = rules
            .Where(rule => Matches(rule, twitchEvent))
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

    private static bool MatchesChatCommand(string? message, string command)
    {
        if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var firstToken = message.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.Equals(firstToken, NormalizeCommand(command), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCommand(string? value)
    {
        var command = value?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(command)
            ? ""
            : command.StartsWith('!') ? command : $"!{command}";
    }

    private static TwitchEventKind ToTwitchCompatibilityKind(StreamEventKind kind) => kind switch
    {
        StreamEventKind.Follow => TwitchEventKind.Follow,
        StreamEventKind.Subscription => TwitchEventKind.Subscription,
        StreamEventKind.Raid => TwitchEventKind.Raid,
        StreamEventKind.PlatformCurrency => TwitchEventKind.Cheer,
        StreamEventKind.ChatCommand => TwitchEventKind.ChatCommand,
        StreamEventKind.RewardRedemption => TwitchEventKind.ChannelPointRedemption,
        StreamEventKind.Test => TwitchEventKind.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
