namespace NeoTwitch.Services.Dashboard;

public sealed record DashboardSummaryMetricDisplay(string Text, string Color);

public sealed record DashboardSummaryDisplay(
    DashboardSummaryMetricDisplay Followers,
    DashboardSummaryMetricDisplay Subscriptions,
    DashboardSummaryMetricDisplay Bits,
    DashboardSummaryMetricDisplay ChatMessages,
    DashboardSummaryMetricDisplay Events);

public static class DashboardSummaryDisplayService
{
    public static DashboardSummaryDisplay Build(DashboardSummarySnapshot summary)
    {
        return new DashboardSummaryDisplay(
            new DashboardSummaryMetricDisplay($"+{summary.Followers}", "#14B8A6"),
            new DashboardSummaryMetricDisplay($"+{summary.Subscriptions}", "#B56CFF"),
            new DashboardSummaryMetricDisplay($"+{summary.Bits}", "#37C7F3"),
            new DashboardSummaryMetricDisplay(summary.ChatMessages.ToString(), "#22C55E"),
            new DashboardSummaryMetricDisplay(summary.Events.ToString(), "#84CC16"));
    }
}
