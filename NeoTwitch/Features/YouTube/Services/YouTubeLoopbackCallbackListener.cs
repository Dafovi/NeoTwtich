using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NeoTwitch.Services.YouTube;

/// <summary>
/// Receives one OAuth redirect on a loopback-only socket. Keeping the listener bound from port
/// selection through the callback avoids HTTP.sys and the port reservation race of HttpListener.
/// </summary>
public sealed class YouTubeLoopbackCallbackListener : IAsyncDisposable
{
    private const int MaximumRequestBytes = 16 * 1024;
    private readonly TcpListener _listener;
    private int _disposed;

    private YouTubeLoopbackCallbackListener(TcpListener listener)
    {
        _listener = listener;
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        RedirectUri = new Uri($"http://127.0.0.1:{port}/");
    }

    public Uri RedirectUri { get; }

    public static YouTubeLoopbackCallbackListener Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start(backlog: 1);
            return new YouTubeLoopbackCallbackListener(listener);
        }
        catch
        {
            listener.Stop();
            throw;
        }
    }

    public async Task<string> WaitForAuthorizationCodeAsync(string expectedState, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedState);
        using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        var requestTarget = await ReadRequestTargetAsync(stream, cancellationToken);
        var query = ParseQuery(requestTarget);
        var state = GetQueryValue(query, "state");
        var error = GetQueryValue(query, "error");
        var code = GetQueryValue(query, "code");

        var accepted = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedState),
            Encoding.UTF8.GetBytes(state));
        var success = accepted && string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(code);
        await RespondAsync(stream, success, cancellationToken);

        if (!accepted)
        {
            throw new InvalidOperationException("La respuesta de YouTube no corresponde a la autorización iniciada por Neo Stream.");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("La autorización de YouTube fue cancelada o rechazada.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("YouTube no devolvió un código de autorización.");
        }

        return code;
    }

    private static async Task<string> ReadRequestTargetAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>(512);
        var buffer = new byte[512];
        while (bytes.Count < MaximumRequestBytes)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            bytes.AddRange(buffer.AsSpan(0, read).ToArray());
            if (bytes.Count >= 4
                && bytes[^4] == '\r'
                && bytes[^3] == '\n'
                && bytes[^2] == '\r'
                && bytes[^1] == '\n')
            {
                break;
            }
        }

        var request = Encoding.ASCII.GetString(CollectionsMarshal.AsSpan(bytes));
        var firstLineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
        var firstLine = firstLineEnd < 0 ? request : request[..firstLineEnd];
        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La respuesta local de YouTube no contiene una solicitud válida.");
        }

        return parts[1];
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string requestTarget)
    {
        if (!Uri.TryCreate("http://127.0.0.1" + requestTarget, UriKind.Absolute, out var uri))
        {
            return new Dictionary<string, string>();
        }

        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]),
                StringComparer.Ordinal);
    }

    private static string GetQueryValue(IReadOnlyDictionary<string, string> query, string name) =>
        query.TryGetValue(name, out var value) ? value : "";

    private static async Task RespondAsync(NetworkStream stream, bool success, CancellationToken cancellationToken)
    {
        const string successHtml = "<html><body style=\"font-family:Segoe UI;background:#07131f;color:#f8fafc;padding:32px\"><h2>Cuenta conectada</h2><p>Puedes volver a Neo Stream.</p></body></html>";
        const string failureHtml = "<html><body style=\"font-family:Segoe UI;background:#07131f;color:#f8fafc;padding:32px\"><h2>No se pudo conectar la cuenta</h2><p>Vuelve a Neo Stream e inténtalo de nuevo.</p></body></html>";
        var body = Encoding.UTF8.GetBytes(success ? successHtml : failureHtml);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {(success ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _listener.Stop();
        }

        return ValueTask.CompletedTask;
    }
}
