using NeoTwitch.Models;
using NeoTwitch.Services.Streaming;

public static class StreamingPlatformTests
{
    public static void TwitchAdapterPreservesEventData()
    {
        var original = new TwitchEvent
        {
            Kind = TwitchEventKind.Cheer,
            Title = "Dafovi mando 100 bits",
            UserName = "Dafovi",
            Bits = 100,
            Message = "Vamos!",
            RawType = "channel.cheer",
            EventSubMessageId = "event-123",
            EventSubSessionId = "session-456",
            EventSubMessageType = "notification"
        };

        var streamEvent = TwitchStreamEventAdapter.FromTwitch(original, DateTimeOffset.UnixEpoch);
        var roundTrip = TwitchStreamEventAdapter.ToTwitch(streamEvent);

        TestAssert.Equal(StreamingPlatform.Twitch, streamEvent.Platform);
        TestAssert.Equal(StreamEventKind.PlatformCurrency, streamEvent.Kind);
        TestAssert.Equal("event-123", streamEvent.EventId);
        TestAssert.Equal(TwitchEventKind.Cheer, roundTrip.Kind);
        TestAssert.Equal(100, roundTrip.Bits);
        TestAssert.Equal("session-456", roundTrip.EventSubSessionId);
        TestAssert.Equal("notification", roundTrip.EventSubMessageType);
    }

    public static void RouterScopesDuplicateIdsByPlatform()
    {
        var router = new PlatformEventRouter(new PlatformEventDeduplicator(new FixedTimeProvider(DateTimeOffset.UnixEpoch)));
        var received = new List<StreamEvent>();
        router.EventReceivedAsync += (streamEvent, _) =>
        {
            received.Add(streamEvent);
            return Task.CompletedTask;
        };

        var twitch = CreateEvent(StreamingPlatform.Twitch, "same-id");
        var duplicateTwitch = CreateEvent(StreamingPlatform.Twitch, "same-id");
        var youtube = CreateEvent(StreamingPlatform.YouTube, "same-id");

        TestAssert.True(router.PublishAsync(twitch, CancellationToken.None).GetAwaiter().GetResult());
        TestAssert.False(router.PublishAsync(duplicateTwitch, CancellationToken.None).GetAwaiter().GetResult());
        TestAssert.True(router.PublishAsync(youtube, CancellationToken.None).GetAwaiter().GetResult());
        TestAssert.Equal(2, received.Count);
    }

    private static StreamEvent CreateEvent(StreamingPlatform platform, string eventId) => new(
        platform,
        StreamEventKind.Follow,
        eventId,
        DateTimeOffset.UnixEpoch,
        "Nuevo seguidor");
}
