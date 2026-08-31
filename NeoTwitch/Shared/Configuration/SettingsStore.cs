using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;
using NeoTwitch.Services.Configuration;
using NeoTwitch.Services.Configuration.Migrations;
using NeoTwitch.Services.Configuration.Security;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed record SettingsStorePaths(string SettingsPath, string BackupDirectory, string LegacySettingsPath)
{
    public static SettingsStorePaths Default { get; } = new(
        ApplicationPaths.SettingsPath,
        ApplicationPaths.BackupDirectory,
        ApplicationPaths.LegacySettingsPath);
}

public sealed class SettingsStore : IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly IUiTextService _text;
    private readonly TimeProvider _timeProvider;
    private readonly ConfigurationSecretService _secretService;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly Func<string> _stagingIdFactory;
    private bool _createdSessionBackup;
    private bool _writesBlockedByIncompatibleSchema;
    private long _nextSaveSequence;
    private long _lastCommittedSequence;

    public SettingsStore(
        IUiTextService text,
        TimeProvider timeProvider,
        IConfigurationSecretProtector? secretProtector = null,
        SettingsStorePaths? paths = null,
        Func<string>? stagingIdFactory = null)
    {
        _text = text;
        _timeProvider = timeProvider;
        _secretService = new ConfigurationSecretService(secretProtector ?? new WindowsDpapiConfigurationSecretProtector());
        Paths = paths ?? SettingsStorePaths.Default;
        _stagingIdFactory = stagingIdFactory ?? (() => Guid.NewGuid().ToString("N"));
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public SettingsStorePaths Paths { get; }
    public string SettingsPath => Paths.SettingsPath;
    public string BackupDirectory => Paths.BackupDirectory;
    public string LatestBackupPath => Path.Combine(Path.GetDirectoryName(SettingsPath)!, "settings.backup.json");
    public string? LastLoadError { get; private set; }
    public IReadOnlyList<string> LastIntegrityWarnings { get; private set; } = [];
    public IReadOnlyList<string> LastSecretFailures { get; private set; } = [];

    public AppConfig Load()
    {
        LastLoadError = null;
        LastIntegrityWarnings = [];
        LastSecretFailures = [];
        _writesBlockedByIncompatibleSchema = false;

        var foundCandidate = false;
        foreach (var candidate in ResolveLoadCandidates())
        {
            if (!File.Exists(candidate.Path))
            {
                continue;
            }

            foundCandidate = true;

            LoadCandidateResult loaded;
            try
            {
                loaded = LoadCandidate(candidate.Path);
            }
            catch (UnsupportedConfigurationSchemaException ex)
            {
                _writesBlockedByIncompatibleSchema = true;
                LastLoadError = ex.Message;
                CrashReporter.Log(ex, _text.Format(UiTextKeys.SettingsStoreLoadFailureCrash, candidate.Path));
                return DefaultAppConfigFactory.Create(_text);
            }
            catch (Exception ex)
            {
                LastLoadError = ex.Message;
                CrashReporter.Log(ex, _text.Format(UiTextKeys.SettingsStoreLoadFailureCrash, candidate.Path));
                continue;
            }

            LastIntegrityWarnings = loaded.IntegrityReport.AmbiguousReferences;
            LastSecretFailures = loaded.SecretResult.FailedPurposes;
            if (candidate.IsRecovery)
            {
                LastLoadError = $"Se recuperó la configuración desde {candidate.Path}.";
            }
            else if (loaded.SecretResult.FailedPurposes.Count > 0)
            {
                LastLoadError = "Una o más credenciales protegidas no pudieron abrirse; se requiere autenticarlas de nuevo.";
            }

            if (loaded.SecretResult.FailedPurposes.Count == 0
                && (loaded.Migration.WasMigrated
                    || loaded.SecretResult.HadLegacyPlaintext
                    || !string.Equals(candidate.Path, SettingsPath, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    Save(loaded.Config);
                    if (candidate.IsRecovery && loaded.SecretResult.FailedPurposes.Count == 0)
                    {
                        CreateProtectedBackup(loaded.Config, candidate.Path);
                    }

                    if (candidate.IsLegacy
                        && File.Exists(candidate.Path)
                        && !string.Equals(candidate.Path, SettingsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(candidate.Path);
                    }
                }
                catch (Exception ex)
                {
                    LastLoadError = "La configuración se cargó, pero su migración segura no pudo guardarse; el archivo recuperable original se conservó.";
                    CrashReporter.Log(ex, _text.Format(UiTextKeys.SettingsStoreLoadFailureCrash, candidate.Path));
                }
            }

            return loaded.Config;
        }

        if (foundCandidate)
        {
            _writesBlockedByIncompatibleSchema = true;
        }

        return DefaultAppConfigFactory.Create(_text);
    }

    public void Save(AppConfig config) => RunSynchronously(() => SaveAsync(config));

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (_writesBlockedByIncompatibleSchema)
        {
            throw new InvalidOperationException(
                "El guardado está bloqueado para no sobrescribir una configuración incompatible o no recuperable.");
        }

        EnsureFailedSecretsHaveReplacements(config);

        var sequence = Interlocked.Increment(ref _nextSaveSequence);
        var snapshot = CreateSnapshot(config, includeProtectedSecrets: true);
        await SaveSnapshotAsync(snapshot, sequence, cancellationToken);
        LastSecretFailures = [];
    }

    public void Export(AppConfig config, string destinationPath) =>
        RunSynchronously(() => ExportAsync(config, destinationPath));

    public async Task ExportAsync(AppConfig config, string destinationPath, CancellationToken cancellationToken = default)
    {
        var snapshot = CreateSnapshot(config, includeProtectedSecrets: false);
        await WriteSnapshotAtomicallyAsync(snapshot, destinationPath, validateSecrets: false, cancellationToken);
    }

    public void CreateProtectedBackup(AppConfig config, string destinationPath) =>
        RunSynchronously(() => CreateProtectedBackupAsync(config, destinationPath));

    public async Task CreateProtectedBackupAsync(AppConfig config, string destinationPath, CancellationToken cancellationToken = default)
    {
        EnsureFailedSecretsHaveReplacements(config);
        var snapshot = CreateSnapshot(config, includeProtectedSecrets: true);
        await WriteSnapshotAtomicallyAsync(snapshot, destinationPath, validateSecrets: true, cancellationToken);
    }

    public AppConfig Import(string sourcePath)
    {
        var loaded = LoadCandidate(sourcePath);
        var wasBlocked = _writesBlockedByIncompatibleSchema;
        var previousSecretFailures = LastSecretFailures;
        _writesBlockedByIncompatibleSchema = false;
        LastSecretFailures = [];
        try
        {
            Save(loaded.Config);
        }
        catch
        {
            _writesBlockedByIncompatibleSchema = wasBlocked;
            LastSecretFailures = previousSecretFailures;
            throw;
        }

        LastIntegrityWarnings = loaded.IntegrityReport.AmbiguousReferences;
        LastSecretFailures = loaded.SecretResult.FailedPurposes;
        return loaded.Config;
    }

    public void Dispose() => _writeGate.Dispose();

    private static void RunSynchronously(Func<Task> operation) =>
        Task.Run(operation).GetAwaiter().GetResult();

    private void EnsureFailedSecretsHaveReplacements(AppConfig config)
    {
        if (LastSecretFailures.Count > 0 && !_secretService.HasReplacementsFor(config, LastSecretFailures))
        {
            throw new InvalidOperationException(
                "La operación está bloqueada hasta reemplazar las credenciales protegidas que no pudieron abrirse.");
        }
    }

    private async Task SaveSnapshotAsync(AppConfig snapshot, long sequence, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            if (sequence <= Volatile.Read(ref _lastCommittedSequence))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            await PreserveCurrentAsProtectedBackupAsync(cancellationToken);
            await WriteSnapshotAtomicallyAsync(snapshot, SettingsPath, validateSecrets: true, cancellationToken);
            Volatile.Write(ref _lastCommittedSequence, sequence);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task PreserveCurrentAsProtectedBackupAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsPath))
        {
            return;
        }

        AppConfig safePrevious;
        try
        {
            safePrevious = CreateSnapshot(LoadCandidate(SettingsPath).Config, includeProtectedSecrets: true);
        }
        catch
        {
            // A corrupt primary must not replace an existing last-known-good backup.
            return;
        }

        await WriteSnapshotAtomicallyAsync(safePrevious, LatestBackupPath, validateSecrets: true, cancellationToken);
        if (_createdSessionBackup)
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);
        var timestamp = _timeProvider.GetLocalNow().DateTime.ToString("yyyyMMdd-HHmmss");
        var timestamped = Path.Combine(BackupDirectory, $"settings-{timestamp}.json");
        await WriteSnapshotAtomicallyAsync(safePrevious, timestamped, validateSecrets: true, cancellationToken);
        _createdSessionBackup = true;
        PruneTimestampedBackups();
    }

    private async Task WriteSnapshotAtomicallyAsync(
        AppConfig snapshot,
        string destinationPath,
        bool validateSecrets,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
            destinationPath = Path.Combine(directory, destinationPath);
        }

        Directory.CreateDirectory(directory);
        var stagingPath = Path.Combine(directory, $".{Path.GetFileName(destinationPath)}.staging.{_stagingIdFactory()}");
        try
        {
            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                await using (var stream = new FileStream(
                    stagingPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    16 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    stream.Flush(flushToDisk: true);
                }
            }
            finally
            {
                Array.Clear(bytes);
            }

            ValidateStagedFile(stagingPath, validateSecrets);
            CommitStagedFile(stagingPath, destinationPath);
        }
        finally
        {
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }
        }
    }

    private void ValidateStagedFile(string stagingPath, bool validateSecrets)
    {
        var migration = AppConfigMigrationService.DeserializeAndMigrate(File.ReadAllText(stagingPath), _jsonOptions);
        if (migration.SourceSchemaVersion != AppConfig.CurrentSchemaVersion)
        {
            throw new InvalidOperationException("El archivo temporal no conserva el esquema actual.");
        }

        var normalized = AppConfigNormalizer.NormalizeWithReport(migration.Config, _text);
        if (validateSecrets)
        {
            var secretResult = _secretService.RestoreForRuntime(normalized.Config);
            if (secretResult.FailedPurposes.Count > 0)
            {
                throw new InvalidOperationException("No se pudo validar una credencial protegida del archivo temporal.");
            }
        }
    }

    private LoadCandidateResult LoadCandidate(string path)
    {
        var migration = AppConfigMigrationService.DeserializeAndMigrate(File.ReadAllText(path), _jsonOptions);
        var normalized = AppConfigNormalizer.NormalizeWithReport(migration.Config, _text);
        var secretResult = _secretService.RestoreForRuntime(normalized.Config);
        return new LoadCandidateResult(normalized.Config, migration, normalized.IntegrityReport, secretResult);
    }

    private AppConfig CreateSnapshot(AppConfig config, bool includeProtectedSecrets)
    {
        // Each request captures a deep snapshot before joining the writer queue.
        // Mutations after this point belong to a later save request.
        var inMemoryJson = JsonSerializer.Serialize(config, _jsonOptions);
        var clone = JsonSerializer.Deserialize<AppConfig>(inMemoryJson, _jsonOptions)
            ?? throw new InvalidOperationException("No se pudo crear una instantánea de configuración.");
        clone.SchemaVersion = AppConfig.CurrentSchemaVersion;
        var normalized = AppConfigNormalizer.NormalizeWithReport(clone, _text);
        if (includeProtectedSecrets)
        {
            _secretService.ProtectForPersistence(normalized.Config);
        }
        else
        {
            ConfigurationSecretService.RemoveFromExport(normalized.Config);
        }

        return normalized.Config;
    }

    private IReadOnlyList<LoadSource> ResolveLoadCandidates()
    {
        if (File.Exists(SettingsPath))
        {
            return
            [
                new LoadSource(SettingsPath, IsRecovery: false, IsLegacy: false),
                new LoadSource(LatestBackupPath, IsRecovery: true, IsLegacy: false)
            ];
        }

        return
        [
            new LoadSource(Paths.LegacySettingsPath, IsRecovery: false, IsLegacy: true),
            new LoadSource(LatestBackupPath, IsRecovery: true, IsLegacy: false)
        ];
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
                CrashReporter.Log(ex, _text.Format(UiTextKeys.SettingsStorePruneBackupFailureCrash, backup));
            }
        }
    }

    private static void CommitStagedFile(string stagingPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(stagingPath, destinationPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(stagingPath, destinationPath);
        }
    }

    private sealed record LoadSource(string Path, bool IsRecovery, bool IsLegacy);
    private sealed record LoadCandidateResult(
        AppConfig Config,
        AppConfigMigrationResult Migration,
        AppConfigIntegrityReport IntegrityReport,
        ConfigurationSecretLoadResult SecretResult);
}
