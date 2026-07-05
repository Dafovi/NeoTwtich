using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services.Navigation;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Shell;

public sealed class ShellViewModel : ObservableObject
{
    public const int DashboardTabIndex = 0;
    public const int ConnectionsTabIndex = 1;
    public const int AlertsTabIndex = 2;
    public const int LightsTabIndex = 3;
    public const int AlexaTabIndex = 4;
    public const int AudioTabIndex = 5;
    public const int ImagesTabIndex = 6;
    public const int VideosTabIndex = 7;
    public const int ObsTabIndex = 8;
    public const int SettingsTabIndex = 9;
    public const int ActivityTabIndex = 10;

    private readonly Func<int, bool> _navigate;
    private int _selectedTabIndex;
    private string _channelName = "";
    private string _channelLogin = "";
    private string _liveStateText = "";
    private string _topProfileText = "";
    private string _twitchConnectionText = "Sin conectar";
    private string _twitchStatusText = "";
    private string _arduinoConnectionText = "Sin conectar";
    private string _arduinoStatusText = "";
    private string _alexaConnectionText = "Sin conectar";
    private string _alexaSidebarStatusText = "";
    private SolidColorBrush _liveDotFill = UiBrushFactory.FrozenBrushFrom("#00000000");
    private SolidColorBrush _liveDotStroke = UiBrushFactory.FrozenBrushFrom("#94A3B8");
    private SolidColorBrush _liveStateBrush = UiBrushFactory.FrozenBrushFrom("#94A3B8");
    private SolidColorBrush _topProfileBrush = UiBrushFactory.FrozenBrushFrom("#F8FAFC");

    public ShellViewModel(IUiTextService text, Func<int, bool> navigate)
    {
        _navigate = navigate;
        NavigateCommand = new RelayCommand(parameter =>
        {
            if (TryReadTabIndex(parameter, out var tabIndex))
            {
                NavigateTo(tabIndex);
            }
        });

        Items =
        [
            new(DashboardTabIndex, "panel", text.Get(UiTextKeys.NavPanel), "Assets/Icons/nav_panel.png", text.Get(UiTextKeys.NavPanel)),
            new(ConnectionsTabIndex, "connections", text.Get(UiTextKeys.NavConnections), "Assets/Icons/nav_connections.png", text.Get(UiTextKeys.NavConnections)),
            new(AlertsTabIndex, "alerts", text.Get(UiTextKeys.NavAlerts), "Assets/Icons/nav_rules.png", text.Get(UiTextKeys.NavAlerts)),
            new(LightsTabIndex, "lights", text.Get(UiTextKeys.NavLights), "Assets/Icons/nav_lights.png", text.Get(UiTextKeys.NavLights)),
            new(AlexaTabIndex, "alexa", text.Get(UiTextKeys.NavAlexa), "Assets/Icons/nav_alexa.png", text.Get(UiTextKeys.NavAlexa)),
            new(AudioTabIndex, "audio", text.Get(UiTextKeys.NavAudio), "Assets/Icons/nav_audio.png", text.Get(UiTextKeys.NavAudio)),
            new(ImagesTabIndex, "images", text.Get(UiTextKeys.NavImages), "Assets/Icons/nav_images.png", text.Get(UiTextKeys.NavImages)),
            new(VideosTabIndex, "videos", text.Get(UiTextKeys.NavVideos), "Assets/Icons/nav_videos.png", text.Get(UiTextKeys.NavVideos)),
            new(ObsTabIndex, "obs", text.Get(UiTextKeys.NavObs), "Assets/Icons/nav_obs.png", text.Get(UiTextKeys.NavObs)),
            new(SettingsTabIndex, "settings", text.Get(UiTextKeys.NavConfiguration), "Assets/Icons/nav_settings.png", text.Get(UiTextKeys.NavConfiguration)),
            new(ActivityTabIndex, "activity", text.Get(UiTextKeys.NavActivity), "Assets/Icons/nav_activity.png", text.Get(UiTextKeys.NavActivity))
        ];

        VersionText = $"V{NeoTwitchProduct.CurrentVersionText}";
        UpdateChannel(text.Get(UiTextKeys.DashboardNoTwitch), text.Get(UiTextKeys.DashboardNoLogin));
        UpdateLiveIndicator(false, ThemePalette.Dark, text.Get(UiTextKeys.TwitchLive), text.Get(UiTextKeys.TwitchOffline), text.Get(UiTextKeys.TwitchProfile));
        UpdateSelectedItem();
    }

    public ObservableCollection<NavigationItemViewModel> Items { get; }

    public ICommand NavigateCommand { get; }

    public string VersionText { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        private set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                UpdateSelectedItem();
            }
        }
    }

    public string ChannelName
    {
        get => _channelName;
        private set => SetProperty(ref _channelName, value);
    }

    public string ChannelLogin
    {
        get => _channelLogin;
        private set => SetProperty(ref _channelLogin, value);
    }

    public string LiveStateText
    {
        get => _liveStateText;
        private set => SetProperty(ref _liveStateText, value);
    }

    public SolidColorBrush LiveDotFill
    {
        get => _liveDotFill;
        private set => SetProperty(ref _liveDotFill, value);
    }

    public SolidColorBrush LiveDotStroke
    {
        get => _liveDotStroke;
        private set => SetProperty(ref _liveDotStroke, value);
    }

    public SolidColorBrush LiveStateBrush
    {
        get => _liveStateBrush;
        private set => SetProperty(ref _liveStateBrush, value);
    }

    public string TopProfileText
    {
        get => _topProfileText;
        private set => SetProperty(ref _topProfileText, value);
    }

    public SolidColorBrush TopProfileBrush
    {
        get => _topProfileBrush;
        private set => SetProperty(ref _topProfileBrush, value);
    }

    public string TwitchConnectionText
    {
        get => _twitchConnectionText;
        private set => SetProperty(ref _twitchConnectionText, value);
    }

    public string TwitchStatusText
    {
        get => _twitchStatusText;
        private set => SetProperty(ref _twitchStatusText, value);
    }

    public string ArduinoConnectionText
    {
        get => _arduinoConnectionText;
        private set => SetProperty(ref _arduinoConnectionText, value);
    }

    public string ArduinoStatusText
    {
        get => _arduinoStatusText;
        private set => SetProperty(ref _arduinoStatusText, value);
    }

    public string AlexaConnectionText
    {
        get => _alexaConnectionText;
        private set => SetProperty(ref _alexaConnectionText, value);
    }

    public string AlexaSidebarStatusText
    {
        get => _alexaSidebarStatusText;
        private set => SetProperty(ref _alexaSidebarStatusText, value);
    }

    public NavigationItemViewModel? FindByIndex(int tabIndex)
    {
        return Items.FirstOrDefault(item => item.TabIndex == tabIndex);
    }

    public void NavigateTo(int tabIndex)
    {
        if (tabIndex == SelectedTabIndex)
        {
            return;
        }

        if (_navigate(tabIndex))
        {
            SelectedTabIndex = tabIndex;
        }
    }

    public void SyncSelectedTab(int tabIndex)
    {
        SelectedTabIndex = tabIndex;
    }

    public void ApplyServiceVisibility(AppConfig config)
    {
        var visibility = ServiceNavigationVisibilityService.Resolve(config);
        SetVisible(LightsTabIndex, visibility.Lights);
        SetVisible(AlexaTabIndex, visibility.Alexa);
        SetVisible(ObsTabIndex, visibility.Obs);
        SetVisible(ImagesTabIndex, visibility.Images);
        SetVisible(VideosTabIndex, visibility.Videos);

        if (FindByIndex(SelectedTabIndex) is { IsVisible: false })
        {
            NavigateTo(ConnectionsTabIndex);
        }
    }

    public void UpdateChannel(string name, string login)
    {
        ChannelName = name;
        ChannelLogin = login;
    }

    public void UpdateServiceStatusText(
        string? twitchConnection = null,
        string? twitchStatus = null,
        string? arduinoConnection = null,
        string? arduinoStatus = null,
        string? alexaConnection = null,
        string? alexaSidebarStatus = null)
    {
        if (twitchConnection is not null)
        {
            TwitchConnectionText = twitchConnection;
        }

        if (twitchStatus is not null)
        {
            TwitchStatusText = twitchStatus;
        }

        if (arduinoConnection is not null)
        {
            ArduinoConnectionText = arduinoConnection;
        }

        if (arduinoStatus is not null)
        {
            ArduinoStatusText = arduinoStatus;
        }

        if (alexaConnection is not null)
        {
            AlexaConnectionText = alexaConnection;
        }

        if (alexaSidebarStatus is not null)
        {
            AlexaSidebarStatusText = alexaSidebarStatus;
        }
    }

    public void UpdateLiveIndicator(bool isLive, ThemePalette palette, string liveText, string offlineText, string profileText)
    {
        TopProfileText = profileText;
        TopProfileBrush = palette.Text;

        if (isLive)
        {
            var liveBrush = UiBrushFactory.FrozenBrushFrom("#FF2D55");
            LiveDotFill = liveBrush;
            LiveDotStroke = liveBrush;
            LiveStateText = liveText;
            LiveStateBrush = liveBrush;
            return;
        }

        LiveDotFill = UiBrushFactory.FrozenBrushFrom("#00000000");
        LiveDotStroke = palette.SidebarText;
        LiveStateText = offlineText;
        LiveStateBrush = palette.SidebarText;
    }

    private void SetVisible(int tabIndex, bool isVisible)
    {
        if (FindByIndex(tabIndex) is { } item)
        {
            item.IsVisible = isVisible;
        }
    }

    private void UpdateSelectedItem()
    {
        foreach (var item in Items)
        {
            item.IsSelected = item.TabIndex == SelectedTabIndex;
        }
    }

    private static bool TryReadTabIndex(object? parameter, out int tabIndex)
    {
        switch (parameter)
        {
            case int value:
                tabIndex = value;
                return true;
            case string text when int.TryParse(text, out var value):
                tabIndex = value;
                return true;
            case NavigationItemViewModel item:
                tabIndex = item.TabIndex;
                return true;
            default:
                tabIndex = -1;
                return false;
        }
    }
}
