using NeoTwitch.Models;

namespace NeoTwitch.Services.Obs;

public static class ObsOverlayPositionService
{
    public static (int X, int Y) Resolve(
        ObsIntegrationConfig config,
        int mediaWidth,
        int mediaHeight,
        Func<int, int, int>? randomNext = null)
    {
        var maxX = Math.Max(0, config.OverlayWidth - mediaWidth);
        var maxY = Math.Max(0, config.OverlayHeight - mediaHeight);
        return config.OverlayPositionMode switch
        {
            ObsWebSocketProtocol.CustomPositionMode => (Math.Clamp(config.OverlayX, 0, maxX), Math.Clamp(config.OverlayY, 0, maxY)),
            ObsWebSocketProtocol.RandomPositionMode => (Next(randomNext, maxX), Next(randomNext, maxY)),
            _ => (maxX / 2, maxY / 2)
        };
    }

    private static int Next(Func<int, int, int>? randomNext, int maxInclusive)
    {
        return randomNext is null
            ? Random.Shared.Next(0, maxInclusive + 1)
            : randomNext(0, maxInclusive + 1);
    }
}
