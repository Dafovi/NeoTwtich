namespace NeoTwitch.Models;

public sealed class ObsIntegrationConfig
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 4455;

    public string Password { get; set; } = "";

    public bool AutoReconnect { get; set; } = true;

    public int OverlayWidth { get; set; } = 1920;

    public int OverlayHeight { get; set; } = 1080;

    public int OverlayMediaWidth { get; set; } = 720;

    public int OverlayMediaHeight { get; set; } = 420;

    public string OverlayPositionMode { get; set; } = "Center";

    public int OverlayX { get; set; }

    public int OverlayY { get; set; }

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535;
}
