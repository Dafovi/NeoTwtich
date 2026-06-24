using System.Net.WebSockets;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class ObsWebSocketService : IAsyncDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(6);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private ClientWebSocket? _socket;

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public string Version { get; private set; } = "";

    public string CurrentScene { get; private set; } = "";

    public bool StudioMode { get; private set; }

    public IReadOnlyList<ObsSceneInfo> Scenes { get; private set; } = [];

    public async Task<ObsConnectionResult> ConnectAsync(ObsIntegrationConfig config, CancellationToken cancellationToken)
    {
        if (!config.IsConfigured)
        {
            throw new InvalidOperationException("Activa OBS y configura host/puerto antes de conectar.");
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
            if (ReadInt(hello.RootElement, "op") != 0)
            {
                throw new InvalidOperationException("OBS no envio el saludo esperado de WebSocket.");
            }

            var helloData = hello.RootElement.GetProperty("d");
            var rpcVersion = ReadInt(helloData, "rpcVersion", 1);
            var identify = new Dictionary<string, object?>
            {
                ["rpcVersion"] = rpcVersion
            };

            if (helloData.TryGetProperty("authentication", out var auth))
            {
                if (string.IsNullOrWhiteSpace(config.Password))
                {
                    throw new InvalidOperationException("OBS solicita contraseña WebSocket. Escribela en Conexiones > OBS.");
                }

                identify["authentication"] = BuildAuthentication(
                    config.Password,
                    ReadString(auth, "salt"),
                    ReadString(auth, "challenge"));
            }

            await SendAsync(new { op = 1, d = identify }, token);
            using var identified = await ReceiveJsonAsync(token);
            if (ReadInt(identified.RootElement, "op") != 2)
            {
                throw new InvalidOperationException("OBS no confirmo la identificacion. Revisa la contraseña WebSocket.");
            }

            Version = await GetVersionAsync(token);
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException("OBS no respondio a tiempo. Revisa que OBS Studio este abierto y que WebSocket este activo.");
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
            throw new TimeoutException("OBS no respondio al actualizar escenas. Intenta reconectar OBS.");
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
            throw new InvalidOperationException("Selecciona una escena de OBS primero.");
        }

        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            await SendRequestAsync(
                "SetCurrentProgramScene",
                new Dictionary<string, object?> { ["sceneName"] = sceneName.Trim() },
                token);
            await RefreshScenesCoreAsync(token);
            return Snapshot();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException("OBS no respondio al cambiar escena. Intenta reconectar OBS.");
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
            throw new InvalidOperationException("Selecciona una escena OBS para mostrar el medio.");
        }

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            throw new InvalidOperationException("El source OBS de Neo Twitch no tiene nombre.");
        }

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new InvalidOperationException("El archivo que se enviara a OBS no existe.");
        }

        using var timeout = CreateTimeoutToken(cancellationToken, RequestTimeout);
        var token = timeout.Token;

        await _gate.WaitAsync(token);
        try
        {
            EnsureConnected();
            var inputKind = kind == ObsMediaKind.Image ? "image_source" : "ffmpeg_source";
            var settings = BuildMediaInputSettings(kind, filePath);
            try
            {
                await SendRequestAsync(
                    "CreateInput",
                    new Dictionary<string, object?>
                    {
                        ["sceneName"] = sceneName.Trim(),
                        ["inputName"] = sourceName.Trim(),
                        ["inputKind"] = inputKind,
                        ["inputSettings"] = settings,
                        ["sceneItemEnabled"] = true
                    },
                    token);
            }
            catch (InvalidOperationException)
            {
                await SendRequestAsync(
                    "SetInputSettings",
                    new Dictionary<string, object?>
                    {
                        ["inputName"] = sourceName.Trim(),
                        ["inputSettings"] = settings,
                        ["overlay"] = true
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
            throw new TimeoutException("OBS no respondio al mostrar el medio. Intenta reconectar OBS.");
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
            throw new TimeoutException("OBS no respondio al ocultar el medio. Intenta reconectar OBS.");
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
        using var response = await SendRequestAsync("GetVersion", null, cancellationToken);
        var data = response.RootElement.GetProperty("d").GetProperty("responseData");
        return ReadString(data, "obsVersion");
    }

    private async Task RefreshScenesCoreAsync(CancellationToken cancellationToken)
    {
        using var sceneResponse = await SendRequestAsync("GetSceneList", null, cancellationToken);
        var sceneData = sceneResponse.RootElement.GetProperty("d").GetProperty("responseData");
        CurrentScene = ReadString(sceneData, "currentProgramSceneName");
        Scenes = sceneData.GetProperty("scenes")
            .EnumerateArray()
            .Select(scene => new ObsSceneInfo(ReadString(scene, "sceneName")))
            .Where(scene => !string.IsNullOrWhiteSpace(scene.Name))
            .ToArray();

        using var studioResponse = await SendRequestAsync("GetStudioModeEnabled", null, cancellationToken);
        var studioData = studioResponse.RootElement.GetProperty("d").GetProperty("responseData");
        StudioMode = studioData.TryGetProperty("studioModeEnabled", out var enabled)
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
            "CreateSceneItem",
            new Dictionary<string, object?>
            {
                ["sceneName"] = sceneName.Trim(),
                ["sourceName"] = sourceName.Trim(),
                ["sceneItemEnabled"] = true
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
            "SetSceneItemEnabled",
            new Dictionary<string, object?>
            {
                ["sceneName"] = sceneName.Trim(),
                ["sceneItemId"] = sceneItemId,
                ["sceneItemEnabled"] = enabled
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
            "SetSceneItemTransform",
            new Dictionary<string, object?>
            {
                ["sceneName"] = sceneName.Trim(),
                ["sceneItemId"] = sceneItemId,
                ["sceneItemTransform"] = new Dictionary<string, object?>
                {
                    ["positionX"] = positionX,
                    ["positionY"] = positionY,
                    ["boundsType"] = "OBS_BOUNDS_SCALE_INNER",
                    ["boundsWidth"] = mediaWidth,
                    ["boundsHeight"] = mediaHeight
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
            "SetInputVolume",
            new Dictionary<string, object?>
            {
                ["inputName"] = sourceName.Trim(),
                ["inputVolumeMul"] = inputVolumeMul
            },
            cancellationToken);
    }

    private async Task<int> GetSceneItemIdAsync(
        string sceneName,
        string sourceName,
        CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(
            "GetSceneItemId",
            new Dictionary<string, object?>
            {
                ["sceneName"] = sceneName.Trim(),
                ["sourceName"] = sourceName.Trim()
            },
            cancellationToken);
        var data = response.RootElement.GetProperty("d").GetProperty("responseData");
        return ReadInt(data, "sceneItemId");
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
            ["requestType"] = requestType,
            ["requestId"] = requestId
        };

        if (requestData is not null)
        {
            payload["requestData"] = requestData;
        }

        await SendAsync(new { op = 6, d = payload }, cancellationToken);

        while (true)
        {
            var response = await ReceiveJsonAsync(cancellationToken);
            if (ReadInt(response.RootElement, "op") != 7)
            {
                response.Dispose();
                continue;
            }

            var data = response.RootElement.GetProperty("d");
            if (!string.Equals(ReadString(data, "requestId"), requestId, StringComparison.Ordinal))
            {
                response.Dispose();
                continue;
            }

            var status = data.GetProperty("requestStatus");
            if (!status.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.True)
            {
                var code = status.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : 0;
                var comment = ReadString(status, "comment");
                response.Dispose();
                throw new InvalidOperationException($"OBS rechazo {requestType} ({code}): {comment}");
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
                throw new InvalidOperationException("OBS cerro la conexion WebSocket.");
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
            throw new InvalidOperationException("OBS no esta conectado.");
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
            ? new Dictionary<string, object?> { ["file"] = filePath }
            : new Dictionary<string, object?>
            {
                ["is_local_file"] = true,
                ["local_file"] = filePath,
                ["looping"] = false,
                ["restart_on_activate"] = true,
                ["close_when_inactive"] = true
            };
    }

    private static (int X, int Y) ResolveOverlayPosition(ObsIntegrationConfig config, int mediaWidth, int mediaHeight)
    {
        var maxX = Math.Max(0, config.OverlayWidth - mediaWidth);
        var maxY = Math.Max(0, config.OverlayHeight - mediaHeight);
        return config.OverlayPositionMode switch
        {
            "Custom" => (Math.Clamp(config.OverlayX, 0, maxX), Math.Clamp(config.OverlayY, 0, maxY)),
            "Random" => (Random.Shared.Next(0, maxX + 1), Random.Shared.Next(0, maxY + 1)),
            _ => (maxX / 2, maxY / 2)
        };
    }

    private static Uri BuildUri(ObsIntegrationConfig config)
    {
        var host = config.Host.Trim();
        if (host.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(host);
        }

        return new Uri($"ws://{host}:{config.Port}");
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
