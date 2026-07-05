namespace NeoTwitch.Services.Navigation;

public sealed record ServiceNavigationVisibility(
    bool Lights,
    bool Alexa,
    bool Obs,
    bool Images,
    bool Videos);
