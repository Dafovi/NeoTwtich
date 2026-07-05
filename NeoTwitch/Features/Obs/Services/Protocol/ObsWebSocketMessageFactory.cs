using System.Text.Json.Serialization;

namespace NeoTwitch.Services.Obs;

public sealed class ObsWebSocketMessageFactory
{
    private readonly Func<string> _requestIdFactory;

    public ObsWebSocketMessageFactory(Func<string>? requestIdFactory = null)
    {
        _requestIdFactory = requestIdFactory ?? (() => Guid.NewGuid().ToString("N"));
    }

    public ObsWebSocketMessage BuildIdentify(Dictionary<string, object?> identifyData)
    {
        return new ObsWebSocketMessage(ObsWebSocketProtocol.OpIdentify, identifyData);
    }

    public ObsWebSocketRequestMessage BuildRequest(
        string requestType,
        Dictionary<string, object?>? requestData)
    {
        var requestId = _requestIdFactory();
        var data = new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.RequestType] = requestType,
            [ObsWebSocketProtocol.RequestId] = requestId
        };

        if (requestData is not null)
        {
            data[ObsWebSocketProtocol.RequestData] = requestData;
        }

        return new ObsWebSocketRequestMessage(
            requestId,
            new ObsWebSocketMessage(ObsWebSocketProtocol.OpRequest, data));
    }
}

public sealed record ObsWebSocketRequestMessage(string RequestId, ObsWebSocketMessage Message);

public sealed record ObsWebSocketMessage(
    [property: JsonPropertyName(ObsWebSocketProtocol.Op)] int Operation,
    [property: JsonPropertyName(ObsWebSocketProtocol.Data)] Dictionary<string, object?> Data);
