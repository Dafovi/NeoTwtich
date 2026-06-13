using System.Net.WebSockets;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class ObsWebSocketService : IAsyncDisposable
{
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

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await DisposeSocketAsync();

            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(BuildUri(config), cancellationToken);

            using var hello = await ReceiveJsonAsync(cancellationToken);
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

            if (helloData.TryGetProperty("authentication", out var auth)
                && !string.IsNullOrWhiteSpace(config.Password))
            {
                identify["authentication"] = BuildAuthentication(
                    config.Password,
                    ReadString(auth, "salt"),
                    ReadString(auth, "challenge"));
            }

            await SendAsync(new { op = 1, d = identify }, cancellationToken);
            using var identified = await ReceiveJsonAsync(cancellationToken);
            if (ReadInt(identified.RootElement, "op") != 2)
            {
                throw new InvalidOperationException("OBS no confirmo la identificacion. Revisa la contraseña WebSocket.");
            }

            Version = await GetVersionAsync(cancellationToken);
            await RefreshScenesCoreAsync(cancellationToken);
            return Snapshot();
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
        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            await RefreshScenesCoreAsync(cancellationToken);
            return Snapshot();
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

        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            await SendRequestAsync(
                "SetCurrentProgramScene",
                new Dictionary<string, object?> { ["sceneName"] = sceneName.Trim() },
                cancellationToken);
            await RefreshScenesCoreAsync(cancellationToken);
            return Snapshot();
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
        }
    }

    private ObsConnectionResult Snapshot()
    {
        return new ObsConnectionResult(IsConnected, Version, CurrentScene, StudioMode, Scenes);
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
