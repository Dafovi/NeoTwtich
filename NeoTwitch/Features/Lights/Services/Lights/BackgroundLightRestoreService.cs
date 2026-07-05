using NeoTwitch.Models;

namespace NeoTwitch.Services.Lights;

public enum BackgroundArduinoAction
{
    None,
    ApplyBackground,
    StopLights
}

public enum BackgroundAlexaAction
{
    None,
    SendOn,
    SendOff
}

public sealed record BackgroundActionPlan(
    BackgroundArduinoAction ArduinoAction,
    BackgroundAlexaAction AlexaAction)
{
    public bool HasAnyAction => ArduinoAction != BackgroundArduinoAction.None || AlexaAction != BackgroundAlexaAction.None;
}

public sealed record BackgroundRestorePlan(
    int ArduinoAttempts,
    BackgroundArduinoAction ArduinoAction,
    BackgroundAlexaAction AlexaAction);

public static class BackgroundLightRestoreService
{
    public static int ResolveArduinoRestoreAttempts(
        bool arduinoEnabled,
        bool backgroundEnabled,
        bool retryArduino)
    {
        return arduinoEnabled && backgroundEnabled && retryArduino ? 2 : 1;
    }

    public static BackgroundActionPlan ResolveApplyPlan(AppConfig config)
    {
        return new BackgroundActionPlan(
            ResolveArduinoAction(config),
            config.BackgroundAlexaEnabled ? BackgroundAlexaAction.SendOn : BackgroundAlexaAction.None);
    }

    public static BackgroundRestorePlan ResolveRestorePlan(AppConfig config, bool retryArduino)
    {
        return new BackgroundRestorePlan(
            ResolveArduinoRestoreAttempts(config.ArduinoEnabled, config.BackgroundEnabled, retryArduino),
            ResolveArduinoAction(config),
            ResolveRestoreAlexaAction(config));
    }

    private static BackgroundArduinoAction ResolveArduinoAction(AppConfig config)
    {
        if (config.ArduinoEnabled && config.BackgroundEnabled)
        {
            return BackgroundArduinoAction.ApplyBackground;
        }

        return config.ArduinoEnabled ? BackgroundArduinoAction.StopLights : BackgroundArduinoAction.None;
    }

    private static BackgroundAlexaAction ResolveRestoreAlexaAction(AppConfig config)
    {
        if (config.BackgroundAlexaTurnOffAfterEvent)
        {
            return BackgroundAlexaAction.SendOff;
        }

        return config.BackgroundAlexaEnabled ? BackgroundAlexaAction.SendOn : BackgroundAlexaAction.None;
    }
}
