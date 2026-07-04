using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Text;
using ObsProtocol = NeoTwitch.Services.Obs.ObsWebSocketProtocol;

namespace NeoTwitch.Services;

public sealed partial class ObsWebSocketService
{
    private async Task<JsonDocument> SendRequestAsync(
        string requestType,
        Dictionary<string, object?>? requestData,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var request = _messageFactory.BuildRequest(requestType, requestData);
        await SendAsync(request.Message, cancellationToken);

        while (true)
        {
            var response = await ReceiveJsonAsync(cancellationToken);
            if (ObsWebSocketResponseReader.ReadOperation(response) != ObsProtocol.OpRequestResponse)
            {
                response.Dispose();
                continue;
            }

            if (!string.Equals(ObsWebSocketResponseReader.ReadRequestId(response), request.RequestId, StringComparison.Ordinal))
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

    private async Task<ObsConnectionResult> ExecuteExclusiveAsync(
        CancellationToken cancellationToken,
        TimeSpan timeout,
        string timeoutMessageKey,
        Func<CancellationToken, Task<ObsConnectionResult>> action,
        bool disposeOnFailure = false,
        Func<ObsConnectionResult>? invalidOperationFallback = null)
    {
        using var timeoutToken = CreateTimeoutToken(cancellationToken, timeout);
        var token = timeoutToken.Token;

        await _gate.WaitAsync(token);
        try
        {
            return await action(token);
        }
        catch (InvalidOperationException) when (invalidOperationFallback is not null)
        {
            return invalidOperationFallback();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync();
            throw new TimeoutException(_text.Get(timeoutMessageKey));
        }
        catch
        {
            if (disposeOnFailure)
            {
                await DisposeSocketAsync();
            }

            throw;
        }
        finally
        {
            _gate.Release();
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
