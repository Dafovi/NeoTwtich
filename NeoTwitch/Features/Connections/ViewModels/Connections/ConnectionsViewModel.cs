using System.Collections;
using NeoTwitch.Models;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Streaming;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Connections;

public sealed class ConnectionsViewModel : ObservableObject
{
    private Action _save = Noop;
    private Action _toggleTwitch = Noop;
    private Action _toggleYouTube = Noop;
    private Action _openTwitchConsole = Noop;
    private Action _toggleClientIdVisibility = Noop;
    private Action _toggleClientSecretVisibility = Noop;
    private Action _detectPorts = Noop;
    private Action _connectArduino = Noop;
    private Action _openAlexaConsole = Noop;
    private Action _testAlexa = Noop;
    private Action _toggleAlexaRelayUrlVisibility = Noop;
    private Action _toggleAlexaAuthTokenVisibility = Noop;
    private Action _openObsGuide = Noop;
    private Action _connectObs = Noop;
    private Action _testObs = Noop;
    private Action _toggleObsPasswordVisibility = Noop;
    private ConnectionBadgeViewModel _twitchBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _youTubeBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _arduinoBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _alexaBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionBadgeViewModel _obsBadge = ConnectionBadgeViewModel.From("Desconectado", "#F43F5E");
    private ConnectionButtonViewModel _twitchButton = ConnectionButtonViewModel.From("Conectar Twitch", "Plug", isEnabled: true);
    private ConnectionButtonViewModel _youTubeButton = ConnectionButtonViewModel.From("Conectar YouTube", "Plug", isEnabled: true);
    private ConnectionButtonViewModel _arduinoButton = ConnectionButtonViewModel.From("Conectar Arduino", "Plug", isEnabled: false);
    private ConnectionButtonViewModel _alexaTestButton = ConnectionButtonViewModel.From("Probar Alexa", "Play", isEnabled: false);
    private ConnectionButtonViewModel _obsButton = ConnectionButtonViewModel.From("Conectar OBS", "Plug", isEnabled: false);
    private ConnectionButtonViewModel _obsTestButton = ConnectionButtonViewModel.From("Actualizar escenas", "Refresh", isEnabled: false);
    private string _alexaStatusText = "";
    private string _obsConnectionHelpText = "";
    private string _twitchClientId = "";
    private string _twitchClientSecret = "";
    private string _youTubeClientSecret = "";
    private string _youTubeChannelDisplay = "Autoriza tu canal de YouTube desde el navegador.";
    private bool _twitchEnabled = true;
    private bool _youTubeEnabled;
    private bool _arduinoEnabled;
    private string _serialPort = "";
    private string _baudRateText = "115200";
    private bool _alexaEnabled;
    private string _alexaRelayUrl = "";
    private string _alexaAuthToken = "";
    private bool _obsEnabled;
    private string _obsHost = "127.0.0.1";
    private string _obsPortText = "4455";
    private string _obsPassword = "";
    private IEnumerable? _portChoices;

    public ConnectionsViewModel()
    {
        SaveCommand = new RelayCommand(() => _save());
        ToggleTwitchCommand = new RelayCommand(() => _toggleTwitch());
        ToggleYouTubeCommand = new RelayCommand(() => _toggleYouTube());
        OpenTwitchConsoleCommand = new RelayCommand(() => _openTwitchConsole());
        ToggleClientIdVisibilityCommand = new RelayCommand(() => _toggleClientIdVisibility());
        ToggleClientSecretVisibilityCommand = new RelayCommand(() => _toggleClientSecretVisibility());
        DetectPortsCommand = new RelayCommand(() => _detectPorts());
        ConnectArduinoCommand = new RelayCommand(() => _connectArduino());
        OpenAlexaConsoleCommand = new RelayCommand(() => _openAlexaConsole());
        TestAlexaCommand = new RelayCommand(() => _testAlexa());
        ToggleAlexaRelayUrlVisibilityCommand = new RelayCommand(() => _toggleAlexaRelayUrlVisibility());
        ToggleAlexaAuthTokenVisibilityCommand = new RelayCommand(() => _toggleAlexaAuthTokenVisibility());
        OpenObsGuideCommand = new RelayCommand(() => _openObsGuide());
        ConnectObsCommand = new RelayCommand(() => _connectObs());
        TestObsCommand = new RelayCommand(() => _testObs());
        ToggleObsPasswordVisibilityCommand = new RelayCommand(() => _toggleObsPasswordVisibility());
    }

    public RelayCommand SaveCommand { get; }

    public RelayCommand ToggleTwitchCommand { get; }

    public RelayCommand ToggleYouTubeCommand { get; }

    public RelayCommand OpenTwitchConsoleCommand { get; }

    public RelayCommand ToggleClientIdVisibilityCommand { get; }

    public RelayCommand ToggleClientSecretVisibilityCommand { get; }

    public RelayCommand DetectPortsCommand { get; }

    public RelayCommand ConnectArduinoCommand { get; }

    public RelayCommand OpenAlexaConsoleCommand { get; }

    public RelayCommand TestAlexaCommand { get; }

    public RelayCommand ToggleAlexaRelayUrlVisibilityCommand { get; }

    public RelayCommand ToggleAlexaAuthTokenVisibilityCommand { get; }

    public RelayCommand OpenObsGuideCommand { get; }

    public RelayCommand ConnectObsCommand { get; }

    public RelayCommand TestObsCommand { get; }

    public RelayCommand ToggleObsPasswordVisibilityCommand { get; }

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

    public ConnectionBadgeViewModel YouTubeBadge
    {
        get => _youTubeBadge;
        private set => SetProperty(ref _youTubeBadge, value);
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

    public ConnectionButtonViewModel YouTubeButton
    {
        get => _youTubeButton;
        private set => SetProperty(ref _youTubeButton, value);
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

    public IEnumerable? PortChoices
    {
        get => _portChoices;
        private set => SetProperty(ref _portChoices, value);
    }

    public string TwitchClientId
    {
        get => _twitchClientId;
        set => SetProperty(ref _twitchClientId, value ?? "");
    }

    public string TwitchClientSecret
    {
        get => _twitchClientSecret;
        set => SetProperty(ref _twitchClientSecret, value ?? "");
    }

    public string YouTubeClientSecret
    {
        get => _youTubeClientSecret;
        set => SetProperty(ref _youTubeClientSecret, value ?? "");
    }

    public string YouTubeChannelDisplay
    {
        get => _youTubeChannelDisplay;
        private set => SetProperty(ref _youTubeChannelDisplay, value);
    }

    public bool TwitchEnabled
    {
        get => _twitchEnabled;
        set => SetProperty(ref _twitchEnabled, value);
    }

    public bool YouTubeEnabled
    {
        get => _youTubeEnabled;
        set => SetProperty(ref _youTubeEnabled, value);
    }

    public bool ArduinoEnabled
    {
        get => _arduinoEnabled;
        set => SetProperty(ref _arduinoEnabled, value);
    }

    public string SerialPort
    {
        get => _serialPort;
        set => SetProperty(ref _serialPort, value ?? "");
    }

    public string BaudRateText
    {
        get => _baudRateText;
        set => SetProperty(ref _baudRateText, value ?? "");
    }

    public bool AlexaEnabled
    {
        get => _alexaEnabled;
        set => SetProperty(ref _alexaEnabled, value);
    }

    public string AlexaRelayUrl
    {
        get => _alexaRelayUrl;
        set => SetProperty(ref _alexaRelayUrl, value ?? "");
    }

    public string AlexaAuthToken
    {
        get => _alexaAuthToken;
        set => SetProperty(ref _alexaAuthToken, value ?? "");
    }

    public bool ObsEnabled
    {
        get => _obsEnabled;
        set => SetProperty(ref _obsEnabled, value);
    }

    public string ObsHost
    {
        get => _obsHost;
        set => SetProperty(ref _obsHost, value ?? "");
    }

    public string ObsPortText
    {
        get => _obsPortText;
        set => SetProperty(ref _obsPortText, value ?? "");
    }

    public string ObsPassword
    {
        get => _obsPassword;
        set => SetProperty(ref _obsPassword, value ?? "");
    }

    public void LoadTwitchConfig(AppConfig config)
    {
        TwitchClientId = config.TwitchClientId;
        TwitchClientSecret = config.TwitchClientSecret;
        YouTubeClientSecret = config.YouTubeClientSecret;
        TwitchEnabled = StreamingPlatformConfigurationService
            .GetOrCreate(config.StreamingPlatforms, StreamingPlatform.Twitch)
            .IsEnabled;
        YouTubeEnabled = StreamingPlatformConfigurationService
            .GetOrCreate(config.StreamingPlatforms, StreamingPlatform.YouTube)
            .IsEnabled;
        UpdateYouTubeChannel(config.YouTubeChannel);
    }

    public void UpdateYouTubeChannel(YouTubeChannelInfo channel)
    {
        YouTubeChannelDisplay = channel.IsReady
            ? "Cuenta autorizada. El canal aparece en la barra lateral."
            : "Autoriza tu canal de YouTube desde el navegador.";
    }

    public void UpdateYouTubeConnection(ConnectionStateVisual badge, ConnectionButtonState button)
    {
        YouTubeBadge = ConnectionBadgeViewModel.From(badge);
        YouTubeButton = ConnectionButtonViewModel.From(button);
    }

    public void LoadArduinoConfig(AppConfig config)
    {
        ArduinoEnabled = config.ArduinoEnabled;
        SerialPort = config.SerialPort;
        BaudRateText = config.BaudRate.ToString();
    }

    public void LoadAlexaConfig(AppConfig config)
    {
        AlexaEnabled = config.Alexa.Enabled;
        AlexaRelayUrl = config.Alexa.RelayUrl;
        AlexaAuthToken = config.Alexa.AuthToken;
    }

    public void LoadObsConnectionConfig(AppConfig config)
    {
        ObsEnabled = config.Obs.Enabled;
        ObsHost = config.Obs.Host;
        ObsPortText = config.Obs.Port.ToString();
        ObsPassword = config.Obs.Password;
    }

    public void UpdateBadges(
        ConnectionStateVisual twitch,
        ConnectionStateVisual youTube,
        ConnectionStateVisual arduino,
        ConnectionStateVisual alexa,
        ConnectionStateVisual obs)
    {
        TwitchBadge = ConnectionBadgeViewModel.From(twitch);
        YouTubeBadge = ConnectionBadgeViewModel.From(youTube);
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

    public void UpdatePortChoices(IEnumerable? ports)
    {
        PortChoices = ports;
    }

    public void ConfigureActions(
        Action save,
        Action toggleTwitch,
        Action toggleYouTube,
        Action openTwitchConsole,
        Action toggleClientIdVisibility,
        Action toggleClientSecretVisibility,
        Action detectPorts,
        Action connectArduino,
        Action openAlexaConsole,
        Action testAlexa,
        Action toggleAlexaRelayUrlVisibility,
        Action toggleAlexaAuthTokenVisibility,
        Action openObsGuide,
        Action connectObs,
        Action testObs,
        Action toggleObsPasswordVisibility)
    {
        _save = save;
        _toggleTwitch = toggleTwitch;
        _toggleYouTube = toggleYouTube;
        _openTwitchConsole = openTwitchConsole;
        _toggleClientIdVisibility = toggleClientIdVisibility;
        _toggleClientSecretVisibility = toggleClientSecretVisibility;
        _detectPorts = detectPorts;
        _connectArduino = connectArduino;
        _openAlexaConsole = openAlexaConsole;
        _testAlexa = testAlexa;
        _toggleAlexaRelayUrlVisibility = toggleAlexaRelayUrlVisibility;
        _toggleAlexaAuthTokenVisibility = toggleAlexaAuthTokenVisibility;
        _openObsGuide = openObsGuide;
        _connectObs = connectObs;
        _testObs = testObs;
        _toggleObsPasswordVisibility = toggleObsPasswordVisibility;
    }

    private static void Noop()
    {
    }
}
