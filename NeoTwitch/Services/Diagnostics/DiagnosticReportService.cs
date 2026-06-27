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

    public DiagnosticReportService(AppUpdateService updateService)
        : this(updateService.CheckLatestAsync)
    {
    }

    public DiagnosticReportService(Func<CancellationToken, Task<VersionCheckResult>> checkLatestAsync)
    {
        _checkLatestAsync = checkLatestAsync;
    }

    public async Task<DiagnosticResult> BuildAsync(DiagnosticReportContext context)
    {
        var report = new DiagnosticReportBuilder();

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
        report.Section("Version");
        report.Ok($"Version local: V{NeoTwitchProduct.CurrentVersionText}.");

        try
        {
            using var versionCts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var version = await _checkLatestAsync(versionCts.Token);
            if (version.IsUpdateAvailable)
            {
                report.Warn($"Hay una version nueva: V{version.LatestVersion}. Releases: {version.ReleaseUrl}");
            }
            else
            {
                report.Ok("La app esta al dia segun el ultimo release de GitHub.");
            }
        }
        catch (Exception ex)
        {
            report.Info($"No pude consultar GitHub ahora mismo: {ex.Message}");
        }
    }

    private static void AddFilesSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        report.Section("Archivos");
        if (File.Exists(context.SettingsPath))
        {
            report.Ok($"Configuracion encontrada: {context.SettingsPath}");
        }
        else
        {
            report.Warn($"No existe todavia el archivo de configuracion: {context.SettingsPath}");
        }

        if (Directory.Exists(context.BackupDirectory))
        {
            var backupCount = Directory.EnumerateFiles(context.BackupDirectory, "*.json").Count();
            report.Ok($"Backups automaticos: {backupCount} archivo(s) en {context.BackupDirectory}");
        }
        else
        {
            report.Info($"La carpeta de backups se creara cuando haya cambios guardados: {context.BackupDirectory}");
        }
    }

    private static void AddTwitchSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section("Twitch");
        if (string.IsNullOrWhiteSpace(config.TwitchClientId))
        {
            report.Warn("Falta el Client ID de Twitch.");
        }
        else
        {
            report.Ok("Client ID configurado.");
        }

        if (config.Token.HasToken)
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(config.Token);
            if (missingScopes.Count == 0)
            {
                report.Ok($"Token guardado con permisos necesarios. Expira: {config.Token.ExpiresAt.LocalDateTime:g}.");
            }
            else
            {
                report.Warn($"Twitch necesita reautorizar permisos: {string.Join(", ", missingScopes)}.");
            }
        }
        else
        {
            report.Warn("No hay sesion de Twitch autorizada.");
        }

        if (config.Channel.IsReady)
        {
            report.Ok($"Canal: {FirstNonEmpty(config.Channel.DisplayName, config.Channel.Login, "sin nombre")}.");
        }
        else
        {
            report.Warn("No hay canal de Twitch resuelto todavia.");
        }

        report.Info(context.EventSubRunning
            ? "EventSub esta escuchando eventos."
            : "EventSub no esta activo en este momento.");

        AddStreamStatus(report, context.StreamStatus);
    }

    private static void AddStreamStatus(DiagnosticReportBuilder report, TwitchStreamStatus? streamStatus)
    {
        if (streamStatus is { IsLive: true } live)
        {
            report.Ok($"Canal en directo con {live.ViewerCount} espectadores.");
        }
        else if (streamStatus is { IsLive: false })
        {
            report.Info("Canal sin directo activo.");
        }
        else
        {
            report.Info("Estado del directo no consultado en esta sesion.");
        }
    }

    private static void AddArduinoSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section("Arduino");
        if (!config.ArduinoEnabled)
        {
            report.Info("Arduino esta desactivado en Conexiones.");
            return;
        }

        var ports = SerialLightController.GetAvailablePortInfos();
        AddArduinoPortSummary(report, config.SerialPort, ports);

        report.Info(context.LightHasOpenPort
            ? $"Arduino conectado en {context.LightCurrentPort}. {context.LightAckStatusText}."
            : "Arduino no esta conectado desde la app.");
        report.Ok($"{config.LedStrips.Count} salida(s) LED configurada(s), {config.LedStrips.Sum(strip => strip.LedCount)} LEDs en total.");
    }

    private static void AddArduinoPortSummary(
        DiagnosticReportBuilder report,
        string configuredPort,
        IReadOnlyList<SerialPortInfo> ports)
    {
        if (ports.Count == 0)
        {
            report.Warn("No encontre puertos COM disponibles.");
        }
        else
        {
            report.Info($"Puertos detectados: {string.Join(", ", ports.Select(port => port.DisplayName))}.");
        }

        if (string.IsNullOrWhiteSpace(configuredPort))
        {
            report.Warn("No hay puerto COM configurado para Arduino.");
        }
        else if (ports.Any(port => string.Equals(port.PortName, configuredPort, StringComparison.OrdinalIgnoreCase)))
        {
            report.Ok($"Puerto configurado disponible: {configuredPort}.");
        }
        else
        {
            report.Warn($"El puerto configurado {configuredPort} no aparece conectado ahora.");
        }
    }

    private static void AddAlexaSection(DiagnosticReportBuilder report, AppConfig config)
    {
        report.Section("Alexa");
        if (!config.Alexa.Enabled)
        {
            report.Info("Alexa esta desactivada. Esto es correcto si no la quieres usar.");
        }
        else if (config.Alexa.IsConfigured)
        {
            report.Ok("Alexa relay configurado.");
        }
        else
        {
            report.Warn("Alexa esta activa, pero falta una URL valida del relay.");
        }

        if (config.BackgroundAlexaEnabled)
        {
            report.Info($"Fondo Alexa encendido: {config.BackgroundAlexaOnEventName}. Apagado: {config.BackgroundAlexaOffEventName}.");
        }
    }

    private static void AddRulesSection(DiagnosticReportBuilder report, DiagnosticReportContext context)
    {
        var config = context.Config;

        report.Section("Alertas");
        var activeRules = config.Rules.Where(rule => rule.IsEnabled).ToArray();
        if (activeRules.Length == 0)
        {
            report.Warn("No hay reglas activas.");
        }
        else
        {
            report.Ok($"{activeRules.Length} regla(s) activa(s) de {config.Rules.Count} total(es).");
        }

        AddRuleWarnings(report, context, activeRules);
    }

    private static void AddRuleWarnings(
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
            report.Warn($"Alertas activas sin acciones: {FormatNameList(rulesWithoutAction)}.");
        }

        var missingAudio = activeRules
            .Where(rule => rule.PlayAudio && !context.RuleHasValidAudio(rule))
            .Select(rule => rule.Name)
            .ToArray();
        if (missingAudio.Length > 0)
        {
            report.Warn($"Alertas con audio faltante: {FormatNameList(missingAudio)}.");
        }

        var chatCommandsWithoutCommand = activeRules
            .Where(rule => rule.EventKind == TwitchEventKind.ChatCommand && string.IsNullOrWhiteSpace(rule.ChatCommand))
            .Select(rule => rule.Name)
            .ToArray();
        if (chatCommandsWithoutCommand.Length > 0)
        {
            report.Warn($"Comandos de chat sin comando escrito: {FormatNameList(chatCommandsWithoutCommand)}.");
        }

        var rulesWithInvalidPins = activeRules
            .Where(rule => rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithInvalidPins.Length > 0)
        {
            report.Warn($"Alertas con pines LED no validos: {FormatNameList(rulesWithInvalidPins)}.");
        }

        var activeAlexaRules = activeRules.Count(rule => rule.SendAlexaEvent);
        if (activeAlexaRules > 0 && !context.Config.Alexa.IsConfigured)
        {
            report.Warn($"{activeAlexaRules} regla(s) intentan enviar Alexa, pero el relay no esta listo.");
        }
    }

    private static void AddBackgroundAndQueueSection(DiagnosticReportBuilder report, AppConfig config)
    {
        report.Section("Fondo y cola");
        report.Info(config.BackgroundEnabled
            ? $"Fondo LED activo: {DisplayNames.For(config.BackgroundPattern)} en pines {FirstNonEmpty(config.BackgroundTargetPins, "todos")}."
            : "Fondo LED apagado.");
        report.Info(config.BackgroundAlexaEnabled
            ? $"Fondo Alexa activo con evento {config.BackgroundAlexaOnEventName}."
            : "Fondo Alexa apagado.");
        report.Ok($"Cola: misma regla max {config.MaxQueuedSameRuleAlerts}, cooldown {config.SameRuleQueueCooldownMs} ms. Distintas max {config.MaxQueuedDifferentRuleAlerts}, cooldown {config.DifferentRuleQueueCooldownMs} ms.");
    }

    private static string BuildReport(DiagnosticReportBuilder report)
    {
        var header = new StringBuilder();
        header.AppendLine("Diagnostico Neo Twitch");
        header.AppendLine(report.WarningCount == 0
            ? "Estado general: sin advertencias."
            : $"Estado general: {report.WarningCount} punto(s) por revisar.");
        header.AppendLine($"Fecha: {DateTime.Now:g}");
        header.AppendLine();
        header.Append(report.BuildBody());
        header.AppendLine();
        header.AppendLine("Este diagnostico no ejecuta eventos, no prende luces, no envia chat y no dispara Alexa.");
        return header.ToString();
    }
}
