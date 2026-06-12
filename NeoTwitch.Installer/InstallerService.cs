using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace NeoTwitch.Installer;

internal sealed class InstallerService
{
    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string WindowsStartupValueName = "Neo Twitch";

    private readonly GitHubReleaseClient _releaseClient = new();

    public async Task<InstallResult> InstallAsync(
        InstallerOptions options,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        progress.Report(new InstallProgress(3, "Preparando instalación"));
        var tempRoot = Path.Combine(Path.GetTempPath(), $"NeoTwitchInstaller_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            await WaitForNeoTwitchToExitAsync(progress, cancellationToken);

            var packagePath = options.PackagePath;
            string version;
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                var asset = await _releaseClient.GetLatestInstallAssetAsync(cancellationToken);
                version = NormalizeVersion(asset.Version);
                packagePath = await _releaseClient.DownloadAsync(asset, tempRoot, progress, cancellationToken);
            }
            else
            {
                version = string.IsNullOrWhiteSpace(options.RequestedVersion)
                    ? "local"
                    : NormalizeVersion(options.RequestedVersion);
            }

            progress.Report(new InstallProgress(50, "Copiando archivos"));
            Directory.CreateDirectory(options.InstallPath);
            InstallPackage(packagePath, options.InstallPath, tempRoot);
            CopyInstallerToTarget(options.InstallPath);

            progress.Report(new InstallProgress(74, "Creando accesos directos"));
            var appExePath = Path.Combine(options.InstallPath, "NeoTwitch.exe");
            if (!File.Exists(appExePath))
            {
                throw new FileNotFoundException("La instalación no encontró NeoTwitch.exe.", appExePath);
            }

            if (options.CreateDesktopShortcut)
            {
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Neo Twitch.lnk"),
                    appExePath);
            }

            if (options.CreateStartMenuShortcut)
            {
                var startMenuFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "Neo Twitch");
                Directory.CreateDirectory(startMenuFolder);
                CreateShortcut(Path.Combine(startMenuFolder, "Neo Twitch.lnk"), appExePath);
            }

            progress.Report(new InstallProgress(86, "Configurando inicio con Windows"));
            ApplyStartWithWindows(options.StartWithWindows, appExePath);
            WriteAppStartWithWindowsSetting(options.StartWithWindows);
            WriteInstallManifest(options.InstallPath, version);

            progress.Report(new InstallProgress(100, options.IsUpdate ? "Actualización completada" : "Instalación completada"));
            return new InstallResult(appExePath, version);
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
            var running = Process.GetProcessesByName("NeoTwitch")
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

    private static void InstallPackage(string packagePath, string installPath, string tempRoot)
    {
        if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var extractPath = Path.Combine(tempRoot, "extract");
            ZipFile.ExtractToDirectory(packagePath, extractPath, overwriteFiles: true);
            var sourceRoot = ResolvePackageRoot(extractPath);
            CleanInstallPath(installPath);
            CopyDirectory(sourceRoot, installPath);
            return;
        }

        if (packagePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            CleanInstallPath(installPath);
            File.Copy(packagePath, Path.Combine(installPath, "NeoTwitch.exe"), overwrite: true);
            return;
        }

        throw new InvalidOperationException("El paquete descargado no es .exe ni .zip.");
    }

    private static string ResolvePackageRoot(string extractPath)
    {
        if (File.Exists(Path.Combine(extractPath, "NeoTwitch.exe")))
        {
            return extractPath;
        }

        var nested = Directory
            .EnumerateDirectories(extractPath, "*", SearchOption.AllDirectories)
            .FirstOrDefault(directory => File.Exists(Path.Combine(directory, "NeoTwitch.exe")));

        return nested ?? throw new InvalidOperationException("El paquete no contiene NeoTwitch.exe.");
    }

    private static void CleanInstallPath(string installPath)
    {
        Directory.CreateDirectory(installPath);

        foreach (var file in Directory.EnumerateFiles(installPath))
        {
            if (Path.GetFileName(file).StartsWith("NeoTwitch.Installer", StringComparison.OrdinalIgnoreCase))
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

        var targetPath = Path.Combine(installPath, "NeoTwitch.Installer.exe");
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
            runKey?.SetValue(WindowsStartupValueName, $"\"{appExePath}\"");
        }
        else
        {
            runKey?.DeleteValue(WindowsStartupValueName, throwOnMissingValue: false);
        }
    }

    private static void WriteAppStartWithWindowsSetting(bool enabled)
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NeoTwitch");
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

    private static void WriteInstallManifest(string installPath, string version)
    {
        var manifestPath = Path.Combine(installPath, "neo-twitch-install.json");
        var installedAt = DateTimeOffset.Now.ToString("O");
        var content = $$"""
        {
          "version": "{{version}}",
          "installedAt": "{{installedAt}}",
          "installPath": "{{EscapeJson(installPath)}}"
        }
        """;
        File.WriteAllText(manifestPath, content);
    }

    private static string NormalizeVersion(string version)
    {
        var text = version.Trim();
        return text.StartsWith('v') || text.StartsWith('V') ? text[1..] : text;
    }

    private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

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

internal sealed record InstallResult(string AppExePath, string Version);
