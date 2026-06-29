using System.Windows.Media;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Connections;

public sealed class ConnectionsViewModel : ObservableObject
{
    private ConnectionBadgeViewModel _twitchBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _arduinoBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _alexaBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _obsBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private string _alexaStatusText = "";
    private string _obsConnectionHelpText = "";

    public ConnectionBadgeViewModel TwitchBadge
    {
        get => _twitchBadge;
        private set => SetProperty(ref _twitchBadge, value);
    }

    public ConnectionBadgeViewModel ArduinoBadge
    {
        get => _arduinoBadge;
        private set => SetProperty(ref _arduinoBadge, value);
    }

    public ConnectionBadgeViewModel AlexaBadge
    {
        get => _alexaBadge;
        private set => SetProperty(ref _alexaBadge, value);
    }

    public ConnectionBadgeViewModel ObsBadge
    {
        get => _obsBadge;
        private set => SetProperty(ref _obsBadge, value);
    }

    public string AlexaStatusText
    {
        get => _alexaStatusText;
        private set => SetProperty(ref _alexaStatusText, value);
    }

    public string ObsConnectionHelpText
    {
        get => _obsConnectionHelpText;
        private set => SetProperty(ref _obsConnectionHelpText, value);
    }

    public void UpdateBadges(
        ConnectionStateVisual twitch,
        ConnectionStateVisual arduino,
        ConnectionStateVisual alexa,
        ConnectionStateVisual obs)
    {
        TwitchBadge = ConnectionBadgeViewModel.From(twitch);
        ArduinoBadge = ConnectionBadgeViewModel.From(arduino);
        AlexaBadge = ConnectionBadgeViewModel.From(alexa);
        ObsBadge = ConnectionBadgeViewModel.From(obs);
    }

    public void UpdateAlexaStatusText(string statusText)
    {
        AlexaStatusText = statusText;
    }

    public void UpdateObsConnectionHelpText(string statusText)
    {
        ObsConnectionHelpText = statusText;
    }
}

public sealed record ConnectionBadgeViewModel(
    string Text,
    SolidColorBrush ForegroundBrush,
    SolidColorBrush BackgroundBrush,
    SolidColorBrush BorderBrush)
{
    public static ConnectionBadgeViewModel From(ConnectionStateVisual visual)
    {
        return From(visual.Text, visual.Color);
    }

    public static ConnectionBadgeViewModel From(string text, string color)
    {
        return new ConnectionBadgeViewModel(
            text,
            UiBrushFactory.FrozenBrushFrom(color),
            UiBrushFactory.TranslucentBrushFrom(color),
            UiBrushFactory.FrozenBrushFrom(color));
    }
}
