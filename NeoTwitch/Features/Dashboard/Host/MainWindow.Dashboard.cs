using NeoTwitch.Models;
using NeoTwitch.Services.Streaming;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RegisterDashboardMatchedRules(int count)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardMatchedRules(count));
            return;
        }

        _dashboardSummary.RegisterMatchedRules(count);
        UpdateDashboardSummary();
    }

    private void RegisterDashboardTwitchEvent(TwitchEvent twitchEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardTwitchEvent(twitchEvent));
            return;
        }

        _dashboardSummary.RegisterTwitchEvent(twitchEvent);
        UpdateDashboardSummary();
    }

    private void RegisterDashboardStreamEvent(StreamEvent streamEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardStreamEvent(streamEvent));
            return;
        }

        _dashboardSummary.RegisterStreamEvent(streamEvent);
        UpdateDashboardSummary();
    }

}
