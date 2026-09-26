namespace NeoTwitch.Models;

/// <summary>
/// A platform-neutral event that can enter Neo Stream's alert pipeline.
/// Provider-specific information stays in <see cref="RawType"/> and <see cref="Metadata"/>.
/// </summary>
public sealed record StreamEvent(
    StreamingPlatform Platform,
    StreamEventKind Kind,
    string EventId,
    DateTimeOffset OccurredAt,
    string Title,
    string? UserName = null,
    string? RewardTitle = null,
    int? ViewerCount = null,
    int? ContributionUnits = null,
    string? Message = null,
    string? RawType = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
