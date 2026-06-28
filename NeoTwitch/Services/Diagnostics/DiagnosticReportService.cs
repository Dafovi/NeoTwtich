using System.IO;
using System.Text;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Status;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch.Services.Diagnostics;

public sealed class DiagnosticReportService
{
    private readonly Func<CancellationToken, Task<VersionCheckResult>> _checkLatestAsync;
    private readonly IUiTextService _text;

    public DiagnosticReportService(AppUpdateService updateService)
        : this(updateService.CheckLatestAsync, UiTextService.CreateDefault())
    {
    }

    public DiagnosticReportService(Func<CancellationToken, Task<VersionCheckResult>> checkLatestAsync)
        : this(checkLatestAsync, UiTextService.CreateDefault())
    {
    }

    public DiagnosticReportService(
        Func<CancellationToken, Task<VersionCheckResult>> checkLatestAsync,
        IUiTextService text)
    {
        _checkLatestAsync = checkLatestAsync;
        _text = text;
    }

    public async Task<DiagnosticResult> BuildAsync(DiagnosticReportContext context)
    {
        var report = new DiagnosticReportBuilder(
            _text.Get(UiTextKeys.DiagnosticsLevelOk),
            _text.Get(UiTextKeys.DiagnosticsLevelInfo),
            _text.Get(UiTextKeys.DiagnosticsLevelReview));

        await AddVersionSectionAsync(report);
        AddFilesSection(report, context);
        AddTwitchSection(report, context);
        AddArduinoSection(report, context);
        AddAlexaSection(report, context.Config);
        AddRulesSection(report, context);
        AddBackgroundAndQueueSection(report, context.Config);

        return new DiagnosticResult(BuildReport(report), report.WarningCount);
    }

    private async Task AddVersionSectionAsync(DiagnosticReportBuilder report)
    {
        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionVersion));
        report.Ok(_text.Format(UiTextKeys.DiagnosticsReportLocalVersion, NeoTwitchProduct.CurrentVersionText));

        try
        {
            using var versionCts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var version = await _checkLatestAsync(versionCts.Token);
            if (version.IsUpdateAvailable)
            {
                report.Warn(_text.Format(UiTextKeys.DiagnosticsReportUpdateAvailable, version.LatestVersion, version.ReleaseUrl));
            }
            else
            {
                report.Ok(_text.Get(UiTextKeys.DiagnosticsReportAppUpToDate));
            }
        }
        catch (Exception ex)
        {
            report.Info(_text.Format(UiTextKeys.DiagnosticsReportGithubUnavailable, ex.Message));
        }
    }

    private void AddFilesSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionFiles));
        if (File.Exists(context.SettingsPath))
        {
            report.Ok(_text.Format(UiTextKeys.DiagnosticsReportSettingsFound, context.SettingsPath));
        }
        else
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportSettingsMissing, context.SettingsPath));
        }

        if (Directory.Exists(context.BackupDirectory))
        {
            var backupCount = Directory.EnumerateFiles(context.BackupDirectory, "*.json").Count();
            report.Ok(_text.Format(UiTextKeys.DiagnosticsReportBackupsFound, backupCount, context.BackupDirectory));
        }
        else
        {
            report.Info(_text.Format(UiTextKeys.DiagnosticsReportBackupsDeferred, context.BackupDirectory));
        }
    }

    private void AddTwitchSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionTwitch));
        if (string.IsNullOrWhiteSpace(config.TwitchClientId))
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportMissingClientId));
        }
        else
        {
            report.Ok(_text.Get(UiTextKeys.DiagnosticsReportClientIdConfigured));
        }

        if (config.Token.HasToken)
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(config.Token);
            if (missingScopes.Count == 0)
            {
                report.Ok(_text.Format(UiTextKeys.DiagnosticsReportTokenReady, config.Token.ExpiresAt.LocalDateTime));
            }
            else
            {
                report.Warn(_text.Format(UiTextKeys.DiagnosticsReportMissingScopes, string.Join(", ", missingScopes)));
            }
        }
        else
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportNoTwitchSession));
        }

        if (config.Channel.IsReady)
        {
            report.Ok(_text.Format(
                UiTextKeys.DiagnosticsReportChannelReady,
                FirstNonEmpty(config.Channel.DisplayName, config.Channel.Login, _text.Get(UiTextKeys.DiagnosticsReportUnnamed))));
        }
        else
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportChannelMissing));
        }

        report.Info(context.EventSubRunning
            ? _text.Get(UiTextKeys.DiagnosticsReportEventSubRunning)
            : _text.Get(UiTextKeys.DiagnosticsReportEventSubStopped));

        AddStreamStatus(report, context.StreamStatus);
    }

    private void AddStreamStatus(DiagnosticReportBuilder report, TwitchStreamStatus? streamStatus)
    {
        if (streamStatus is { IsLive: true } live)
        {
            report.Ok(_text.Format(UiTextKeys.DiagnosticsReportStreamLive, live.ViewerCount));
        }
        else if (streamStatus is { IsLive: false })
        {
            report.Info(_text.Get(UiTextKeys.DiagnosticsReportStreamOffline));
        }
        else
        {
            report.Info(_text.Get(UiTextKeys.DiagnosticsReportStreamUnqueried));
        }
    }

    private void AddArduinoSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionArduino));
        if (!config.ArduinoEnabled)
        {
            report.Info(_text.Get(UiTextKeys.DiagnosticsReportArduinoDisabled));
            return;
        }

        var ports = SerialLightController.GetAvailablePortInfos();
        AddArduinoPortSummary(report, config.SerialPort, ports);

        report.Info(context.LightHasOpenPort
            ? _text.Format(UiTextKeys.DiagnosticsReportArduinoConnected, context.LightCurrentPort, context.LightAckStatusText)
            : _text.Get(UiTextKeys.DiagnosticsReportArduinoNotConnected));
        report.Ok(_text.Format(
            UiTextKeys.DiagnosticsReportLedOutputs,
            config.LedStrips.Count,
            config.LedStrips.Sum(strip => strip.LedCount)));
    }

    private void AddArduinoPortSummary(
        DiagnosticReportBuilder report,
        string configuredPort,
        IReadOnlyList<SerialPortInfo> ports)
    {
        if (ports.Count == 0)
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportNoComPorts));
        }
        else
        {
            report.Info(_text.Format(
                UiTextKeys.DiagnosticsReportDetectedPorts,
                string.Join(", ", ports.Select(port => port.DisplayName))));
        }

        if (string.IsNullOrWhiteSpace(configuredPort))
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportMissingCom));
        }
        else if (ports.Any(port => string.Equals(port.PortName, configuredPort, StringComparison.OrdinalIgnoreCase)))
        {
            report.Ok(_text.Format(UiTextKeys.DiagnosticsReportPortAvailable, configuredPort));
        }
        else
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportPortUnavailable, configuredPort));
        }
    }

    private void AddAlexaSection(DiagnosticReportBuilder report, AppConfig config)
    {
        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionAlexa));
        if (!config.Alexa.Enabled)
        {
            report.Info(_text.Get(UiTextKeys.DiagnosticsReportAlexaDisabled));
        }
        else if (config.Alexa.IsConfigured)
        {
            report.Ok(_text.Get(UiTextKeys.DiagnosticsReportAlexaConfigured));
        }
        else
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportAlexaIncomplete));
        }

        if (config.BackgroundAlexaEnabled)
        {
            report.Info(_text.Format(
                UiTextKeys.DiagnosticsReportAlexaBackground,
                config.BackgroundAlexaOnEventName,
                config.BackgroundAlexaOffEventName));
        }
    }

    private void AddRulesSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionAlerts));
        var activeRules = config.Rules.Where(rule => rule.IsEnabled).ToArray();
        if (activeRules.Length == 0)
        {
            report.Warn(_text.Get(UiTextKeys.DiagnosticsReportNoActiveRules));
        }
        else
        {
            report.Ok(_text.Format(UiTextKeys.DiagnosticsReportActiveRules, activeRules.Length, config.Rules.Count));
        }

        AddRuleWarnings(report, context, activeRules);
    }

    private void AddRuleWarnings(
        DiagnosticReportBuilder report,
        DiagnosticReportContext context,
        IReadOnlyCollection<EventRule> activeRules)
    {
        var rulesWithoutAction = activeRules
            .Where(rule => !rule.UseLights && !rule.PlayAudio && !rule.SendChatMessage && !rule.SendAlexaEvent)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithoutAction.Length > 0)
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportRulesWithoutAction, FormatNameList(rulesWithoutAction)));
        }

        var missingAudio = activeRules
            .Where(rule => rule.PlayAudio && !context.RuleHasValidAudio(rule))
            .Select(rule => rule.Name)
            .ToArray();
        if (missingAudio.Length > 0)
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportMissingAudio, FormatNameList(missingAudio)));
        }

        var chatCommandsWithoutCommand = activeRules
            .Where(rule => rule.EventKind == TwitchEventKind.ChatCommand && string.IsNullOrWhiteSpace(rule.ChatCommand))
            .Select(rule => rule.Name)
            .ToArray();
        if (chatCommandsWithoutCommand.Length > 0)
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportChatCommandsMissing, FormatNameList(chatCommandsWithoutCommand)));
        }

        var rulesWithInvalidPins = activeRules
            .Where(rule => rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithInvalidPins.Length > 0)
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportInvalidPins, FormatNameList(rulesWithInvalidPins)));
        }

        var activeAlexaRules = activeRules.Count(rule => rule.SendAlexaEvent);
        if (activeAlexaRules > 0 && !context.Config.Alexa.IsConfigured)
        {
            report.Warn(_text.Format(UiTextKeys.DiagnosticsReportAlexaRulesIncomplete, activeAlexaRules));
        }
    }

    private void AddBackgroundAndQueueSection(DiagnosticReportBuilder report, AppConfig config)
    {
        report.Section(_text.Get(UiTextKeys.DiagnosticsSectionBackgroundQueue));
        report.Info(config.BackgroundEnabled
                ? _text.Format(
                    UiTextKeys.DiagnosticsReportBackgroundLedActive,
                    DisplayNameService.For(config.BackgroundPattern, _text),
                    FirstNonEmpty(config.BackgroundTargetPins, _text.Get(UiTextKeys.DiagnosticsReportAllPins)))
            : _text.Get(UiTextKeys.DiagnosticsReportBackgroundLedOff));
        report.Info(config.BackgroundAlexaEnabled
            ? _text.Format(UiTextKeys.DiagnosticsReportBackgroundAlexaActive, config.BackgroundAlexaOnEventName)
            : _text.Get(UiTextKeys.DiagnosticsReportBackgroundAlexaOff));
        report.Ok(_text.Format(
            UiTextKeys.DiagnosticsReportQueue,
            config.MaxQueuedSameRuleAlerts,
            config.SameRuleQueueCooldownMs,
            config.MaxQueuedDifferentRuleAlerts,
            config.DifferentRuleQueueCooldownMs));
    }

    private string BuildReport(DiagnosticReportBuilder report)
    {
        var header = new StringBuilder();
        header.AppendLine(_text.Get(UiTextKeys.DiagnosticsReportTitle));
        header.AppendLine(report.WarningCount == 0
            ? _text.Get(UiTextKeys.DiagnosticsReportStateOk)
            : _text.Format(UiTextKeys.DiagnosticsReportStateWarnings, report.WarningCount));
        header.AppendLine(_text.Format(UiTextKeys.DiagnosticsReportDate, DateTime.Now));
        header.AppendLine();
        header.Append(report.BuildBody());
        header.AppendLine();
        header.AppendLine(_text.Get(UiTextKeys.DiagnosticsReportFooter));
        return header.ToString();
    }
}
