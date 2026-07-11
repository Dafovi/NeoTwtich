namespace NeoTwitch.Models;

public sealed record VirtualScreenInfo(
    string Id,
    string Label,
    double Left,
    double Top,
    double Width,
    double Height,
    bool IsPrimary);
