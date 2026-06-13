namespace NeoTwitch.Models;

public sealed class ObsIntegrationConfig
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 4455;

    public string Password { get; set; } = "";

    public bool AutoReconnect { get; set; } = true;

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535;
}
