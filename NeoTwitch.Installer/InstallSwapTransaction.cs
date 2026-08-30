using System.IO;

namespace NeoTwitch.Installer;

internal sealed class InstallSwapTransaction : IDisposable
{
    private readonly string _targetPath;
    private readonly string? _rollbackPath;
    private bool _committed;
    private bool _disposed;

    private InstallSwapTransaction(string targetPath, string? rollbackPath)
    {
        _targetPath = targetPath;
        _rollbackPath = rollbackPath;
    }

    public static InstallSwapTransaction Activate(
        string stagingPath,
        string targetPath,
        InstallTargetKind? expectedTargetKind = null)
    {
        var staging = Normalize(stagingPath);
        var target = Normalize(targetPath);
        ValidateSiblingPaths(staging, target);
        ValidatePreparedDirectory(staging);

        var targetClassification = InstallTargetClassifier.Classify(target);
        if (targetClassification.Kind is InstallTargetKind.UnsafeTarget or InstallTargetKind.InvalidTarget
            || (expectedTargetKind is not null && targetClassification.Kind != expectedTargetKind))
        {
            throw new InvalidOperationException("El destino cambió después de su validación; la instalación se canceló de forma segura.");
        }

        var parent = Path.GetDirectoryName(target)!;
        var rollback = Directory.Exists(target)
            ? Path.Combine(parent, $".{Path.GetFileName(target)}.rollback.{Guid.NewGuid():N}")
            : null;

        try
        {
            if (rollback is not null)
            {
                Directory.Move(target, rollback);
            }

            Directory.Move(staging, target);
            ValidatePreparedDirectory(target);
            return new InstallSwapTransaction(target, rollback);
        }
        catch
        {
            if (Directory.Exists(target)
                && (rollback is null || Directory.Exists(rollback)))
            {
                SafeDeleteGeneratedDirectory(target);
            }

            if (rollback is not null && Directory.Exists(rollback) && !Directory.Exists(target))
            {
                Directory.Move(rollback, target);
            }

            throw;
        }
    }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _committed = true;
        if (_rollbackPath is not null)
        {
            try
            {
                SafeDeleteGeneratedDirectory(_rollbackPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The new installation is already active. A locked rollback directory is safe to retain.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_committed)
        {
            return;
        }

        var parent = Path.GetDirectoryName(_targetPath)!;
        var failedPath = Path.Combine(parent, $".{Path.GetFileName(_targetPath)}.failed.{Guid.NewGuid():N}");
        if (Directory.Exists(_targetPath))
        {
            Directory.Move(_targetPath, failedPath);
        }

        try
        {
            if (_rollbackPath is not null && Directory.Exists(_rollbackPath))
            {
                Directory.Move(_rollbackPath, _targetPath);
            }
        }
        finally
        {
            SafeDeleteGeneratedDirectory(failedPath);
        }
    }

    internal static void SafeDeleteGeneratedDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var info = new DirectoryInfo(path);
        Directory.Delete(path, recursive: !info.Attributes.HasFlag(FileAttributes.ReparsePoint));
    }

    private static void ValidatePreparedDirectory(string path)
    {
        var info = new DirectoryInfo(path);
        if (!info.Exists || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("La instalación preparada no es un directorio local seguro.");
        }

        if (!File.Exists(Path.Combine(path, NeoTwitch.Shared.NeoTwitchProduct.AppExecutableName))
            || !File.Exists(Path.Combine(path, NeoTwitch.Shared.NeoTwitchProduct.InstallMarkerFileName)))
        {
            throw new InvalidOperationException("La instalación preparada no contiene ejecutable y marcador válidos.");
        }
    }

    private static void ValidateSiblingPaths(string staging, string target)
    {
        var stagingParent = Path.GetDirectoryName(staging);
        var targetParent = Path.GetDirectoryName(target);
        if (stagingParent is null || targetParent is null
            || !string.Equals(stagingParent, targetParent, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La instalación preparada debe estar en el mismo directorio y volumen que el destino.");
        }
    }

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
