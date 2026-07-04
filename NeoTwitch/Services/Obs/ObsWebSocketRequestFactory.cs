using System.Security.Cryptography;
using System.Text;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Obs;

public static class ObsWebSocketRequestFactory
{
    public static Uri BuildUri(ObsIntegrationConfig config)
    {
        var host = config.Host.Trim();
        if (host.StartsWith(ObsWebSocketProtocol.WebSocketScheme, StringComparison.OrdinalIgnoreCase)
            || host.StartsWith(ObsWebSocketProtocol.SecureWebSocketScheme, StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(host);
        }

        return new Uri($"{ObsWebSocketProtocol.WebSocketScheme}{host}:{config.Port}");
    }

    public static string BuildAuthentication(string password, string salt, string challenge)
    {
        var secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
    }

    public static Dictionary<string, object?> BuildMediaInputSettings(ObsMediaKind kind, string filePath)
    {
        return kind == ObsMediaKind.Image
            ? new Dictionary<string, object?> { [ObsWebSocketProtocol.ImageFile] = filePath }
            : new Dictionary<string, object?>
            {
                [ObsWebSocketProtocol.IsLocalFile] = true,
                [ObsWebSocketProtocol.LocalFile] = filePath,
                [ObsWebSocketProtocol.Looping] = false,
                [ObsWebSocketProtocol.RestartOnActivate] = true,
                [ObsWebSocketProtocol.CloseWhenInactive] = true
            };
    }

    public static Dictionary<string, object?> BuildSceneItemTransformRequest(
        string sceneName,
        int sceneItemId,
        ObsIntegrationConfig config)
    {
        var mediaWidth = Math.Clamp(
            config.OverlayMediaWidth,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayWidth));
        var mediaHeight = Math.Clamp(
            config.OverlayMediaHeight,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayHeight));
        var (positionX, positionY) = ResolveOverlayPosition(config, mediaWidth, mediaHeight);

        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.SceneItemId] = sceneItemId,
            [ObsWebSocketProtocol.SceneItemTransform] = new Dictionary<string, object?>
            {
                [ObsWebSocketProtocol.PositionX] = positionX,
                [ObsWebSocketProtocol.PositionY] = positionY,
                [ObsWebSocketProtocol.BoundsType] = ObsWebSocketProtocol.BoundsScaleInner,
                [ObsWebSocketProtocol.BoundsWidth] = mediaWidth,
                [ObsWebSocketProtocol.BoundsHeight] = mediaHeight
            }
        };
    }

    public static Dictionary<string, object?> BuildInputVolumeRequest(string sourceName, int volumePercent)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.InputName] = sourceName.Trim(),
            [ObsWebSocketProtocol.InputVolumeMul] =
                Math.Clamp(volumePercent, ApplicationLimits.MinVolumePercent, ApplicationLimits.MaxVolumePercent) / 100d
        };
    }

    public static (int X, int Y) ResolveOverlayPosition(ObsIntegrationConfig config, int mediaWidth, int mediaHeight)
    {
        var maxX = Math.Max(0, config.OverlayWidth - mediaWidth);
        var maxY = Math.Max(0, config.OverlayHeight - mediaHeight);
        return config.OverlayPositionMode switch
        {
            ObsWebSocketProtocol.CustomPositionMode => (Math.Clamp(config.OverlayX, 0, maxX), Math.Clamp(config.OverlayY, 0, maxY)),
            ObsWebSocketProtocol.RandomPositionMode => (Random.Shared.Next(0, maxX + 1), Random.Shared.Next(0, maxY + 1)),
            _ => (maxX / 2, maxY / 2)
        };
    }
}
