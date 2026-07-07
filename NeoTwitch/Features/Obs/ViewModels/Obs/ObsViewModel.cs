using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Status;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Obs;

public sealed class ObsViewModel : ObservableObject
{
    private Action _copyOverlayUrl = Noop;
    private Action _copyVirtualLightsOverlayUrl = Noop;
    private Action _refreshScenes = Noop;
    private Action<object?> _previewScene = Noop;
    private Action<object?> _changeScene = Noop;
    private string _connectionState = "Desconectado";
    private string _statusText = "";
    private string _currentScene = "Sin escena";
    private string _host = "127.0.0.1";
    private string _port = "4455";
    private string _version = "Sin version";
    private string _sceneCount = "0";
    private string _studioMode = "Desactivado";
    private string _overlayUrl = "";
    private string _virtualLightsOverlayUrl = "";
    private string _overlayWidthText = "1920";
    private string _overlayHeightText = "1080";
    private string _overlayMediaWidthText = "640";
    private string _overlayMediaHeightText = "360";
    private string _overlayPositionMode = "Center";
    private string _overlayXText = "0";
    private string _overlayYText = "0";
    private bool _isScenesEnabled;
    private double _scenesOpacity = 0.58d;
    private bool _isCustomOverlayPosition;
    private double _overlayCoordinateOpacity = 0.58d;

    public ObsViewModel()
    {
        CopyOverlayUrlCommand = new RelayCommand(() => _copyOverlayUrl());
        CopyVirtualLightsOverlayUrlCommand = new RelayCommand(() => _copyVirtualLightsOverlayUrl());
        RefreshScenesCommand = new RelayCommand(() => _refreshScenes());
        PreviewSceneCommand = new RelayCommand(parameter => _previewScene(parameter));
        ChangeSceneCommand = new RelayCommand(parameter => _changeScene(parameter));
    }

    public ObservableCollection<ObsSceneRow> SceneRows { get; } = [];

    public RelayCommand CopyOverlayUrlCommand { get; }

    public RelayCommand CopyVirtualLightsOverlayUrlCommand { get; }

    public RelayCommand RefreshScenesCommand { get; }

    public RelayCommand PreviewSceneCommand { get; }

    public RelayCommand ChangeSceneCommand { get; }

    public string ConnectionState
    {
        get => _connectionState;
        private set => SetProperty(ref _connectionState, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string CurrentScene
    {
        get => _currentScene;
        private set => SetProperty(ref _currentScene, value);
    }

    public string Host
    {
        get => _host;
        private set => SetProperty(ref _host, value);
    }

    public string Port
    {
        get => _port;
        private set => SetProperty(ref _port, value);
    }

    public string Version
    {
        get => _version;
        private set => SetProperty(ref _version, value);
    }

    public string SceneCount
    {
        get => _sceneCount;
        private set => SetProperty(ref _sceneCount, value);
    }

    public string StudioMode
    {
        get => _studioMode;
        private set => SetProperty(ref _studioMode, value);
    }

    public bool IsScenesEnabled
    {
        get => _isScenesEnabled;
        private set => SetProperty(ref _isScenesEnabled, value);
    }

    public double ScenesOpacity
    {
        get => _scenesOpacity;
        private set => SetProperty(ref _scenesOpacity, value);
    }

    public string OverlayUrl
    {
        get => _overlayUrl;
        private set => SetProperty(ref _overlayUrl, value);
    }

    public string VirtualLightsOverlayUrl
    {
        get => _virtualLightsOverlayUrl;
        private set => SetProperty(ref _virtualLightsOverlayUrl, value);
    }

    public string OverlayWidthText
    {
        get => _overlayWidthText;
        set => SetProperty(ref _overlayWidthText, value ?? "");
    }

    public string OverlayHeightText
    {
        get => _overlayHeightText;
        set => SetProperty(ref _overlayHeightText, value ?? "");
    }

    public string OverlayMediaWidthText
    {
        get => _overlayMediaWidthText;
        set => SetProperty(ref _overlayMediaWidthText, value ?? "");
    }

    public string OverlayMediaHeightText
    {
        get => _overlayMediaHeightText;
        set => SetProperty(ref _overlayMediaHeightText, value ?? "");
    }

    public string OverlayPositionMode
    {
        get => _overlayPositionMode;
        set
        {
            if (SetProperty(ref _overlayPositionMode, value ?? "Center"))
            {
                UpdateOverlayPositionState();
            }
        }
    }

    public string OverlayXText
    {
        get => _overlayXText;
        set => SetProperty(ref _overlayXText, value ?? "");
    }

    public string OverlayYText
    {
        get => _overlayYText;
        set => SetProperty(ref _overlayYText, value ?? "");
    }

    public bool IsCustomOverlayPosition
    {
        get => _isCustomOverlayPosition;
        private set => SetProperty(ref _isCustomOverlayPosition, value);
    }

    public double OverlayCoordinateOpacity
    {
        get => _overlayCoordinateOpacity;
        private set => SetProperty(ref _overlayCoordinateOpacity, value);
    }

    public void UpdateStatus(ObsStatusText status, bool isScenesEnabled)
    {
        ConnectionState = status.State;
        StatusText = status.StatusText;
        CurrentScene = status.CurrentScene;
        Host = status.Host;
        Port = status.Port;
        Version = status.Version;
        SceneCount = status.SceneCount;
        StudioMode = status.StudioMode;
        IsScenesEnabled = isScenesEnabled;
        ScenesOpacity = isScenesEnabled ? 1d : 0.58d;
    }

    public void ReplaceScenes(IEnumerable<ObsSceneRow> rows)
    {
        SceneRows.Clear();
        foreach (var row in rows)
        {
            SceneRows.Add(row);
        }
    }

    public void ClearScenes()
    {
        SceneRows.Clear();
    }

    public void LoadOverlayConfig(AppConfig config, string overlayUrl, string virtualLightsOverlayUrl)
    {
        OverlayUrl = overlayUrl;
        VirtualLightsOverlayUrl = virtualLightsOverlayUrl;
        OverlayWidthText = config.Obs.OverlayWidth.ToString();
        OverlayHeightText = config.Obs.OverlayHeight.ToString();
        OverlayMediaWidthText = config.Obs.OverlayMediaWidth.ToString();
        OverlayMediaHeightText = config.Obs.OverlayMediaHeight.ToString();
        OverlayPositionMode = config.Obs.OverlayPositionMode;
        OverlayXText = config.Obs.OverlayX.ToString();
        OverlayYText = config.Obs.OverlayY.ToString();
        UpdateOverlayPositionState();
    }

    public void UpdateOverlayUrl(string overlayUrl, string virtualLightsOverlayUrl)
    {
        OverlayUrl = overlayUrl;
        VirtualLightsOverlayUrl = virtualLightsOverlayUrl;
        UpdateOverlayPositionState();
    }

    public void ConfigureActions(
        Action copyOverlayUrl,
        Action copyVirtualLightsOverlayUrl,
        Action refreshScenes,
        Action<object?> previewScene,
        Action<object?> changeScene)
    {
        _copyOverlayUrl = copyOverlayUrl;
        _copyVirtualLightsOverlayUrl = copyVirtualLightsOverlayUrl;
        _refreshScenes = refreshScenes;
        _previewScene = previewScene;
        _changeScene = changeScene;
    }

    private static void Noop()
    {
    }

    private static void Noop(object? _)
    {
    }

    private void UpdateOverlayPositionState()
    {
        var customPosition = string.Equals(OverlayPositionMode, "Custom", StringComparison.OrdinalIgnoreCase);
        IsCustomOverlayPosition = customPosition;
        OverlayCoordinateOpacity = customPosition ? 1d : 0.58d;
    }
}
