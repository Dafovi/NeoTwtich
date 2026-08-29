using NeoTwitch.Models;
using Protocol = NeoTwitch.Services.TwitchEventSubProtocol;

namespace NeoTwitch.Services;

public static class TwitchEventSubSubscriptionPlanner
{
    public static IReadOnlyList<EventSubDefinition> BuildDefinitions(AppConfig config)
    {
        var broadcasterId = config.Channel.UserId;
        return config.Rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => rule.EventKind)
            .Where(kind => kind != TwitchEventKind.Test)
            .Distinct()
            .SelectMany(kind => BuildDefinitionsForKind(kind, broadcasterId))
            .DistinctBy(definition => definition.Type)
            .ToArray();
    }

    public static IEnumerable<EventSubDefinition> BuildDefinitionsForKind(
        TwitchEventKind kind,
        string broadcasterId)
    {
        switch (kind)
        {
            case TwitchEventKind.Follow:
                yield return Create(
                    Protocol.Events.Follow,
                    Protocol.Versions.V2,
                    (Protocol.Conditions.BroadcasterUserId, broadcasterId),
                    (Protocol.Conditions.ModeratorUserId, broadcasterId));
                yield break;
            case TwitchEventKind.Subscription:
                yield return Create(Protocol.Events.Subscribe, Protocol.Versions.V1, (Protocol.Conditions.BroadcasterUserId, broadcasterId));
                yield return Create(Protocol.Events.SubscriptionMessage, Protocol.Versions.V1, (Protocol.Conditions.BroadcasterUserId, broadcasterId));
                yield return Create(Protocol.Events.SubscriptionGift, Protocol.Versions.V1, (Protocol.Conditions.BroadcasterUserId, broadcasterId));
                yield break;
            case TwitchEventKind.Raid:
                yield return Create(Protocol.Events.Raid, Protocol.Versions.V1, (Protocol.Conditions.ToBroadcasterUserId, broadcasterId));
                yield break;
            case TwitchEventKind.Cheer:
                yield return Create(Protocol.Events.Cheer, Protocol.Versions.V1, (Protocol.Conditions.BroadcasterUserId, broadcasterId));
                yield break;
            case TwitchEventKind.ChatCommand:
                yield return Create(
                    Protocol.Events.ChatMessage,
                    Protocol.Versions.V1,
                    (Protocol.Conditions.BroadcasterUserId, broadcasterId),
                    (Protocol.Conditions.UserId, broadcasterId));
                yield break;
            case TwitchEventKind.ChannelPointRedemption:
                yield return Create(Protocol.Events.ChannelPointRedemption, Protocol.Versions.V1, (Protocol.Conditions.BroadcasterUserId, broadcasterId));
                yield break;
            default:
                throw new InvalidOperationException($"Tipo de evento Twitch no soportado para EventSub: {kind}.");
        }
    }

    private static EventSubDefinition Create(string type, string version, params (string Key, string Value)[] conditions)
    {
        return new EventSubDefinition(
            type,
            version,
            conditions.ToDictionary(condition => condition.Key, condition => condition.Value));
    }
}

public sealed record EventSubDefinition(
    string Type,
    string Version,
    Dictionary<string, string> Condition,
    bool IsRequired = true);
