using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;
using NeoTwitch.Services.Configuration;

namespace NeoTwitch.Services;

public sealed class SettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private bool _createdSessionBackup;

    public SettingsStore()
    {
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public string SettingsPath { get; } = ApplicationPaths.SettingsPath;

    public string BackupDirectory { get; } = ApplicationPaths.BackupDirectory;

    private string LegacySettingsPath { get; } = ApplicationPaths.LegacySettingsPath;

    public string? LastLoadError { get; private set; }

    public AppConfig Load()
    {
        LastLoadError = null;

        var loadPath = ResolveSettingsPathForLoad();
        if (loadPath is null)
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(loadPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? AppConfig.CreateDefault();
            return AppConfigNormalizer.Normalize(config);
        }
        catch (Exception ex)
        {
            LastLoadError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo leer la configuracion: {loadPath}");
            return AppConfig.CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        var tempPath = Path.Combine(directory, "settings.tmp");

        File.WriteAllText(tempPath, json);

        BackupCurrentSettings(directory);

        File.Copy(tempPath, SettingsPath, overwrite: true);
        File.Delete(tempPath);
    }

    public void Export(AppConfig config, string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(destinationPath, json);
    }

    public AppConfig Import(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions)
            ?? throw new InvalidOperationException("El archivo no contiene una configuracion valida.");
        var normalized = AppConfigNormalizer.Normalize(config);
        Save(normalized);
        return normalized;
    }

    private void BackupCurrentSettings(string directory)
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);

        var latestBackupPath = Path.Combine(directory, "settings.backup.json");
        File.Copy(SettingsPath, latestBackupPath, overwrite: true);

        if (_createdSessionBackup)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var timestampedBackupPath = Path.Combine(BackupDirectory, $"settings-{timestamp}.json");
        File.Copy(SettingsPath, timestampedBackupPath, overwrite: true);
        _createdSessionBackup = true;
        PruneTimestampedBackups();
    }

    private void PruneTimestampedBackups()
    {
        var backups = Directory.GetFiles(BackupDirectory, "settings-*.json")
            .OrderByDescending(File.GetCreationTimeUtc)
            .Skip(ApplicationLimits.MaxSettingsBackups)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                File.Delete(backup);
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, $"No se pudo borrar backup antiguo: {backup}");
            }
        }
    }

    private string? ResolveSettingsPathForLoad()
    {
        if (File.Exists(SettingsPath))
        {
            return SettingsPath;
        }

        return File.Exists(LegacySettingsPath)
            ? LegacySettingsPath
            : null;
    }

}
