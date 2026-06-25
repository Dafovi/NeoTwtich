using NeoTwitch.Models;
using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Diagnostics;

public sealed record DiagnosticReportContext(
    AppConfig Config,
    string SettingsPath,
    string BackupDirectory,
    bool EventSubRunning,
    TwitchStreamStatus? StreamStatus,
    bool LightHasOpenPort,
    string LightCurrentPort,
    string LightAckStatusText,
    Func<EventRule, bool> RuleHasValidAudio);
