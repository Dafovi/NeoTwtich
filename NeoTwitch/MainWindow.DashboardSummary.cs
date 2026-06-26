using NeoTwitch.Services.Dashboard;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateDashboardSummary()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateDashboardSummary);
            return;
        }

        var display = DashboardSummaryDisplayService.Build(_dashboardSummary.Snapshot);
        DashboardFollowersSummaryText.Text = display.Followers.Text;
        DashboardSubsSummaryText.Text = display.Subscriptions.Text;
        DashboardBitsSummaryText.Text = display.Bits.Text;
        DashboardChatSummaryText.Text = display.ChatMessages.Text;
        DashboardEventsSummaryText.Text = display.Events.Text;

        DashboardFollowersSummaryText.Foreground = FrozenBrushFrom(display.Followers.Color);
        DashboardSubsSummaryText.Foreground = FrozenBrushFrom(display.Subscriptions.Color);
        DashboardBitsSummaryText.Foreground = FrozenBrushFrom(display.Bits.Color);
        DashboardChatSummaryText.Foreground = FrozenBrushFrom(display.ChatMessages.Color);
        DashboardEventsSummaryText.Foreground = FrozenBrushFrom(display.Events.Color);

        RefreshDashboardConnectionStates();
    }
}
