using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Integrations;

/// <summary>Session-scoped loopback transport. No Twitch/OBS credentials are exposed to browser code.</summary>
public sealed class SubtitleServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly string _key = Guid.NewGuid().ToString("N");
    private readonly string? _project;
    private readonly SubtitleIntegrationConfig _config;
    private readonly object _sync = new();
    private Task? _loop;
    private int _disposed;
    private string _original = "", _translation = "", _status = "Esperando…";
    private bool _listening;
    private long _updatedAt;
    private DateTimeOffset _lastContact;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public string BaseUrl { get; private set; } = "";
    public string OverlayUrl => BaseUrl + "overlay";
    public string Status { get { lock (_sync) return _status; } }
    public bool HasContact { get { lock (_sync) return DateTimeOffset.UtcNow - _lastContact < TimeSpan.FromSeconds(6); } }

    public SubtitleServer(SubtitleIntegrationConfig config, string? captionsProject = null)
    {
        _config = config.Snapshot();
        _project = captionsProject;
    }
    public void Start()
    {
        _listener.Start(16);
        BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/{_key}/";
        _loop = Task.Run(ListenAsync);
    }
    public void SetText(string original, string translation)
    {
        lock (_sync)
        {
            _original = original[..Math.Min(original.Length, 4000)];
            _translation = translation[..Math.Min(translation.Length, 4000)];
            _updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    public void SetStatus(string status) { lock (_sync) _status = status; }
    public static string Asset(string name)
    {
        var assembly = typeof(SubtitleServer).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".Assets." + name, StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        return reader.ReadToEnd();
    }
    private async Task ListenAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await HandleAsync(client.GetStream(), timeout.Token);
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or JsonException or FormatException or InvalidOperationException)
            { }
        }
    }
    private async Task HandleAsync(NetworkStream stream, CancellationToken token)
    {
        using var header = new MemoryStream();
        var one = new byte[1];
        var end = 0;
        while (header.Length < 8192 && end != 4)
        {
            if (await stream.ReadAsync(one, token) == 0) return;
            header.WriteByte(one[0]);
            end = one[0] == "\r\n\r\n"[end] ? end + 1 : (one[0] == '\r' ? 1 : 0);
        }
        if (end != 4) return;
        var lines = Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");
        var request = lines[0].Split(' ');
        if (request.Length != 3) return;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon > 0) headers[line[..colon]] = line[(colon + 1)..].Trim();
        }
        var origin = new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
        if (headers.GetValueOrDefault("Host") != new Uri(BaseUrl).Authority ||
            (headers.TryGetValue("Origin", out var suppliedOrigin) && suppliedOrigin != origin) ||
            !request[1].StartsWith($"/{_key}/", StringComparison.Ordinal))
        { await ReplyAsync(stream, 403, "text/plain", "Acceso denegado", token); return; }
        var route = request[1][($"/{_key}/".Length)..];
        if (request[0] == "POST" && route == "push" && _project is not null)
        {
            if (headers.GetValueOrDefault("Content-Type") != "application/json" ||
                !int.TryParse(headers.GetValueOrDefault("Content-Length"), out var length) || length < 0 || length > 32768)
            { await ReplyAsync(stream, 400, "text/plain", "Solicitud inválida", token); return; }
            var body = new byte[length];
            await stream.ReadExactlyAsync(body, token);
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            string Read(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
            if (Read("kind") == "text") SetText(Read("original"), Read("translation"));
            lock (_sync)
            {
                _lastContact = DateTimeOffset.UtcNow;
                _status = Read("status")[..Math.Min(Read("status").Length, 300)];
                _listening = root.TryGetProperty("listening", out var value) && value.ValueKind == JsonValueKind.True;
            }
            await ReplyAsync(stream, 200, "application/json", "{}", token);
            return;
        }
        if (request[0] != "GET") { await ReplyAsync(stream, 405, "text/plain", "", token); return; }
        if (route == "config")
            await ReplyAsync(stream, 200, "application/json", JsonSerializer.Serialize(_config, JsonOptions), token);
        else if (route == "state")
        {
            string state;
            lock (_sync) state = JsonSerializer.Serialize(new { original = _original, translation = _translation, updatedAt = _updatedAt, status = _status, listening = _listening }, JsonOptions);
            await ReplyAsync(stream, 200, "application/json", state, token);
        }
        else if (route == "overlay") await ReplyAsync(stream, 200, "text/html; charset=utf-8", Asset("subtitle-overlay.html"), token);
        else if (route == "captions" && _project is not null) await ReplyAsync(stream, 200, "text/html; charset=utf-8", Asset("captions.html"), token);
        else if (_project is not null && new[] { "recognizer.js", "translator.js", "chrome_translator.js" }.Contains(route))
            await ReplyAsync(stream, 200, "text/javascript; charset=utf-8", await File.ReadAllTextAsync(Path.Combine(_project, "v2", "js", route), token), token);
        else await ReplyAsync(stream, 404, "text/plain", "No encontrado", token);
    }
    private static async Task ReplyAsync(NetworkStream stream, int status, string type, string text, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Response\r\nContent-Type: {type}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\n\r\n");
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(bytes, token);
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel();
        _listener.Stop();
        if (_loop is not null) await _loop;
        _stop.Dispose();
    }
}
