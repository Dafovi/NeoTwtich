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

    public static Dictionary<string, object?> BuildSetCurrentProgramSceneRequest(string sceneName)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim()
        };
    }

    public static Dictionary<string, object?> BuildCreateInputRequest(
        string sceneName,
        string sourceName,
        ObsMediaKind kind,
        string filePath)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.InputName] = sourceName.Trim(),
            [ObsWebSocketProtocol.InputKind] = kind == ObsMediaKind.Image
                ? ObsWebSocketProtocol.ImageSourceKind
                : ObsWebSocketProtocol.FfmpegSourceKind,
            [ObsWebSocketProtocol.InputSettings] = BuildMediaInputSettings(kind, filePath),
            [ObsWebSocketProtocol.SceneItemEnabled] = true
        };
    }

    public static Dictionary<string, object?> BuildSetInputSettingsRequest(
        string sourceName,
        ObsMediaKind kind,
        string filePath)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.InputName] = sourceName.Trim(),
            [ObsWebSocketProtocol.InputSettings] = BuildMediaInputSettings(kind, filePath),
            [ObsWebSocketProtocol.Overlay] = true
        };
    }

    public static Dictionary<string, object?> BuildBrowserInputSettings(string url, int width, int height)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.BrowserUrl] = url,
            [ObsWebSocketProtocol.BrowserWidth] = Math.Max(ApplicationLimits.MinObsOverlayMediaSize, width),
            [ObsWebSocketProtocol.BrowserHeight] = Math.Max(ApplicationLimits.MinObsOverlayMediaSize, height),
            [ObsWebSocketProtocol.BrowserShutdown] = false,
            [ObsWebSocketProtocol.BrowserRestartWhenActive] = false
        };
    }

    public static Dictionary<string, object?> BuildCreateBrowserInputRequest(
        string sceneName,
        string sourceName,
        string url,
        int width,
        int height)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.InputName] = sourceName.Trim(),
            [ObsWebSocketProtocol.InputKind] = ObsWebSocketProtocol.BrowserSourceKind,
            [ObsWebSocketProtocol.InputSettings] = BuildBrowserInputSettings(url, width, height),
            [ObsWebSocketProtocol.SceneItemEnabled] = true
        };
    }

    public static Dictionary<string, object?> BuildSetBrowserInputSettingsRequest(
        string sourceName,
        string url,
        int width,
        int height)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.InputName] = sourceName.Trim(),
            [ObsWebSocketProtocol.InputSettings] = BuildBrowserInputSettings(url, width, height),
            [ObsWebSocketProtocol.Overlay] = true
        };
    }

    public static Dictionary<string, object?> BuildCreateSceneItemRequest(string sceneName, string sourceName)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.SourceName] = sourceName.Trim(),
            [ObsWebSocketProtocol.SceneItemEnabled] = true
        };
    }

    public static Dictionary<string, object?> BuildSetSceneItemEnabledRequest(
        string sceneName,
        int sceneItemId,
        bool enabled)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.SceneItemId] = sceneItemId,
            [ObsWebSocketProtocol.SceneItemEnabled] = enabled
        };
    }

    public static Dictionary<string, object?> BuildGetSceneItemIdRequest(string sceneName, string sourceName)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.SourceName] = sourceName.Trim()
        };
    }

    public static Dictionary<string, object?> BuildSceneItemTransformRequest(
        string sceneName,
        int sceneItemId,
        ObsIntegrationConfig config,
        Func<int, int, int>? randomNext = null)
    {
        var mediaWidth = Math.Clamp(
            config.OverlayMediaWidth,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayWidth));
        var mediaHeight = Math.Clamp(
            config.OverlayMediaHeight,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayHeight));
        var (positionX, positionY) = ResolveOverlayPosition(config, mediaWidth, mediaHeight, randomNext);

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

    public static Dictionary<string, object?> BuildFullSceneItemTransformRequest(
        string sceneName,
        int sceneItemId,
        int width,
        int height)
    {
        return new Dictionary<string, object?>
        {
            [ObsWebSocketProtocol.SceneName] = sceneName.Trim(),
            [ObsWebSocketProtocol.SceneItemId] = sceneItemId,
            [ObsWebSocketProtocol.SceneItemTransform] = new Dictionary<string, object?>
            {
                [ObsWebSocketProtocol.PositionX] = 0,
                [ObsWebSocketProtocol.PositionY] = 0,
                [ObsWebSocketProtocol.BoundsType] = ObsWebSocketProtocol.BoundsStretch,
                [ObsWebSocketProtocol.BoundsWidth] = Math.Max(ApplicationLimits.MinObsOverlayMediaSize, width),
                [ObsWebSocketProtocol.BoundsHeight] = Math.Max(ApplicationLimits.MinObsOverlayMediaSize, height)
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

    public static (int X, int Y) ResolveOverlayPosition(
        ObsIntegrationConfig config,
        int mediaWidth,
        int mediaHeight,
        Func<int, int, int>? randomNext = null)
    {
        return ObsOverlayPositionService.Resolve(config, mediaWidth, mediaHeight, randomNext);
    }
}
