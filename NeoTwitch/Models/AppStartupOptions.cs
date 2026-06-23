namespace NeoTwitch.Models;

public sealed record AppStartupOptions(
    bool DebugMode,
    bool SafeMode,
    bool NoAutoConnect,
    bool NoStartHidden)
{
    public static AppStartupOptions Default { get; } = new(false, false, false, false);

    public bool SuppressAutoConnect => SafeMode || NoAutoConnect;

    public bool SuppressStartHidden => SafeMode || NoStartHidden || DebugMode;

    public static AppStartupOptions Parse(IEnumerable<string> args)
    {
        var normalizedArgs = args
            .Select(arg => arg.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AppStartupOptions(
            normalizedArgs.Contains("--debug"),
            normalizedArgs.Contains("--safe-mode"),
            normalizedArgs.Contains("--no-autoconnect"),
            normalizedArgs.Contains("--no-start-hidden"));
    }
}
