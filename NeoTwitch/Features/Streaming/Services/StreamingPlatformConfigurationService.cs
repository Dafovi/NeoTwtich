using System.Collections.ObjectModel;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public static class StreamingPlatformConfigurationService
{
    public static ObservableCollection<StreamingPlatformConnectionConfig> Normalize(
        IEnumerable<StreamingPlatformConnectionConfig>? connections,
        bool legacyTwitchAutoConnect)
    {
        var normalized = new List<StreamingPlatformConnectionConfig>();
        var knownPlatforms = new HashSet<StreamingPlatform>();

        foreach (var connection in connections ?? [])
        {
            if (!Enum.IsDefined(connection.Platform) || !knownPlatforms.Add(connection.Platform))
            {
                continue;
            }

            normalized.Add(new StreamingPlatformConnectionConfig
            {
                Platform = connection.Platform,
                IsEnabled = connection.IsEnabled,
                AutoConnect = connection.AutoConnect
            });
        }

        var twitch = normalized.FirstOrDefault(connection => connection.Platform == StreamingPlatform.Twitch);
        if (twitch is null)
        {
            normalized.Insert(0, new StreamingPlatformConnectionConfig
            {
                Platform = StreamingPlatform.Twitch,
                IsEnabled = true,
                AutoConnect = legacyTwitchAutoConnect
            });
        }
        else
        {
            // Until the platform connection UI replaces the legacy setting, the existing value
            // remains authoritative so upgrades cannot unexpectedly alter startup behavior.
            twitch.AutoConnect = legacyTwitchAutoConnect;
        }

        return new ObservableCollection<StreamingPlatformConnectionConfig>(normalized);
    }

    public static StreamingPlatformConnectionConfig GetOrCreate(
        ICollection<StreamingPlatformConnectionConfig> connections,
        StreamingPlatform platform)
    {
        var existing = connections.FirstOrDefault(connection => connection.Platform == platform);
        if (existing is not null)
        {
            return existing;
        }

        var created = new StreamingPlatformConnectionConfig
        {
            Platform = platform,
            IsEnabled = platform == StreamingPlatform.Twitch,
            AutoConnect = false
        };
        connections.Add(created);
        return created;
    }
}
