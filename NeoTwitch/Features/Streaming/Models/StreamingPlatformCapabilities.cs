namespace NeoTwitch.Models;

[Flags]
public enum StreamingPlatformCapabilities
{
    None = 0,
    FollowEvents = 1 << 0,
    SubscriptionEvents = 1 << 1,
    RaidEvents = 1 << 2,
    PlatformCurrencyEvents = 1 << 3,
    ChatMessages = 1 << 4,
    ChatCommands = 1 << 5,
    RewardRedemptions = 1 << 6,
    LiveStatus = 1 << 7
}

public enum StreamingPlatformAvailability
{
    BuiltIn,
    Planned,
    RequiresCloudRelay,
    RequiresProviderApproval
}

public sealed record StreamingPlatformDescriptor(
    StreamingPlatform Platform,
    string DisplayName,
    StreamingPlatformCapabilities Capabilities,
    StreamingPlatformAvailability Availability);
