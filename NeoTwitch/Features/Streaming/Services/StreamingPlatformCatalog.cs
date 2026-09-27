using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public static class StreamingPlatformCatalog
{
    private static readonly IReadOnlyDictionary<StreamingPlatform, StreamingPlatformDescriptor> Descriptors =
        new Dictionary<StreamingPlatform, StreamingPlatformDescriptor>
        {
            [StreamingPlatform.Twitch] = new(
                StreamingPlatform.Twitch,
                "Twitch",
                StreamingPlatformCapabilities.FollowEvents
                | StreamingPlatformCapabilities.SubscriptionEvents
                | StreamingPlatformCapabilities.RaidEvents
                | StreamingPlatformCapabilities.PlatformCurrencyEvents
                | StreamingPlatformCapabilities.ChatMessages
                | StreamingPlatformCapabilities.ChatCommands
                | StreamingPlatformCapabilities.RewardRedemptions
                | StreamingPlatformCapabilities.LiveStatus,
                StreamingPlatformAvailability.BuiltIn),
            [StreamingPlatform.YouTube] = new(
                StreamingPlatform.YouTube,
                "YouTube",
                StreamingPlatformCapabilities.SubscriptionEvents
                | StreamingPlatformCapabilities.PlatformCurrencyEvents
                | StreamingPlatformCapabilities.ChatMessages
                | StreamingPlatformCapabilities.ChatCommands
                | StreamingPlatformCapabilities.LiveStatus,
                StreamingPlatformAvailability.BuiltIn),
            [StreamingPlatform.Kick] = new(
                StreamingPlatform.Kick,
                "Kick",
                StreamingPlatformCapabilities.FollowEvents
                | StreamingPlatformCapabilities.SubscriptionEvents
                | StreamingPlatformCapabilities.PlatformCurrencyEvents
                | StreamingPlatformCapabilities.ChatMessages
                | StreamingPlatformCapabilities.ChatCommands
                | StreamingPlatformCapabilities.RewardRedemptions
                | StreamingPlatformCapabilities.LiveStatus,
                StreamingPlatformAvailability.RequiresCloudRelay),
            [StreamingPlatform.TikTok] = new(
                StreamingPlatform.TikTok,
                "TikTok",
                StreamingPlatformCapabilities.None,
                StreamingPlatformAvailability.RequiresProviderApproval)
        };

    public static IReadOnlyCollection<StreamingPlatformDescriptor> All => Descriptors.Values.ToArray();

    public static StreamingPlatformDescriptor Get(StreamingPlatform platform) => Descriptors[platform];
}
