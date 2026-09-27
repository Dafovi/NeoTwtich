namespace NeoTwitch.Models;

public sealed record YouTubeLiveChatMessage(
    string Id,
    string Type,
    DateTimeOffset PublishedAt,
    string UserName,
    string Message,
    int? ContributionTier,
    string ContributionDisplay,
    string Currency,
    long? AmountMicros,
    string MembershipLevel);

public sealed record YouTubeLiveChatPage(
    IReadOnlyList<YouTubeLiveChatMessage> Messages,
    string NextPageToken,
    TimeSpan PollingInterval,
    bool IsOffline);
