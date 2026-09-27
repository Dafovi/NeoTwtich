using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

/// <summary>
/// Bridges neutral platform events to the established alert execution payload while rules are
/// still modelled with Twitch-compatible event kinds.
/// </summary>
public static class StreamEventRuleAdapter
{
    public static TwitchEvent ToAlertEvent(StreamEvent streamEvent)
    {
        ArgumentNullException.ThrowIfNull(streamEvent);
        return new TwitchEvent
        {
            Kind = streamEvent.Kind switch
            {
                StreamEventKind.Follow => TwitchEventKind.Follow,
                StreamEventKind.Subscription => TwitchEventKind.Subscription,
                StreamEventKind.Raid => TwitchEventKind.Raid,
                StreamEventKind.PlatformCurrency => TwitchEventKind.Cheer,
                StreamEventKind.ChatCommand => TwitchEventKind.ChatCommand,
                StreamEventKind.RewardRedemption => TwitchEventKind.ChannelPointRedemption,
                StreamEventKind.Test => TwitchEventKind.Test,
                _ => throw new ArgumentOutOfRangeException(nameof(streamEvent), streamEvent.Kind, null)
            },
            Title = streamEvent.Title,
            UserName = streamEvent.UserName,
            RewardTitle = streamEvent.RewardTitle,
            ViewerCount = streamEvent.ViewerCount,
            Bits = streamEvent.ContributionUnits,
            Message = streamEvent.Message,
            RawType = streamEvent.RawType,
            EventSubMessageId = streamEvent.EventId,
            EventSubMessageType = streamEvent.Metadata is not null
                && streamEvent.Metadata.TryGetValue("messageType", out var type)
                    ? type
                    : ""
        };
    }
}
