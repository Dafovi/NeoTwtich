using NeoTwitch.Models;
using NeoTwitch.Services.Streaming;

namespace NeoTwitch.Services.Dashboard;

public readonly record struct DashboardSummarySnapshot(
    int Followers,
    int Subscriptions,
    int Bits,
    int ChatMessages,
    int Events);

public sealed class DashboardSummaryService
{
    private int _followers;
    private int _subscriptions;
    private int _bits;
    private int _chatMessages;
    private int _events;

    public DashboardSummarySnapshot Snapshot => new(
        _followers,
        _subscriptions,
        _bits,
        _chatMessages,
        _events);

    public void RegisterMatchedRules(int count)
    {
        _events += Math.Max(0, count);
    }

    public void RegisterTwitchEvent(TwitchEvent twitchEvent)
    {
        RegisterStreamEvent(TwitchStreamEventAdapter.FromTwitch(twitchEvent));
    }

    public void RegisterStreamEvent(StreamEvent streamEvent)
    {
        switch (streamEvent.Kind)
        {
            case StreamEventKind.Follow:
                _followers++;
                break;
            case StreamEventKind.Subscription:
                _subscriptions++;
                break;
            case StreamEventKind.PlatformCurrency:
                _bits += Math.Max(0, streamEvent.ContributionUnits ?? 0);
                break;
            case StreamEventKind.ChatCommand:
                _chatMessages++;
                break;
        }
    }
}
