using System.IO;
using System.Net.WebSockets;
using System.Text.Json;

namespace NeoTwitch.Services.Obs;

public sealed class ObsWebSocketMessageTooLargeException(int maximumBytes)
    : IOException($"OBS WebSocket message exceeded the {maximumBytes}-byte limit.");

public static class ObsWebSocketFrameReader
{
    public const int MaximumMessageBytes = 1_048_576;
    private const int ReceiveBufferBytes = 8192;

    public static async Task<JsonDocument> ReceiveJsonAsync(
        Func<ArraySegment<byte>, CancellationToken, Task<WebSocketReceiveResult>> receiveAsync,
        Action abortTransport,
        string closedMessage,
        CancellationToken cancellationToken,
        int maximumMessageBytes = MaximumMessageBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumMessageBytes, 1);

        using var stream = new MemoryStream(Math.Min(maximumMessageBytes, ReceiveBufferBytes));
        var buffer = new byte[Math.Min(maximumMessageBytes, ReceiveBufferBytes)];
        WebSocketReceiveResult result;
        do
        {
            result = await receiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException(closedMessage);
            }

            if (result.Count < 0 || result.Count > buffer.Length)
            {
                abortTransport();
                throw new InvalidDataException("OBS WebSocket returned an invalid frame length.");
            }

            if (stream.Length > maximumMessageBytes - result.Count)
            {
                abortTransport();
                throw new ObsWebSocketMessageTooLargeException(maximumMessageBytes);
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
