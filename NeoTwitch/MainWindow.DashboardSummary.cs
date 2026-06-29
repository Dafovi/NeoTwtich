using NeoTwitch.Services.Dashboard;

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
        _dashboardViewModel.UpdateSummary(display);

        RefreshDashboardConnectionStates();
    }
}
