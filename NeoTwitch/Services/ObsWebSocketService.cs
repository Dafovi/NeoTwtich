using System.Net.WebSockets;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
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
            await _socket.ConnectAsync(BuildUri(config), token);

            using var hello = await ReceiveJsonAsync(token);
            if (ReadInt(hello.RootElement, ObsProtocol.Op) != ObsProtocol.OpHello)
            {
                throw new InvalidOperationException(_text.Get(UiTextKeys.ObsUnexpectedHello));
            }

            var helloData = hello.RootElement.GetProperty(ObsProtocol.Data);
            var rpcVersion = ReadInt(helloData, ObsProtocol.RpcVersion, 1);
            var identify = new Dictionary<string, object?>
            {
                [ObsProtocol.RpcVersion] = rpcVersion
            };

            if (helloData.TryGetProperty(ObsProtocol.Authentication, out var auth))
            {
                if (string.IsNullOrWhiteSpace(config.Password))
                {
                    throw new InvalidOperationException(_text.Get(UiTextKeys.ObsPasswordRequired));
                }

                identify[ObsProtocol.Authentication] = BuildAuthentication(
                    config.Password,
                    ReadString(auth, ObsProtocol.Salt),
                    ReadString(auth, ObsProtocol.Challenge));
            }

            await SendAsync(new { op = ObsProtocol.OpIdentify, d = identify }, token);
            using var identified = await ReceiveJsonAsync(token);
            if (ReadInt(identified.RootElement, ObsProtocol.Op) != ObsProtocol.OpIdentified)
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
            var settings = BuildMediaInputSettings(kind, filePath);
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
        var data = response.RootElement.GetProperty(ObsProtocol.Data).GetProperty(ObsProtocol.ResponseData);
        return ReadString(data, ObsProtocol.ObsVersion);
    }

    private async Task RefreshScenesCoreAsync(CancellationToken cancellationToken)
    {
        using var sceneResponse = await SendRequestAsync(ObsProtocol.GetSceneList, null, cancellationToken);
        var sceneData = sceneResponse.RootElement.GetProperty(ObsProtocol.Data).GetProperty(ObsProtocol.ResponseData);
        CurrentScene = ReadString(sceneData, ObsProtocol.CurrentProgramSceneName);
        Scenes = sceneData.GetProperty(ObsProtocol.Scenes)
            .EnumerateArray()
            .Select(scene => new ObsSceneInfo(ReadString(scene, ObsProtocol.SceneName)))
            .Where(scene => !string.IsNullOrWhiteSpace(scene.Name))
            .ToArray();

        using var studioResponse = await SendRequestAsync(ObsProtocol.GetStudioModeEnabled, null, cancellationToken);
        var studioData = studioResponse.RootElement.GetProperty(ObsProtocol.Data).GetProperty(ObsProtocol.ResponseData);
        StudioMode = studioData.TryGetProperty(ObsProtocol.StudioModeEnabled, out var enabled)
            && enabled.ValueKind == JsonValueKind.True;
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
        var mediaWidth = Math.Clamp(
            config.OverlayMediaWidth,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayWidth));
        var mediaHeight = Math.Clamp(
            config.OverlayMediaHeight,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayHeight));
        var (positionX, positionY) = ResolveOverlayPosition(config, mediaWidth, mediaHeight);

        await SendRequestAsync(
            ObsProtocol.SetSceneItemTransform,
            new Dictionary<string, object?>
            {
                [ObsProtocol.SceneName] = sceneName.Trim(),
                [ObsProtocol.SceneItemId] = sceneItemId,
                [ObsProtocol.SceneItemTransform] = new Dictionary<string, object?>
                {
                    [ObsProtocol.PositionX] = positionX,
                    [ObsProtocol.PositionY] = positionY,
                    [ObsProtocol.BoundsType] = ObsProtocol.BoundsScaleInner,
                    [ObsProtocol.BoundsWidth] = mediaWidth,
                    [ObsProtocol.BoundsHeight] = mediaHeight
                }
            },
            cancellationToken);
    }

    private async Task SetInputVolumeAsync(
        string sourceName,
        int volumePercent,
        CancellationToken cancellationToken)
    {
        var inputVolumeMul = Math.Clamp(volumePercent, ApplicationLimits.MinVolumePercent, ApplicationLimits.MaxVolumePercent) / 100d;
        await SendRequestAsync(
            ObsProtocol.SetInputVolume,
            new Dictionary<string, object?>
            {
                [ObsProtocol.InputName] = sourceName.Trim(),
                [ObsProtocol.InputVolumeMul] = inputVolumeMul
            },
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
        var data = response.RootElement.GetProperty(ObsProtocol.Data).GetProperty(ObsProtocol.ResponseData);
        return ReadInt(data, ObsProtocol.SceneItemId);
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
            if (ReadInt(response.RootElement, ObsProtocol.Op) != ObsProtocol.OpRequestResponse)
            {
                response.Dispose();
                continue;
            }

            var data = response.RootElement.GetProperty(ObsProtocol.Data);
            if (!string.Equals(ReadString(data, ObsProtocol.RequestId), requestId, StringComparison.Ordinal))
            {
                response.Dispose();
                continue;
            }

            var status = data.GetProperty(ObsProtocol.RequestStatus);
            if (!status.TryGetProperty(ObsProtocol.RequestResult, out var result) || result.ValueKind != JsonValueKind.True)
            {
                var code = status.TryGetProperty(ObsProtocol.RequestCode, out var codeElement) ? codeElement.GetInt32() : 0;
                var comment = ReadString(status, ObsProtocol.RequestComment);
                response.Dispose();
                throw new InvalidOperationException(_text.Format(UiTextKeys.ObsRequestRejected, requestType, code, comment));
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

    private static Dictionary<string, object?> BuildMediaInputSettings(ObsMediaKind kind, string filePath)
    {
        return kind == ObsMediaKind.Image
            ? new Dictionary<string, object?> { [ObsProtocol.ImageFile] = filePath }
            : new Dictionary<string, object?>
            {
                [ObsProtocol.IsLocalFile] = true,
                [ObsProtocol.LocalFile] = filePath,
                [ObsProtocol.Looping] = false,
                [ObsProtocol.RestartOnActivate] = true,
                [ObsProtocol.CloseWhenInactive] = true
            };
    }

    private static (int X, int Y) ResolveOverlayPosition(ObsIntegrationConfig config, int mediaWidth, int mediaHeight)
    {
        var maxX = Math.Max(0, config.OverlayWidth - mediaWidth);
        var maxY = Math.Max(0, config.OverlayHeight - mediaHeight);
        return config.OverlayPositionMode switch
        {
            ObsProtocol.CustomPositionMode => (Math.Clamp(config.OverlayX, 0, maxX), Math.Clamp(config.OverlayY, 0, maxY)),
            ObsProtocol.RandomPositionMode => (Random.Shared.Next(0, maxX + 1), Random.Shared.Next(0, maxY + 1)),
            _ => (maxX / 2, maxY / 2)
        };
    }

    private static Uri BuildUri(ObsIntegrationConfig config)
    {
        var host = config.Host.Trim();
        if (host.StartsWith(ObsProtocol.WebSocketScheme, StringComparison.OrdinalIgnoreCase)
            || host.StartsWith(ObsProtocol.SecureWebSocketScheme, StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(host);
        }

        return new Uri($"{ObsProtocol.WebSocketScheme}{host}:{config.Port}");
    }

    private static string BuildAuthentication(string password, string salt, string challenge)
    {
        var secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
    }

    private static int ReadInt(JsonElement element, string propertyName, int fallback = 0)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : fallback;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }
}
