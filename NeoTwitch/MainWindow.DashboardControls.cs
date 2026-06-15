using System.Windows.Controls;

namespace NeoTwitch;

public partial class MainWindow
{
    private TextBlock DashboardTwitchStateText => DashboardView.DashboardTwitchStateText;
    private Border DashboardTwitchStatusIcon => DashboardView.DashboardTwitchStatusIcon;
    private TextBlock DashboardArduinoStateText => DashboardView.DashboardArduinoStateText;
    private Border DashboardArduinoStatusIcon => DashboardView.DashboardArduinoStatusIcon;
    private TextBlock DashboardAlexaStateText => DashboardView.DashboardAlexaStateText;
    private Border DashboardAlexaStatusIcon => DashboardView.DashboardAlexaStatusIcon;
    private TextBlock DashboardObsStateText => DashboardView.DashboardObsStateText;
    private Border DashboardObsStatusIcon => DashboardView.DashboardObsStatusIcon;
    private TextBlock DashboardFollowersSummaryText => DashboardView.DashboardFollowersSummaryText;
    private TextBlock DashboardSubsSummaryText => DashboardView.DashboardSubsSummaryText;
    private TextBlock DashboardBitsSummaryText => DashboardView.DashboardBitsSummaryText;
    private TextBlock DashboardChatSummaryText => DashboardView.DashboardChatSummaryText;
    private TextBlock DashboardEventsSummaryText => DashboardView.DashboardEventsSummaryText;
    private System.Windows.Controls.ListBox DashboardActivityList => DashboardView.DashboardActivityList;
}
