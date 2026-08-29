using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using NeoTwitch.Shared;

namespace NeoTwitch.Installer;

internal sealed class InstallerService
{
    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly IReleaseClient _releaseClient;
    private readonly TimeProvider _timeProvider;

    public InstallerService(IReleaseClient? releaseClient = null, TimeProvider? timeProvider = null)
    {
        _releaseClient = releaseClient ?? new GitHubReleaseClient();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<InstallResult> InstallAsync(
        InstallerOptions options,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new InstallProgress(3, "Preparando instalación"));
        ValidateInstallTarget(options);

        var tempRoot = Path.Combine(Path.GetTempPath(), $"{NeoTwitchProduct.GitHubInstallerUserAgent}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            await WaitForNeoTwitchToExitAsync(progress, cancellationToken);

            var packagePath = options.PackagePath;
            string version;
            var releaseNotes = "";
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                var asset = await _releaseClient.DownloadLatestVerifiedAsync(tempRoot, progress, cancellationToken);
                version = NormalizeVersion(asset.Version);
                if (!string.IsNullOrWhiteSpace(options.RequestedVersion)
                    && !string.Equals(
                        version,
                        NormalizeVersion(options.RequestedVersion),
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ReleaseIntegrityException(
                        ReleaseIntegrityFailure.VersionMismatch,
                        $"La versión verificada ({version}) no coincide con la actualización solicitada ({options.RequestedVersion}).");
                }

                releaseNotes = asset.ReleaseNotes;
                packagePath = asset.PackagePath;
            }
            else
            {
                var localTarget = ValidateInstallTarget(options);
                if (options.IsUpdate || localTarget.Kind != InstallTargetKind.NewInstallTarget)
                {
                    throw new InvalidOperationException(
                        "Los paquetes locales sin verificar solo se admiten para una instalación nueva en una carpeta vacía. "
                        + "Las actualizaciones deben usar el flujo automático con manifiesto firmado.");
                }

                version = string.IsNullOrWhiteSpace(options.RequestedVersion)
                    ? "local"
                    : NormalizeVersion(options.RequestedVersion);
            }

            progress.Report(new InstallProgress(50, "Copiando archivos"));
            InstallPackage(packagePath, options, tempRoot);
            CopyInstallerToTarget(options.InstallPath);

            progress.Report(new InstallProgress(74, "Creando accesos directos"));
            var appExePath = Path.Combine(options.InstallPath, NeoTwitchProduct.AppExecutableName);
            if (!File.Exists(appExePath))
            {
                throw new FileNotFoundException($"La instalación no encontró {NeoTwitchProduct.AppExecutableName}.", appExePath);
            }

            if (options.CreateDesktopShortcut)
            {
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), NeoTwitchProduct.ShortcutFileName),
                    appExePath);
            }

            if (options.CreateStartMenuShortcut)
            {
                var startMenuFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    NeoTwitchProduct.DisplayName);
                Directory.CreateDirectory(startMenuFolder);
                CreateShortcut(Path.Combine(startMenuFolder, NeoTwitchProduct.ShortcutFileName), appExePath);
            }

            progress.Report(new InstallProgress(86, "Configurando inicio con Windows"));
            ApplyStartWithWindows(options.StartWithWindows, appExePath);
            WriteAppStartWithWindowsSetting(options.StartWithWindows);
            WriteInstallManifest(options.InstallPath, version, _timeProvider.GetLocalNow());

            progress.Report(new InstallProgress(100, options.IsUpdate ? "Actualización completada" : "Instalación completada"));
            return new InstallResult(appExePath, version, releaseNotes);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static async Task WaitForNeoTwitchToExitAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        var currentProcessId = Environment.ProcessId;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var running = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(NeoTwitchProduct.AppExecutableName))
                .Where(process =>
                {
                    try
                    {
                        return process.Id != currentProcessId && !process.HasExited;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToArray();

            foreach (var process in running)
            {
                process.Dispose();
            }

            if (running.Length == 0)
            {
                return;
            }

            progress.Report(new InstallProgress(8, "Esperando a que Neo Twitch se cierre"));
            await Task.Delay(500, cancellationToken);
        }
    }

    private static void InstallPackage(string packagePath, InstallerOptions options, string tempRoot)
    {
        if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var extractPath = Path.Combine(tempRoot, "extract");
            ZipFile.ExtractToDirectory(packagePath, extractPath, overwriteFiles: true);
            var sourceRoot = ResolvePackageRoot(extractPath);
            PrepareInstallPath(options);
            CopyDirectory(sourceRoot, options.InstallPath);
            return;
        }

        if (packagePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            PrepareInstallPath(options);
            File.Copy(packagePath, Path.Combine(options.InstallPath, NeoTwitchProduct.AppExecutableName), overwrite: true);
            return;
        }

        throw new InvalidOperationException("El paquete descargado no es .exe ni .zip.");
    }

    private static string ResolvePackageRoot(string extractPath)
    {
        if (File.Exists(Path.Combine(extractPath, NeoTwitchProduct.AppExecutableName)))
        {
            return extractPath;
        }

        var nested = Directory
            .EnumerateDirectories(extractPath, "*", SearchOption.AllDirectories)
            .FirstOrDefault(directory => File.Exists(Path.Combine(directory, NeoTwitchProduct.AppExecutableName)));

        return nested ?? throw new InvalidOperationException($"El paquete no contiene {NeoTwitchProduct.AppExecutableName}.");
    }

    private static void PrepareInstallPath(InstallerOptions options)
    {
        var classification = ValidateInstallTarget(options);
        if (classification.Kind == InstallTargetKind.ExistingNeoTwitchInstallation)
        {
            CleanInstallPath(classification.NormalizedPath);
            return;
        }

        Directory.CreateDirectory(classification.NormalizedPath);
    }

    internal static InstallTargetClassification ValidateInstallTarget(InstallerOptions options)
    {
        var classification = InstallTargetClassifier.Classify(options.InstallPath);
        if (classification.Kind is InstallTargetKind.UnsafeTarget or InstallTargetKind.InvalidTarget)
        {
            throw new InvalidOperationException(
                $"No se puede instalar en '{options.InstallPath}'. {classification.Reason} "
                + "Elige una carpeta nueva o vacía, o la carpeta de una instalación válida de Neo Twitch.");
        }

        if (options.IsUpdate && classification.Kind != InstallTargetKind.ExistingNeoTwitchInstallation)
        {
            throw new InvalidOperationException(
                $"No se puede actualizar '{options.InstallPath}' porque no es una instalación verificada de Neo Twitch. "
                + "Inicia una instalación nueva sin --update o selecciona la carpeta de instalación existente.");
        }

        options.InstallPath = classification.NormalizedPath;
        return classification;
    }

    private static void CleanInstallPath(string installPath)
    {
        Directory.CreateDirectory(installPath);

        foreach (var file in Directory.EnumerateFiles(installPath))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith(Path.GetFileNameWithoutExtension(NeoTwitchProduct.InstallerExecutableName), StringComparison.OrdinalIgnoreCase)
                || fileName.Equals(NeoTwitchProduct.InstallMarkerFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Delete(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(installPath))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, file);
            if (relativePath.Equals(NeoTwitchProduct.InstallMarkerFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetPath = Path.Combine(target, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(file, targetPath, overwrite: true);
        }
    }

    private static void CopyInstallerToTarget(string installPath)
    {
        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
        {
            return;
        }

        var targetPath = Path.Combine(installPath, NeoTwitchProduct.InstallerExecutableName);
        if (string.Equals(
            Path.GetFullPath(currentExe),
            Path.GetFullPath(targetPath),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Copy(currentExe, targetPath, overwrite: true);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("No pude crear accesos directos en Windows.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private static void ApplyStartWithWindows(bool enabled, string appExePath)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(WindowsRunKeyPath, writable: true);
        if (enabled)
        {
            runKey?.SetValue(NeoTwitchProduct.StartupValueName, $"\"{appExePath}\"");
        }
        else
        {
            runKey?.DeleteValue(NeoTwitchProduct.StartupValueName, throwOnMissingValue: false);
        }
    }

    private static void WriteAppStartWithWindowsSetting(bool enabled)
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            NeoTwitchProduct.AppDataFolderName);
        Directory.CreateDirectory(settingsDirectory);

        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        JsonObject root;
        if (File.Exists(settingsPath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? [];
            }
            catch
            {
                root = [];
            }
        }
        else
        {
            root = [];
        }

        root["startWithWindows"] = enabled;
        File.WriteAllText(settingsPath, root.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private static void WriteInstallManifest(string installPath, string version, DateTimeOffset installedAt)
    {
        var manifestPath = Path.Combine(installPath, NeoTwitchProduct.InstallMarkerFileName);
        var temporaryManifestPath = $"{manifestPath}.{Guid.NewGuid():N}.tmp";
        var content = $$"""
        {
          "productId": "{{NeoTwitchProduct.ProductIdentifier}}",
          "schemaVersion": {{NeoTwitchProduct.InstallMarkerSchemaVersion}},
          "version": "{{version}}",
          "installedAt": "{{installedAt:O}}"
        }
        """;
        File.WriteAllText(temporaryManifestPath, content);
        File.Move(temporaryManifestPath, manifestPath, overwrite: true);
    }

    private static string NormalizeVersion(string version)
    {
        var text = version.Trim();
        return text.StartsWith('v') || text.StartsWith('V') ? text[1..] : text;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Temporary files are safe to leave behind if Windows still has a handle open.
        }
    }
}

internal sealed record InstallProgress(int Percent, string Message);

internal sealed record InstallResult(string AppExePath, string Version, string ReleaseNotes);
