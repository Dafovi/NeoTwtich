using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public enum RuleTestValidationIssueKind
{
    MissingAudio,
    ArduinoMissingCom,
    ArduinoDisconnected,
    InvalidPins,
    AlexaNotConfigured,
    ChatCommandMismatch
}

public sealed record RuleTestValidationIssue(RuleTestValidationIssueKind Kind);

public sealed record RuleTestValidationResult(IReadOnlyList<RuleTestValidationIssue> Issues)
{
    public bool CanRun => Issues.All(issue => issue.Kind != RuleTestValidationIssueKind.MissingAudio);
}

public static class RuleTestValidationService
{
    public static RuleTestValidationResult Validate(
        EventRule rule,
        TwitchEvent twitchEvent,
        AppConfig config,
        bool hasOpenArduinoPort,
        bool hasValidAudio)
    {
        var issues = new List<RuleTestValidationIssue>();

        if (rule.PlayAudio && !hasValidAudio)
        {
            issues.Add(new RuleTestValidationIssue(RuleTestValidationIssueKind.MissingAudio));
            return new RuleTestValidationResult(issues);
        }

        if (config.ArduinoEnabled && rule.UseLights && !hasOpenArduinoPort)
        {
            issues.Add(new RuleTestValidationIssue(
                string.IsNullOrWhiteSpace(config.SerialPort)
                    ? RuleTestValidationIssueKind.ArduinoMissingCom
                    : RuleTestValidationIssueKind.ArduinoDisconnected));
        }

        if (config.ArduinoEnabled
            && rule.UseLights
            && !string.IsNullOrWhiteSpace(rule.TargetPins)
            && LightCommand.ParsePins(rule.TargetPins).Count == 0)
        {
            issues.Add(new RuleTestValidationIssue(RuleTestValidationIssueKind.InvalidPins));
        }

        if (rule.SendAlexaEvent && !config.Alexa.IsConfigured)
        {
            issues.Add(new RuleTestValidationIssue(RuleTestValidationIssueKind.AlexaNotConfigured));
        }

        if (rule.EventKind == TwitchEventKind.ChatCommand
            && !RuleSimulationService.MatchesChatCommand(rule, twitchEvent.Message))
        {
            issues.Add(new RuleTestValidationIssue(RuleTestValidationIssueKind.ChatCommandMismatch));
        }

        return new RuleTestValidationResult(issues);
    }
}
