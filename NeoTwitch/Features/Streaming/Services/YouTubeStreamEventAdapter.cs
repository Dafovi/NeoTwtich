using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public static class YouTubeStreamEventAdapter
{
    public static StreamEvent? FromLiveChatMessage(YouTubeLiveChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var kind = message.Type switch
        {
            "textMessageEvent" => StreamEventKind.ChatCommand,
            "superChatEvent" or "superStickerEvent" => StreamEventKind.PlatformCurrency,
            "newSponsorEvent" or "memberMilestoneChatEvent" or "membershipGiftingEvent" or "giftMembershipReceivedEvent" => StreamEventKind.Subscription,
            _ => (StreamEventKind?)null
        };
        if (kind is null)
        {
            return null;
        }

        var title = kind switch
        {
            StreamEventKind.ChatCommand => $"{DisplayUser(message)} escribio {message.Message}",
            StreamEventKind.PlatformCurrency => $"{DisplayUser(message)} envio un Super Chat {message.ContributionDisplay}",
            StreamEventKind.Subscription => $"{DisplayUser(message)} se unio como miembro",
            _ => "Evento de YouTube"
        };
        var metadata = new Dictionary<string, string>
        {
            ["messageType"] = message.Type,
            ["currency"] = message.Currency,
            ["amountMicros"] = message.AmountMicros?.ToString() ?? "",
            ["amountDisplay"] = message.ContributionDisplay,
            ["membershipLevel"] = message.MembershipLevel
        };

        return new StreamEvent(
            StreamingPlatform.YouTube,
            kind.Value,
            message.Id,
            message.PublishedAt,
            title,
            message.UserName,
            message.MembershipLevel,
            ContributionUnits: message.ContributionTier,
            Message: message.Message,
            RawType: message.Type,
            Metadata: metadata);
    }

    private static string DisplayUser(YouTubeLiveChatMessage message) =>
        string.IsNullOrWhiteSpace(message.UserName) ? "Alguien" : message.UserName;
}
