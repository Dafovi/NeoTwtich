using System.Net.WebSockets;
using System.IO;
using System.Text;

namespace NeoTwitch.Services;

internal interface IEventSubWebSocket : IAsyncDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken);
    void Abort();
}

internal sealed class EventSubWebSocket : IEventSubWebSocket
{
    private readonly ClientWebSocket _socket = new();

    public WebSocketState State => _socket.State;
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _socket.ConnectAsync(uri, cancellationToken);
    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        _socket.ReceiveAsync(buffer, cancellationToken);
    public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken) =>
        _socket.CloseAsync(status, description, cancellationToken);
    public void Abort() => _socket.Abort();
    public ValueTask DisposeAsync()
    {
        _socket.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class EventSubMessageAccumulator
{
    public const int MaximumMessageBytes = 256 * 1024;
    private readonly MemoryStream _stream = new();

    public int Length => checked((int)_stream.Length);

    public void Append(ReadOnlySpan<byte> fragment)
    {
        if (fragment.Length > MaximumMessageBytes - _stream.Length)
        {
            throw new EventSubMessageTooLargeException(MaximumMessageBytes);
        }

        _stream.Write(fragment);
    }

    public string GetText()
    {
        if (!_stream.TryGetBuffer(out var buffer))
        {
            throw new InvalidOperationException("No se pudo leer el mensaje EventSub acumulado.");
        }

        return Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, checked((int)_stream.Length));
    }
}

internal sealed class EventSubMessageTooLargeException(int maximumBytes)
    : IOException($"El mensaje EventSub excede el límite de {maximumBytes} bytes.");
