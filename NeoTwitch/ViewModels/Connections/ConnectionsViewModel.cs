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
    private ConnectionButtonViewModel _twitchButton = ConnectionButtonViewModel.From("Conectar Twitch", "Plug", isEnabled: true);
    private ConnectionButtonViewModel _arduinoButton = ConnectionButtonViewModel.From("Conectar Arduino", "Plug", isEnabled: false);
    private ConnectionButtonViewModel _alexaTestButton = ConnectionButtonViewModel.From("Probar Alexa", "Play", isEnabled: false);
    private ConnectionButtonViewModel _obsButton = ConnectionButtonViewModel.From("Conectar OBS", "Plug", isEnabled: false);
    private ConnectionButtonViewModel _obsTestButton = ConnectionButtonViewModel.From("Actualizar escenas", "Refresh", isEnabled: false);
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

    public ConnectionButtonViewModel TwitchButton
    {
        get => _twitchButton;
        private set => SetProperty(ref _twitchButton, value);
    }

    public ConnectionButtonViewModel ArduinoButton
    {
        get => _arduinoButton;
        private set => SetProperty(ref _arduinoButton, value);
    }

    public ConnectionButtonViewModel AlexaTestButton
    {
        get => _alexaTestButton;
        private set => SetProperty(ref _alexaTestButton, value);
    }

    public ConnectionButtonViewModel ObsButton
    {
        get => _obsButton;
        private set => SetProperty(ref _obsButton, value);
    }

    public ConnectionButtonViewModel ObsTestButton
    {
        get => _obsTestButton;
        private set => SetProperty(ref _obsTestButton, value);
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

    public void UpdateButtonStates(
        ConnectionButtonState twitch,
        ConnectionButtonState arduino,
        ConnectionButtonState alexaTest,
        ConnectionButtonState obs,
        ConnectionButtonState obsTest)
    {
        TwitchButton = ConnectionButtonViewModel.From(twitch);
        ArduinoButton = ConnectionButtonViewModel.From(arduino);
        AlexaTestButton = ConnectionButtonViewModel.From(alexaTest);
        ObsButton = ConnectionButtonViewModel.From(obs);
        ObsTestButton = ConnectionButtonViewModel.From(obsTest);
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

public sealed record ConnectionButtonViewModel(
    bool IsEnabled,
    string Text,
    Geometry IconGeometry)
{
    public static ConnectionButtonViewModel From(ConnectionButtonState state)
    {
        return From(state.Content, state.IconKey, state.IsEnabled);
    }

    public static ConnectionButtonViewModel From(string text, string iconKey, bool isEnabled)
    {
        var geometry = Geometry.Parse(IconPathCatalog.Get(iconKey));
        geometry.Freeze();
        return new ConnectionButtonViewModel(isEnabled, text, geometry);
    }
}
