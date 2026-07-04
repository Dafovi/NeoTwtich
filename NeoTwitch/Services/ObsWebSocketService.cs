using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Text;
using ObsProtocol = NeoTwitch.Services.Obs.ObsWebSocketProtocol;

namespace NeoTwitch.Services;

public sealed class ObsWebSocketService : IAsyncDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(6);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IUiTextService _text;
    private ClientWebSocket? _socket;

    public ObsWebSocketService()
        : this(UiTextService.CreateDefault())
    {
    }

    public ObsWebSocketService(IUiTextService text)
    {
        _text = text;
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

        using var timeout = CreateTimeoutToken(cancellationToken, ConnectTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
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

            await SendAsync(new { op = ObsProtocol.OpIdentify, d = identify }, token);
            using var identified = await ReceiveJsonAsync(token);
            if (ObsWebSocketResponseReader.ReadOperation(identified) != ObsProtocol.OpIdentified)
            {
                throw new InvalidOperationException(_text.Get(UiTextKeys.ObsIdentificationFailure));
            }

            Version = await GetVersionAsync(token);
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(UiTextKeys.ObsConnectTimeout));
        }
        catch
        {
            await DisposeSocketAsync();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ObsConnectionResult> RefreshScenesAsync(CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(UiTextKeys.ObsRefreshScenesTimeout));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ObsConnectionResult> SetCurrentProgramSceneAsync(string sceneName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsSelectSceneFirst));
        }

        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            await SendRequestAsync(
                ObsProtocol.SetCurrentProgramScene,
                new Dictionary<string, object?> { [ObsProtocol.SceneName] = sceneName.Trim() },
                token);
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(UiTextKeys.ObsChangeSceneTimeout));
        }
        finally
        {
            _gate.Release();
        }
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

        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            var inputKind = kind == ObsMediaKind.Image ? ObsProtocol.ImageSourceKind : ObsProtocol.FfmpegSourceKind;
            var settings = ObsWebSocketRequestFactory.BuildMediaInputSettings(kind, filePath);
            try
            {
                await SendRequestAsync(
                    ObsProtocol.CreateInput,
                    new Dictionary<string, object?>
                    {
                        [ObsProtocol.SceneName] = sceneName.Trim(),
                        [ObsProtocol.InputName] = sourceName.Trim(),
                        [ObsProtocol.InputKind] = inputKind,
                        [ObsProtocol.InputSettings] = settings,
                        [ObsProtocol.SceneItemEnabled] = true
                    },
                    token);
            }
            catch (InvalidOperationException)
            {
                await SendRequestAsync(
                    ObsProtocol.SetInputSettings,
                    new Dictionary<string, object?>
                    {
                        [ObsProtocol.InputName] = sourceName.Trim(),
                        [ObsProtocol.InputSettings] = settings,
                        [ObsProtocol.Overlay] = true
                    },
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
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(UiTextKeys.ObsShowMediaTimeout));
        }
        finally
        {
            _gate.Release();
        }
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

        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            await SetSceneItemEnabledAsync(sceneName, sourceName, enabled: false, token);
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (InvalidOperationException)
        {
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(UiTextKeys.ObsHideMediaTimeout));
        }
        finally
        {
            _gate.Release();
        }
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
            new Dictionary<string, object?>
            {
                [ObsProtocol.SceneName] = sceneName.Trim(),
                [ObsProtocol.SourceName] = sourceName.Trim(),
                [ObsProtocol.SceneItemEnabled] = true
            },
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
            new Dictionary<string, object?>
            {
                [ObsProtocol.SceneName] = sceneName.Trim(),
                [ObsProtocol.SceneItemId] = sceneItemId,
                [ObsProtocol.SceneItemEnabled] = enabled
            },
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
            new Dictionary<string, object?>
            {
                [ObsProtocol.SceneName] = sceneName.Trim(),
                [ObsProtocol.SourceName] = sourceName.Trim()
            },
            cancellationToken);
        return ObsWebSocketResponseReader.ReadSceneItemId(response);
    }

    private async Task<JsonDocument> SendRequestAsync(
        string requestType,
        Dictionary<string, object?>? requestData,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var requestId = Guid.NewGuid().ToString("N");
        var payload = new Dictionary<string, object?>
        {
            [ObsProtocol.RequestType] = requestType,
            [ObsProtocol.RequestId] = requestId
        };

        if (requestData is not null)
        {
            payload[ObsProtocol.RequestData] = requestData;
        }

        await SendAsync(new { op = ObsProtocol.OpRequest, d = payload }, cancellationToken);

        while (true)
        {
            var response = await ReceiveJsonAsync(cancellationToken);
            if (ObsWebSocketResponseReader.ReadOperation(response) != ObsProtocol.OpRequestResponse)
            {
                response.Dispose();
                continue;
            }

            if (!string.Equals(ObsWebSocketResponseReader.ReadRequestId(response), requestId, StringComparison.Ordinal))
            {
                response.Dispose();
                continue;
            }

            var status = ObsWebSocketResponseReader.ReadRequestStatus(response);
            if (!status.Succeeded)
            {
                response.Dispose();
                throw new InvalidOperationException(_text.Format(UiTextKeys.ObsRequestRejected, requestType, status.Code, status.Comment));
            }

            return response;
        }
    }

    private async Task SendAsync<T>(T payload, CancellationToken cancellationToken)
    {
        EnsureConnected();
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket!.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<JsonDocument> ReceiveJsonAsync(CancellationToken cancellationToken)
    {
        EnsureConnected();

        using var stream = new MemoryStream();
        var buffer = new byte[8192];
        WebSocketReceiveResult result;
        do
        {
            result = await _socket!.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException(_text.Get(UiTextKeys.ObsSocketClosed));
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private void EnsureConnected()
    {
        if (_socket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.ObsNotConnected));
        }
    }

    private async Task DisposeSocketAsync()
    {
        if (_socket is null)
        {
            return;
        }

        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Neo Twitch", cts.Token);
            }
        }
        catch
        {
            // Closing a half-open socket should not block reconnect attempts.
        }
        finally
        {
            _socket.Dispose();
            _socket = null;
            ClearSnapshot();
        }
    }

    private void ClearSnapshot()
    {
        Version = "";
        CurrentScene = "";
        StudioMode = false;
        Scenes = [];
    }

    private static CancellationTokenSource CreateTimeoutToken(CancellationToken cancellationToken, TimeSpan timeout)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        return timeoutCts;
    }

    private ObsConnectionResult Snapshot()
    {
        return new ObsConnectionResult(IsConnected, Version, CurrentScene, StudioMode, Scenes);
    }

}
