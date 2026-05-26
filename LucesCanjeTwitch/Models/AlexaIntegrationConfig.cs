namespace LucesCanjeTwitch.Models;

public sealed class AlexaIntegrationConfig
{
    public bool Enabled { get; set; }
    public string RelayUrl { get; set; } = "";
    public string AuthToken { get; set; } = "";

    public bool IsConfigured =>
        Enabled
        && Uri.TryCreate(RelayUrl, UriKind.Absolute, out var uri)
        && uri.Scheme is "https" or "http";
}
