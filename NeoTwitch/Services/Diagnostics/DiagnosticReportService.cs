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
        var config = context.Config;
        var body = new StringBuilder();
        var warningCount = 0;

        void Section(string title)
        {
            if (body.Length > 0)
            {
                body.AppendLine();
            }

            body.AppendLine(title);
        }

        void Line(string level, string message)
        {
            body.AppendLine($"{level} {message}");
            if (string.Equals(level, "[REVISAR]", StringComparison.Ordinal))
            {
                warningCount++;
            }
        }

        void Ok(string message) => Line("[OK]", message);
        void Info(string message) => Line("[INFO]", message);
        void Warn(string message) => Line("[REVISAR]", message);

        Section("Version");
        Ok($"Version local: V{NeoTwitchProduct.CurrentVersionText}.");
        try
        {
            using var versionCts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var version = await _checkLatestAsync(versionCts.Token);
            if (version.IsUpdateAvailable)
            {
                Warn($"Hay una version nueva: V{version.LatestVersion}. Releases: {version.ReleaseUrl}");
            }
            else
            {
                Ok("La app esta al dia segun el ultimo release de GitHub.");
            }
        }
        catch (Exception ex)
        {
            Info($"No pude consultar GitHub ahora mismo: {ex.Message}");
        }

        Section("Archivos");
        if (File.Exists(context.SettingsPath))
        {
            Ok($"Configuracion encontrada: {context.SettingsPath}");
        }
        else
        {
            Warn($"No existe todavia el archivo de configuracion: {context.SettingsPath}");
        }

        if (Directory.Exists(context.BackupDirectory))
        {
            var backupCount = Directory.EnumerateFiles(context.BackupDirectory, "*.json").Count();
            Ok($"Backups automaticos: {backupCount} archivo(s) en {context.BackupDirectory}");
        }
        else
        {
            Info($"La carpeta de backups se creara cuando haya cambios guardados: {context.BackupDirectory}");
        }

        Section("Twitch");
        if (string.IsNullOrWhiteSpace(config.TwitchClientId))
        {
            Warn("Falta el Client ID de Twitch.");
        }
        else
        {
            Ok("Client ID configurado.");
        }

        if (config.Token.HasToken)
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(config.Token);
            if (missingScopes.Count == 0)
            {
                Ok($"Token guardado con permisos necesarios. Expira: {config.Token.ExpiresAt.LocalDateTime:g}.");
            }
            else
            {
                Warn($"Twitch necesita reautorizar permisos: {string.Join(", ", missingScopes)}.");
            }
        }
        else
        {
            Warn("No hay sesion de Twitch autorizada.");
        }

        if (config.Channel.IsReady)
        {
            Ok($"Canal: {FirstNonEmpty(config.Channel.DisplayName, config.Channel.Login, "sin nombre")}.");
        }
        else
        {
            Warn("No hay canal de Twitch resuelto todavia.");
        }

        Info(context.EventSubRunning
            ? "EventSub esta escuchando eventos."
            : "EventSub no esta activo en este momento.");

        if (context.StreamStatus is { IsLive: true } live)
        {
            Ok($"Canal en directo con {live.ViewerCount} espectadores.");
        }
        else if (context.StreamStatus is { IsLive: false })
        {
            Info("Canal sin directo activo.");
        }
        else
        {
            Info("Estado del directo no consultado en esta sesion.");
        }

        Section("Arduino");
        if (!config.ArduinoEnabled)
        {
            Info("Arduino esta desactivado en Conexiones.");
        }
        else
        {
            var ports = SerialLightController.GetAvailablePortInfos();
            if (ports.Count == 0)
            {
                Warn("No encontre puertos COM disponibles.");
            }
            else
            {
                Info($"Puertos detectados: {string.Join(", ", ports.Select(port => port.DisplayName))}.");
            }

            if (string.IsNullOrWhiteSpace(config.SerialPort))
            {
                Warn("No hay puerto COM configurado para Arduino.");
            }
            else if (ports.Any(port => string.Equals(port.PortName, config.SerialPort, StringComparison.OrdinalIgnoreCase)))
            {
                Ok($"Puerto configurado disponible: {config.SerialPort}.");
            }
            else
            {
                Warn($"El puerto configurado {config.SerialPort} no aparece conectado ahora.");
            }

            Info(context.LightHasOpenPort
                ? $"Arduino conectado en {context.LightCurrentPort}. {context.LightAckStatusText}."
                : "Arduino no esta conectado desde la app.");
            Ok($"{config.LedStrips.Count} salida(s) LED configurada(s), {config.LedStrips.Sum(strip => strip.LedCount)} LEDs en total.");
        }

        Section("Alexa");
        if (!config.Alexa.Enabled)
        {
            Info("Alexa esta desactivada. Esto es correcto si no la quieres usar.");
        }
        else if (config.Alexa.IsConfigured)
        {
            Ok("Alexa relay configurado.");
        }
        else
        {
            Warn("Alexa esta activa, pero falta una URL valida del relay.");
        }

        if (config.BackgroundAlexaEnabled)
        {
            Info($"Fondo Alexa encendido: {config.BackgroundAlexaOnEventName}. Apagado: {config.BackgroundAlexaOffEventName}.");
        }

        Section("Alertas");
        var activeRules = config.Rules.Where(rule => rule.IsEnabled).ToArray();
        if (activeRules.Length == 0)
        {
            Warn("No hay reglas activas.");
        }
        else
        {
            Ok($"{activeRules.Length} regla(s) activa(s) de {config.Rules.Count} total(es).");
        }

        var rulesWithoutAction = activeRules
            .Where(rule => !rule.UseLights && !rule.PlayAudio && !rule.SendChatMessage && !rule.SendAlexaEvent)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithoutAction.Length > 0)
        {
            Warn($"Alertas activas sin acciones: {FormatNameList(rulesWithoutAction)}.");
        }

        var missingAudio = activeRules
            .Where(rule => rule.PlayAudio && !context.RuleHasValidAudio(rule))
            .Select(rule => rule.Name)
            .ToArray();
        if (missingAudio.Length > 0)
        {
            Warn($"Alertas con audio faltante: {FormatNameList(missingAudio)}.");
        }

        var chatCommandsWithoutCommand = activeRules
            .Where(rule => rule.EventKind == TwitchEventKind.ChatCommand && string.IsNullOrWhiteSpace(rule.ChatCommand))
            .Select(rule => rule.Name)
            .ToArray();
        if (chatCommandsWithoutCommand.Length > 0)
        {
            Warn($"Comandos de chat sin comando escrito: {FormatNameList(chatCommandsWithoutCommand)}.");
        }

        var rulesWithInvalidPins = activeRules
            .Where(rule => rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithInvalidPins.Length > 0)
        {
            Warn($"Alertas con pines LED no validos: {FormatNameList(rulesWithInvalidPins)}.");
        }

        var activeAlexaRules = activeRules.Count(rule => rule.SendAlexaEvent);
        if (activeAlexaRules > 0 && !config.Alexa.IsConfigured)
        {
            Warn($"{activeAlexaRules} regla(s) intentan enviar Alexa, pero el relay no esta listo.");
        }

        Section("Fondo y cola");
        Info(config.BackgroundEnabled
            ? $"Fondo LED activo: {DisplayNames.For(config.BackgroundPattern)} en pines {FirstNonEmpty(config.BackgroundTargetPins, "todos")}."
            : "Fondo LED apagado.");
        Info(config.BackgroundAlexaEnabled
            ? $"Fondo Alexa activo con evento {config.BackgroundAlexaOnEventName}."
            : "Fondo Alexa apagado.");
        Ok($"Cola: misma regla max {config.MaxQueuedSameRuleAlerts}, cooldown {config.SameRuleQueueCooldownMs} ms. Distintas max {config.MaxQueuedDifferentRuleAlerts}, cooldown {config.DifferentRuleQueueCooldownMs} ms.");

        var header = new StringBuilder();
        header.AppendLine("Diagnostico Neo Twitch");
        header.AppendLine(warningCount == 0
            ? "Estado general: sin advertencias."
            : $"Estado general: {warningCount} punto(s) por revisar.");
        header.AppendLine($"Fecha: {DateTime.Now:g}");
        header.AppendLine();
        header.Append(body);
        header.AppendLine();
        header.AppendLine("Este diagnostico no ejecuta eventos, no prende luces, no envia chat y no dispara Alexa.");

        return new DiagnosticResult(header.ToString(), warningCount);
    }
}
