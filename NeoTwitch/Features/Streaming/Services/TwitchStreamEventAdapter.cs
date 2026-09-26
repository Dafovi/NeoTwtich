using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public static class TwitchStreamEventAdapter
{
    private const string SessionIdMetadataKey = "eventSubSessionId";
    private const string MessageTypeMetadataKey = "eventSubMessageType";

    public static StreamEvent FromTwitch(TwitchEvent twitchEvent, DateTimeOffset? occurredAt = null)
    {
        ArgumentNullException.ThrowIfNull(twitchEvent);
        return new StreamEvent(
            StreamingPlatform.Twitch,
            ToStreamEventKind(twitchEvent.Kind),
            twitchEvent.EventSubMessageId,
            occurredAt ?? DateTimeOffset.UtcNow,
            twitchEvent.Title,
            twitchEvent.UserName,
            twitchEvent.RewardTitle,
            twitchEvent.ViewerCount,
            twitchEvent.Bits,
            twitchEvent.Message,
            twitchEvent.RawType,
            new Dictionary<string, string>
            {
                [SessionIdMetadataKey] = twitchEvent.EventSubSessionId,
                [MessageTypeMetadataKey] = twitchEvent.EventSubMessageType
            });
    }

    public static TwitchEvent ToTwitch(StreamEvent streamEvent)
    {
        ArgumentNullException.ThrowIfNull(streamEvent);
        if (streamEvent.Platform != StreamingPlatform.Twitch)
        {
            throw new ArgumentException("Solo los eventos de Twitch pueden convertirse a TwitchEvent.", nameof(streamEvent));
        }

        return new TwitchEvent
        {
            Kind = ToTwitchEventKind(streamEvent.Kind),
            Title = streamEvent.Title,
            UserName = streamEvent.UserName,
            RewardTitle = streamEvent.RewardTitle,
            ViewerCount = streamEvent.ViewerCount,
            Bits = streamEvent.ContributionUnits,
            Message = streamEvent.Message,
            RawType = streamEvent.RawType,
            EventSubMessageId = streamEvent.EventId,
            EventSubSessionId = GetMetadata(streamEvent, SessionIdMetadataKey),
            EventSubMessageType = GetMetadata(streamEvent, MessageTypeMetadataKey)
        };
    }

    private static StreamEventKind ToStreamEventKind(TwitchEventKind kind) => kind switch
    {
        TwitchEventKind.Follow => StreamEventKind.Follow,
        TwitchEventKind.Subscription => StreamEventKind.Subscription,
        TwitchEventKind.Raid => StreamEventKind.Raid,
        TwitchEventKind.Cheer => StreamEventKind.PlatformCurrency,
        TwitchEventKind.ChatCommand => StreamEventKind.ChatCommand,
        TwitchEventKind.ChannelPointRedemption => StreamEventKind.RewardRedemption,
        TwitchEventKind.Test => StreamEventKind.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static TwitchEventKind ToTwitchEventKind(StreamEventKind kind) => kind switch
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

    private static string GetMetadata(StreamEvent streamEvent, string key) =>
        streamEvent.Metadata is not null && streamEvent.Metadata.TryGetValue(key, out var value)
            ? value
            : "";
}
