using System.Windows.Input;
using System.Windows.Media;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Dashboard;

public sealed class DashboardViewModel : ObservableObject
{
    private DashboardSummaryMetricViewModel _followers = DashboardSummaryMetricViewModel.From("+0", "#14B8A6");
    private DashboardSummaryMetricViewModel _subscriptions = DashboardSummaryMetricViewModel.From("+0", "#B56CFF");
    private DashboardSummaryMetricViewModel _bits = DashboardSummaryMetricViewModel.From("+0", "#37C7F3");
    private DashboardSummaryMetricViewModel _chatMessages = DashboardSummaryMetricViewModel.From("0", "#22C55E");
    private DashboardSummaryMetricViewModel _events = DashboardSummaryMetricViewModel.From("0", "#84CC16");

    public DashboardViewModel(Action goToActivity)
    {
        GoToActivityCommand = new RelayCommand(goToActivity);
    }

    public ICommand GoToActivityCommand { get; }

    public DashboardSummaryMetricViewModel Followers
    {
        get => _followers;
        private set => SetProperty(ref _followers, value);
    }

    public DashboardSummaryMetricViewModel Subscriptions
    {
        get => _subscriptions;
        private set => SetProperty(ref _subscriptions, value);
    }

    public DashboardSummaryMetricViewModel Bits
    {
        get => _bits;
        private set => SetProperty(ref _bits, value);
    }

    public DashboardSummaryMetricViewModel ChatMessages
    {
        get => _chatMessages;
        private set => SetProperty(ref _chatMessages, value);
    }

    public DashboardSummaryMetricViewModel Events
    {
        get => _events;
        private set => SetProperty(ref _events, value);
    }

    public void UpdateSummary(DashboardSummaryDisplay display)
    {
        Followers = DashboardSummaryMetricViewModel.From(display.Followers);
        Subscriptions = DashboardSummaryMetricViewModel.From(display.Subscriptions);
        Bits = DashboardSummaryMetricViewModel.From(display.Bits);
        ChatMessages = DashboardSummaryMetricViewModel.From(display.ChatMessages);
        Events = DashboardSummaryMetricViewModel.From(display.Events);
    }
}

public sealed record DashboardSummaryMetricViewModel(string Text, SolidColorBrush Brush)
{
    public static DashboardSummaryMetricViewModel From(DashboardSummaryMetricDisplay display)
    {
        return From(display.Text, display.Color);
    }

    public static DashboardSummaryMetricViewModel From(string text, string color)
    {
        return new DashboardSummaryMetricViewModel(text, UiBrushFactory.FrozenBrushFrom(color));
    }
}
