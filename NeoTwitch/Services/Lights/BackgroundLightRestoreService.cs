namespace NeoTwitch.Services.Lights;

public static class BackgroundLightRestoreService
{
    public static int ResolveArduinoRestoreAttempts(
        bool arduinoEnabled,
        bool backgroundEnabled,
        bool retryArduino)
    {
        return arduinoEnabled && backgroundEnabled && retryArduino ? 2 : 1;
    }
}
