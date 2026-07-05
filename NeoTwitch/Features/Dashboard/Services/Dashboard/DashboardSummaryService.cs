using NeoTwitch.Models;

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
        switch (twitchEvent.Kind)
        {
            case TwitchEventKind.Follow:
                _followers++;
                break;
            case TwitchEventKind.Subscription:
                _subscriptions++;
                break;
            case TwitchEventKind.Cheer:
                _bits += Math.Max(0, twitchEvent.Bits ?? 0);
                break;
            case TwitchEventKind.ChatCommand:
                _chatMessages++;
                break;
        }
    }
}
