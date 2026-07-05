using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public sealed record QueuedAlertSlot(string Id, string RuleId, string RuleName, TwitchEventKind EventKind);
