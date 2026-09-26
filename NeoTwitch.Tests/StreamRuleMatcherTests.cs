using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

public static class StreamRuleMatcherTests
{
    public static void ScopesEventsToSelectedPlatforms()
    {
        var rule = new EventRule
        {
            EventKind = TwitchEventKind.Subscription,
            SourcePlatforms = [StreamingPlatform.Twitch]
        };
        var twitch = CreateSubscription(StreamingPlatform.Twitch);
        var youtube = CreateSubscription(StreamingPlatform.YouTube);

        TestAssert.True(EventRuleMatcherService.Matches(rule, twitch));
        TestAssert.False(EventRuleMatcherService.Matches(rule, youtube));

        rule.SourcePlatforms = [StreamingPlatform.Twitch, StreamingPlatform.YouTube];
        TestAssert.True(EventRuleMatcherService.Matches(rule, youtube));

        rule.IsEnabled = false;
        TestAssert.False(EventRuleMatcherService.Matches(rule, youtube));
    }

    private static StreamEvent CreateSubscription(StreamingPlatform platform) => new(
        platform,
        StreamEventKind.Subscription,
        "event-1",
        DateTimeOffset.UnixEpoch,
        "Nueva suscripcion");
}
