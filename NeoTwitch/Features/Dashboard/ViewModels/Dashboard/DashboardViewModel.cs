using System.Collections;
using System.Windows.Input;
using System.Windows.Media;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
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
    private DashboardConnectionCardViewModel _twitchState = DashboardConnectionCardViewModel.From("Desconectado", "#F43F5E", "Assets/Icons/status_error.png");
    private DashboardConnectionCardViewModel _arduinoState = DashboardConnectionCardViewModel.From("Desconectado", "#F43F5E", "Assets/Icons/status_error.png");
    private DashboardConnectionCardViewModel _alexaState = DashboardConnectionCardViewModel.From("Desconectado", "#F43F5E", "Assets/Icons/status_error.png");
    private DashboardConnectionCardViewModel _obsState = DashboardConnectionCardViewModel.From("Desconectado", "#F43F5E", "Assets/Icons/status_error.png");

    public DashboardViewModel(Action goToActivity, IEnumerable? recentActivityEntries = null)
    {
        GoToActivityCommand = new RelayCommand(goToActivity);
        RecentActivityEntries = recentActivityEntries;
    }

    public ICommand GoToActivityCommand { get; }

    public IEnumerable? RecentActivityEntries { get; }

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

    public DashboardConnectionCardViewModel TwitchState
    {
        get => _twitchState;
        private set => SetProperty(ref _twitchState, value);
    }

    public DashboardConnectionCardViewModel ArduinoState
    {
        get => _arduinoState;
        private set => SetProperty(ref _arduinoState, value);
    }

    public DashboardConnectionCardViewModel AlexaState
    {
        get => _alexaState;
        private set => SetProperty(ref _alexaState, value);
    }

    public DashboardConnectionCardViewModel ObsState
    {
        get => _obsState;
        private set => SetProperty(ref _obsState, value);
    }

    public void UpdateSummary(DashboardSummaryDisplay display)
    {
        Followers = DashboardSummaryMetricViewModel.From(display.Followers);
        Subscriptions = DashboardSummaryMetricViewModel.From(display.Subscriptions);
        Bits = DashboardSummaryMetricViewModel.From(display.Bits);
        ChatMessages = DashboardSummaryMetricViewModel.From(display.ChatMessages);
        Events = DashboardSummaryMetricViewModel.From(display.Events);
    }

    public void UpdateConnectionStates(
        ConnectionStateVisual twitch,
        ConnectionStateVisual arduino,
        ConnectionStateVisual alexa,
        ConnectionStateVisual obs)
    {
        TwitchState = DashboardConnectionCardViewModel.From(twitch);
        ArduinoState = DashboardConnectionCardViewModel.From(arduino);
        AlexaState = DashboardConnectionCardViewModel.From(alexa);
        ObsState = DashboardConnectionCardViewModel.From(obs);
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

public sealed record DashboardConnectionCardViewModel(
    string Text,
    SolidColorBrush Brush,
    ImageBrush IconMask,
    string ToolTip)
{
    public static DashboardConnectionCardViewModel From(ConnectionStateVisual visual)
    {
        return From(visual.Text, visual.Color, visual.IconPath);
    }

    public static DashboardConnectionCardViewModel From(string text, string color, string iconPath)
    {
        return new DashboardConnectionCardViewModel(
            text,
            UiBrushFactory.FrozenBrushFrom(color),
            new ImageBrush
            {
                ImageSource = PackImageLoader.Load(iconPath),
                Stretch = Stretch.Uniform
            },
            text);
    }
}
