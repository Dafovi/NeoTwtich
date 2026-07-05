using System.Net.WebSockets;
using System.IO;
using NeoTwitch.Models;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Text;
using ObsProtocol = NeoTwitch.Services.Obs.ObsWebSocketProtocol;

namespace NeoTwitch.Services;

public sealed partial class ObsWebSocketService : IAsyncDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(6);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IUiTextService _text;
    private readonly ObsWebSocketMessageFactory _messageFactory;
    private ClientWebSocket? _socket;

    public ObsWebSocketService(IUiTextService text, ObsWebSocketMessageFactory? messageFactory = null)
    {
        _text = text;
        _messageFactory = messageFactory ?? new ObsWebSocketMessageFactory();
    }

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public string Version { get; private set; } = "";

    public string CurrentScene { get; private set; } = "";

    public bool StudioMode { get; private set; }

    public IReadOnlyList<ObsSceneInfo> Scenes { get; private set; } = [];

    public async Task<ObsConnectionResult> ConnectAsync(ObsIntegrationConfig config, CancellationToken cancellationToken)
    {
        if (!config.IsConfigured)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsConnectNotConfigured));
        }

        return await ExecuteExclusiveAsync(
            cancellationToken,
            ConnectTimeout,
            UiTextKeys.ObsConnectTimeout,
            async token =>
            {
                await DisposeSocketAsync();

                _socket = new ClientWebSocket();
                await _socket.ConnectAsync(ObsWebSocketRequestFactory.BuildUri(config), token);

                using var hello = await ReceiveJsonAsync(token);
                if (ObsWebSocketResponseReader.ReadOperation(hello) != ObsProtocol.OpHello)
                {
                    throw new InvalidOperationException(_text.Get(UiTextKeys.ObsUnexpectedHello));
                }

                var rpcVersion = ObsWebSocketResponseReader.ReadRpcVersion(hello);
                var identify = new Dictionary<string, object?>
                {
                    [ObsProtocol.RpcVersion] = rpcVersion
                };

                if (ObsWebSocketResponseReader.TryReadAuthentication(hello, out var salt, out var challenge))
                {
                    if (string.IsNullOrWhiteSpace(config.Password))
                    {
                        throw new InvalidOperationException(_text.Get(UiTextKeys.ObsPasswordRequired));
                    }

                    identify[ObsProtocol.Authentication] = ObsWebSocketRequestFactory.BuildAuthentication(
                        config.Password,
                        salt,
                        challenge);
                }

                await SendAsync(_messageFactory.BuildIdentify(identify), token);
                using var identified = await ReceiveJsonAsync(token);
                if (ObsWebSocketResponseReader.ReadOperation(identified) != ObsProtocol.OpIdentified)
                {
                    throw new InvalidOperationException(_text.Get(UiTextKeys.ObsIdentificationFailure));
                }

                Version = await GetVersionAsync(token);
                await RefreshScenesCoreAsync(token);
                return Snapshot();
            },
            disposeOnFailure: true);
    }

    public async Task<ObsConnectionResult> RefreshScenesAsync(CancellationToken cancellationToken)
    {
        return await ExecuteExclusiveAsync(
            cancellationToken,
            RequestTimeout,
            UiTextKeys.ObsRefreshScenesTimeout,
            async token =>
            {
                EnsureConnected();
                await RefreshScenesCoreAsync(token);
                return Snapshot();
            });
    }

    public async Task<ObsConnectionResult> SetCurrentProgramSceneAsync(string sceneName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsSelectSceneFirst));
        }

        return await ExecuteExclusiveAsync(
            cancellationToken,
            RequestTimeout,
            UiTextKeys.ObsChangeSceneTimeout,
            async token =>
            {
                EnsureConnected();
                await SendRequestAsync(
                    ObsProtocol.SetCurrentProgramScene,
                    ObsWebSocketRequestFactory.BuildSetCurrentProgramSceneRequest(sceneName),
                    token);
                await RefreshScenesCoreAsync(token);
                return Snapshot();
            });
    }

    public async Task<ObsConnectionResult> ShowMediaSourceAsync(
        string sceneName,
        string sourceName,
        string filePath,
        ObsMediaKind kind,
        ObsIntegrationConfig? overlayConfig,
        int? videoVolumePercent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsSelectMediaScene));
        }

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsMissingSourceName));
        }

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsMissingMediaFile));
        }

        return await ExecuteExclusiveAsync(
            cancellationToken,
            RequestTimeout,
            UiTextKeys.ObsShowMediaTimeout,
            async token =>
            {
                EnsureConnected();
                try
                {
                    await SendRequestAsync(
                        ObsProtocol.CreateInput,
                        ObsWebSocketRequestFactory.BuildCreateInputRequest(sceneName, sourceName, kind, filePath),
                        token);
                }
                catch (InvalidOperationException)
                {
                    await SendRequestAsync(
                        ObsProtocol.SetInputSettings,
                        ObsWebSocketRequestFactory.BuildSetInputSettingsRequest(sourceName, kind, filePath),
                        token);
                    await EnsureSceneItemAsync(sceneName, sourceName, token);
                }

                await SetSceneItemEnabledAsync(sceneName, sourceName, enabled: true, token);
                if (kind == ObsMediaKind.Video && videoVolumePercent is int volumePercent)
                {
                    await SetInputVolumeAsync(sourceName, volumePercent, token);
                }

                if (overlayConfig is not null)
                {
                    await ApplySceneItemTransformAsync(sceneName, sourceName, overlayConfig, token);
                }

                await RefreshScenesCoreAsync(token);
                return Snapshot();
            });
    }

    public async Task<ObsConnectionResult> HideSceneSourceAsync(
        string sceneName,
        string sourceName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(sourceName))
        {
            return Snapshot();
        }

        return await ExecuteExclusiveAsync(
            cancellationToken,
            RequestTimeout,
            UiTextKeys.ObsHideMediaTimeout,
            async token =>
            {
                EnsureConnected();
                await SetSceneItemEnabledAsync(sceneName, sourceName, enabled: false, token);
                await RefreshScenesCoreAsync(token);
                return Snapshot();
            },
            invalidOperationFallback: Snapshot);
    }

    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await DisposeSocketAsync();
            Version = "";
            CurrentScene = "";
            StudioMode = false;
            Scenes = [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _gate.Dispose();
        await DisposeSocketAsync();
    }

    private async Task<string> GetVersionAsync(CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(ObsProtocol.GetVersion, null, cancellationToken);
        return ObsWebSocketResponseReader.ReadVersion(response);
    }

    private async Task RefreshScenesCoreAsync(CancellationToken cancellationToken)
    {
        using var sceneResponse = await SendRequestAsync(ObsProtocol.GetSceneList, null, cancellationToken);
        using var studioResponse = await SendRequestAsync(ObsProtocol.GetStudioModeEnabled, null, cancellationToken);
        var snapshot = ObsWebSocketResponseReader.ReadSceneSnapshot(sceneResponse, studioResponse);
        CurrentScene = snapshot.CurrentScene;
        Scenes = snapshot.Scenes;
        StudioMode = snapshot.StudioMode;
    }

    private async Task EnsureSceneItemAsync(string sceneName, string sourceName, CancellationToken cancellationToken)
    {
        try
        {
            _ = await GetSceneItemIdAsync(sceneName, sourceName, cancellationToken);
            return;
        }
        catch (InvalidOperationException)
        {
            // If the input already exists globally but is not in this scene, add it to the scene.
        }

        await SendRequestAsync(
            ObsProtocol.CreateSceneItem,
            ObsWebSocketRequestFactory.BuildCreateSceneItemRequest(sceneName, sourceName),
            cancellationToken);
    }

    private async Task SetSceneItemEnabledAsync(
        string sceneName,
        string sourceName,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var sceneItemId = await GetSceneItemIdAsync(sceneName, sourceName, cancellationToken);
        await SendRequestAsync(
            ObsProtocol.SetSceneItemEnabled,
            ObsWebSocketRequestFactory.BuildSetSceneItemEnabledRequest(sceneName, sceneItemId, enabled),
            cancellationToken);
    }

    private async Task ApplySceneItemTransformAsync(
        string sceneName,
        string sourceName,
        ObsIntegrationConfig config,
        CancellationToken cancellationToken)
    {
        var sceneItemId = await GetSceneItemIdAsync(sceneName, sourceName, cancellationToken);
        await SendRequestAsync(
            ObsProtocol.SetSceneItemTransform,
            ObsWebSocketRequestFactory.BuildSceneItemTransformRequest(sceneName, sceneItemId, config),
            cancellationToken);
    }

    private async Task SetInputVolumeAsync(
        string sourceName,
        int volumePercent,
        CancellationToken cancellationToken)
    {
        await SendRequestAsync(
            ObsProtocol.SetInputVolume,
            ObsWebSocketRequestFactory.BuildInputVolumeRequest(sourceName, volumePercent),
            cancellationToken);
    }

    private async Task<int> GetSceneItemIdAsync(
        string sceneName,
        string sourceName,
        CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(
            ObsProtocol.GetSceneItemId,
            ObsWebSocketRequestFactory.BuildGetSceneItemIdRequest(sceneName, sourceName),
            cancellationToken);
        return ObsWebSocketResponseReader.ReadSceneItemId(response);
    }

}
