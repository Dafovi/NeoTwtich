using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using Protocol = NeoTwitch.Services.TwitchEventSubProtocol;

namespace NeoTwitch.Services;

public sealed class TwitchEventSubMessageParser
{
    private readonly IUiTextService _text;

    public TwitchEventSubMessageParser(IUiTextService text)
    {
        _text = text;
    }

    public string ReadSessionId(string message)
    {
        return ReadSessionInfo(message).SessionId;
    }

    public EventSubSessionInfo ReadSessionInfo(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var messageType = root.GetProperty(Protocol.Json.Metadata).GetProperty(Protocol.Json.MessageType).GetString();

        if (messageType != Protocol.MessageTypes.Welcome)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchEventSubExpectedWelcomeFailure, Protocol.MessageTypes.Welcome, messageType ?? string.Empty));
        }

        var session = root
            .GetProperty(Protocol.Json.Payload)
            .GetProperty(Protocol.Json.Session);
        var sessionId = session
            .GetProperty(Protocol.Json.Id)
            .GetString() ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubMissingSessionId));
        var keepaliveTimeoutSeconds = session.TryGetProperty(Protocol.Json.KeepaliveTimeoutSeconds, out var timeout)
            && timeout.TryGetInt32(out var parsedTimeout)
            && parsedTimeout > 0
                ? parsedTimeout
                : 30;

        return new EventSubSessionInfo(sessionId, TimeSpan.FromSeconds(keepaliveTimeoutSeconds));
    }

    public TwitchEvent? ParseEvent(JsonElement payload)
    {
        var type = payload.GetProperty(Protocol.Json.Subscription).GetProperty(Protocol.Json.Type).GetString();
        var eventPayload = payload.GetProperty(Protocol.Json.Event);

        return type switch
        {
            Protocol.Events.Follow => new TwitchEvent
            {
                Kind = TwitchEventKind.Follow,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleFollow, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.Subscribe => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscribe, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.SubscriptionMessage => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Message = ReadSubscriptionMessage(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscriptionRenew, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.SubscriptionGift => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadSubscriptionGiftUserName(eventPayload),
                ViewerCount = ReadInt(eventPayload, Protocol.EventFields.Total),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscriptionGift, ReadSubscriptionGiftUserName(eventPayload), ReadInt(eventPayload, Protocol.EventFields.Total) ?? 1)
            },
            Protocol.Events.Raid => new TwitchEvent
            {
                Kind = TwitchEventKind.Raid,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.FromBroadcasterUserName),
                ViewerCount = ReadInt(eventPayload, Protocol.EventFields.Viewers),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleRaid, ReadStringOrEmpty(eventPayload, Protocol.EventFields.FromBroadcasterUserName), ReadInt(eventPayload, Protocol.EventFields.Viewers) ?? 0)
            },
            Protocol.Events.Cheer => new TwitchEvent
            {
                Kind = TwitchEventKind.Cheer,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Bits = ReadInt(eventPayload, Protocol.EventFields.Bits),
                Message = ReadString(eventPayload, Protocol.Json.Message),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleCheer, ReadCheerUserName(eventPayload), ReadInt(eventPayload, Protocol.EventFields.Bits) ?? 0)
            },
            Protocol.Events.ChatMessage => new TwitchEvent
            {
                Kind = TwitchEventKind.ChatCommand,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.ChatterUserName),
                Message = ReadChatMessageText(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleChatMessage, ReadStringOrEmpty(eventPayload, Protocol.EventFields.ChatterUserName), ReadChatMessageText(eventPayload) ?? string.Empty)
            },
            Protocol.Events.ChannelPointRedemption => new TwitchEvent
            {
                Kind = TwitchEventKind.ChannelPointRedemption,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                RewardTitle = ReadRewardTitle(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleRedemption, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName), ReadRewardTitle(eventPayload) ?? string.Empty)
            },
            _ => null
        };
    }

    private static string? ReadChatMessageText(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty(Protocol.Json.Message, out var message))
        {
            return null;
        }

        return ReadString(message, Protocol.Json.Text);
    }

    private static string? ReadSubscriptionMessage(JsonElement eventPayload)
    {
        return eventPayload.TryGetProperty(Protocol.Json.Message, out var message)
            && message.TryGetProperty(Protocol.Json.Text, out var text)
            ? text.GetString()
            : null;
    }

    private string ReadSubscriptionGiftUserName(JsonElement eventPayload)
    {
        var userName = ReadString(eventPayload, Protocol.EventFields.UserName);
        return string.IsNullOrWhiteSpace(userName) ? _text.Get(UiTextKeys.TwitchEventSubAnonymousGiftUser) : userName;
    }

    private static string? ReadRewardTitle(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty(Protocol.Json.Reward, out var reward))
        {
            return null;
        }

        return ReadString(reward, Protocol.Json.Title);
    }

    private string ReadCheerUserName(JsonElement eventPayload)
    {
        var userName = ReadString(eventPayload, Protocol.EventFields.UserName);
        return string.IsNullOrWhiteSpace(userName) ? _text.Get(UiTextKeys.TwitchEventSubAnonymousCheerUser) : userName;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }

    private static string ReadStringOrEmpty(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName) ?? string.Empty;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;
    }
}

public sealed record EventSubSessionInfo(string SessionId, TimeSpan KeepaliveTimeout);
