using System.Collections.ObjectModel;
using NeoTwitch.Services.Status;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Obs;

public sealed class ObsViewModel : ObservableObject
{
    private Action _copyOverlayUrl = Noop;
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
    private bool _isScenesEnabled;
    private double _scenesOpacity = 0.58d;

    public ObsViewModel()
    {
        CopyOverlayUrlCommand = new RelayCommand(() => _copyOverlayUrl());
        RefreshScenesCommand = new RelayCommand(() => _refreshScenes());
        PreviewSceneCommand = new RelayCommand(parameter => _previewScene(parameter));
        ChangeSceneCommand = new RelayCommand(parameter => _changeScene(parameter));
    }

    public ObservableCollection<ObsSceneRow> SceneRows { get; } = [];

    public RelayCommand CopyOverlayUrlCommand { get; }

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

    public void ConfigureActions(
        Action copyOverlayUrl,
        Action refreshScenes,
        Action<object?> previewScene,
        Action<object?> changeScene)
    {
        _copyOverlayUrl = copyOverlayUrl;
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
}
