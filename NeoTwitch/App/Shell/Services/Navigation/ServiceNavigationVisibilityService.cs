using NeoTwitch.Models;

namespace NeoTwitch.Services.Navigation;

public static class ServiceNavigationVisibilityService
{
    public static ServiceNavigationVisibility Resolve(AppConfig config)
    {
        var obsEnabled = config.Obs.Enabled;
        return new ServiceNavigationVisibility(
            config.ArduinoEnabled,
            config.Alexa.Enabled,
            obsEnabled,
            obsEnabled,
            obsEnabled);
    }
}
