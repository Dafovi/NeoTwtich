using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Obs;

public sealed record ObsRequestStatus(bool Succeeded, int Code, string Comment);

public sealed record ObsSceneSnapshot(string CurrentScene, bool StudioMode, IReadOnlyList<ObsSceneInfo> Scenes);

public sealed record ObsCanvasSize(int Width, int Height);

public static class ObsWebSocketResponseReader
{
    public static int ReadOperation(JsonDocument document)
    {
        return ReadInt(document.RootElement, ObsWebSocketProtocol.Op);
    }

    public static int ReadRpcVersion(JsonDocument helloDocument)
    {
        var data = helloDocument.RootElement.GetProperty(ObsWebSocketProtocol.Data);
        return ReadInt(data, ObsWebSocketProtocol.RpcVersion, 1);
    }

    public static bool TryReadAuthentication(
        JsonDocument helloDocument,
        out string salt,
        out string challenge)
    {
        salt = "";
        challenge = "";
        var data = helloDocument.RootElement.GetProperty(ObsWebSocketProtocol.Data);
        if (!data.TryGetProperty(ObsWebSocketProtocol.Authentication, out var auth))
        {
            return false;
        }

        salt = ReadString(auth, ObsWebSocketProtocol.Salt);
        challenge = ReadString(auth, ObsWebSocketProtocol.Challenge);
        return true;
    }

    public static string ReadVersion(JsonDocument response)
    {
        var data = ReadResponseData(response);
        return ReadString(data, ObsWebSocketProtocol.ObsVersion);
    }

    public static ObsSceneSnapshot ReadSceneSnapshot(JsonDocument sceneResponse, JsonDocument studioResponse)
    {
        var sceneData = ReadResponseData(sceneResponse);
        var currentScene = ReadString(sceneData, ObsWebSocketProtocol.CurrentProgramSceneName);
        var scenes = sceneData.GetProperty(ObsWebSocketProtocol.Scenes)
            .EnumerateArray()
            .Select(scene => new ObsSceneInfo(ReadString(scene, ObsWebSocketProtocol.SceneName)))
            .Where(scene => !string.IsNullOrWhiteSpace(scene.Name))
            .ToArray();

        var studioData = ReadResponseData(studioResponse);
        var studioMode = studioData.TryGetProperty(ObsWebSocketProtocol.StudioModeEnabled, out var enabled)
            && enabled.ValueKind == JsonValueKind.True;

        return new ObsSceneSnapshot(currentScene, studioMode, scenes);
    }

    public static int ReadSceneItemId(JsonDocument response)
    {
        var data = ReadResponseData(response);
        return ReadInt(data, ObsWebSocketProtocol.SceneItemId);
    }

    public static ObsCanvasSize ReadCanvasSize(JsonDocument response)
    {
        var data = ReadResponseData(response);
        var width = ReadInt(data, ObsWebSocketProtocol.BaseWidth);
        var height = ReadInt(data, ObsWebSocketProtocol.BaseHeight);

        if (width <= 0 || height <= 0)
        {
            width = ReadInt(data, ObsWebSocketProtocol.OutputWidth);
            height = ReadInt(data, ObsWebSocketProtocol.OutputHeight);
        }

        return new ObsCanvasSize(
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, width),
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, height));
    }

    public static string ReadRequestId(JsonDocument response)
    {
        var data = response.RootElement.GetProperty(ObsWebSocketProtocol.Data);
        return ReadString(data, ObsWebSocketProtocol.RequestId);
    }

    public static ObsRequestStatus ReadRequestStatus(JsonDocument response)
    {
        var data = response.RootElement.GetProperty(ObsWebSocketProtocol.Data);
        var status = data.GetProperty(ObsWebSocketProtocol.RequestStatus);
        var succeeded = status.TryGetProperty(ObsWebSocketProtocol.RequestResult, out var result)
            && result.ValueKind == JsonValueKind.True;
        var code = status.TryGetProperty(ObsWebSocketProtocol.RequestCode, out var codeElement)
            ? codeElement.GetInt32()
            : 0;

        return new ObsRequestStatus(succeeded, code, ReadString(status, ObsWebSocketProtocol.RequestComment));
    }

    public static int ReadInt(JsonElement element, string propertyName, int fallback = 0)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : fallback;
    }

    public static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private static JsonElement ReadResponseData(JsonDocument response)
    {
        return response.RootElement
            .GetProperty(ObsWebSocketProtocol.Data)
            .GetProperty(ObsWebSocketProtocol.ResponseData);
    }
}
