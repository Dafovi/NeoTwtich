using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using NeoTwitch.Shared;

namespace NeoTwitch.Installer;

internal enum InstallTargetKind
{
    NewInstallTarget,
    ExistingNeoTwitchInstallation,
    UnsafeTarget,
    InvalidTarget
}

internal sealed record InstallTargetClassification(
    InstallTargetKind Kind,
    string NormalizedPath,
    string Reason);

internal static class InstallTargetClassifier
{
    private static readonly Guid DownloadsKnownFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    public static InstallTargetClassification Classify(
        string? targetPath,
        IEnumerable<string>? additionalProtectedRoots = null)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return Invalid("", "La carpeta de instalación está vacía.");
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(targetPath.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Invalid(targetPath, "La ruta de instalación no es válida.");
        }

        if (File.Exists(normalizedPath))
        {
            return Invalid(normalizedPath, "La ruta de instalación apunta a un archivo.");
        }

        if (IsProtectedRoot(normalizedPath, additionalProtectedRoots))
        {
            return Unsafe(normalizedPath, "La carpeta seleccionada es una ubicación raíz protegida.");
        }

        if (!Directory.Exists(normalizedPath))
        {
            return New(normalizedPath, "La carpeta todavía no existe.");
        }

        try
        {
            if ((File.GetAttributes(normalizedPath) & FileAttributes.ReparsePoint) != 0)
            {
                return Unsafe(normalizedPath, "La carpeta seleccionada es un vínculo o punto de reanálisis.");
            }

            if (Directory.Exists(Path.Combine(normalizedPath, ".git"))
                || File.Exists(Path.Combine(normalizedPath, ".git")))
            {
                return Unsafe(normalizedPath, "La carpeta seleccionada es la raíz de un repositorio.");
            }

            if (!Directory.EnumerateFileSystemEntries(normalizedPath).Any())
            {
                return New(normalizedPath, "La carpeta está vacía.");
            }

            return HasValidProductMarker(normalizedPath)
                ? Existing(normalizedPath, "Se verificó el marcador de instalación de Neo Twitch.")
                : Unsafe(normalizedPath, "La carpeta contiene archivos pero no es una instalación verificada de Neo Twitch.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Invalid(normalizedPath, "No se pudo inspeccionar de forma segura la carpeta de instalación.");
        }
    }

    private static bool HasValidProductMarker(string installPath)
    {
        var markerPath = Path.Combine(installPath, NeoTwitchProduct.InstallMarkerFileName);
        var appPath = Path.Combine(installPath, NeoTwitchProduct.AppExecutableName);
        if (!File.Exists(markerPath) || !File.Exists(appPath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(markerPath));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("productId", out var productId))
            {
                return productId.ValueKind == JsonValueKind.String
                    && string.Equals(productId.GetString(), NeoTwitchProduct.ProductIdentifier, StringComparison.Ordinal)
                    && root.TryGetProperty("schemaVersion", out var schemaVersion)
                    && schemaVersion.TryGetInt32(out var schema)
                    && schema == NeoTwitchProduct.InstallMarkerSchemaVersion
                    && HasNonEmptyString(root, "version");
            }

            // Compatibility with manifests written by released installers before the product marker schema.
            return HasNonEmptyString(root, "version")
                && HasNonEmptyString(root, "installedAt")
                && root.TryGetProperty("installPath", out var legacyInstallPath)
                && legacyInstallPath.ValueKind == JsonValueKind.String
                && PathsEqual(legacyInstallPath.GetString(), installPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private static bool HasNonEmptyString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString());

    private static bool IsProtectedRoot(string path, IEnumerable<string>? additionalProtectedRoots)
    {
        var driveRoot = Path.GetPathRoot(path);
        if (!string.IsNullOrWhiteSpace(driveRoot) && PathsEqual(path, driveRoot))
        {
            return true;
        }

        var protectedRoots = GetSystemProtectedRoots();
        if (additionalProtectedRoots is not null)
        {
            protectedRoots.AddRange(additionalProtectedRoots);
        }

        return protectedRoots.Any(root => PathsEqual(path, root));
    }

    private static List<string> GetSystemProtectedRoots()
    {
        var roots = new List<string>();
        AddSpecialFolder(roots, Environment.SpecialFolder.UserProfile);
        AddSpecialFolder(roots, Environment.SpecialFolder.DesktopDirectory);
        AddSpecialFolder(roots, Environment.SpecialFolder.MyDocuments);
        AddSpecialFolder(roots, Environment.SpecialFolder.CommonDesktopDirectory);
        AddSpecialFolder(roots, Environment.SpecialFolder.CommonDocuments);
        AddSpecialFolder(roots, Environment.SpecialFolder.ProgramFiles);
        AddSpecialFolder(roots, Environment.SpecialFolder.ProgramFilesX86);
        AddSpecialFolder(roots, Environment.SpecialFolder.CommonApplicationData);
        AddSpecialFolder(roots, Environment.SpecialFolder.ApplicationData);
        AddSpecialFolder(roots, Environment.SpecialFolder.LocalApplicationData);
        AddSpecialFolder(roots, Environment.SpecialFolder.System);
        AddSpecialFolder(roots, Environment.SpecialFolder.SystemX86);

        AddKnownFolder(roots, DownloadsKnownFolderId);

        var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");
        if (!string.IsNullOrWhiteSpace(windowsDirectory))
        {
            roots.Add(windowsDirectory);
        }

        return roots;
    }

    private static void AddSpecialFolder(List<string> roots, Environment.SpecialFolder folder)
    {
        var path = Environment.GetFolderPath(folder);
        if (!string.IsNullOrWhiteSpace(path))
        {
            roots.Add(path);
        }
    }

    private static void AddKnownFolder(List<string> roots, Guid folderId)
    {
        if (!OperatingSystem.IsWindows()
            || SHGetKnownFolderPath(folderId, 0, IntPtr.Zero, out var nativePath) != 0)
        {
            return;
        }

        try
        {
            var path = Marshal.PtrToStringUni(nativePath);
            if (!string.IsNullOrWhiteSpace(path))
            {
                roots.Add(path);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(nativePath);
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        in Guid rfid,
        uint flags,
        IntPtr token,
        out IntPtr path);

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static InstallTargetClassification New(string path, string reason) =>
        new(InstallTargetKind.NewInstallTarget, path, reason);

    private static InstallTargetClassification Existing(string path, string reason) =>
        new(InstallTargetKind.ExistingNeoTwitchInstallation, path, reason);

    private static InstallTargetClassification Unsafe(string path, string reason) =>
        new(InstallTargetKind.UnsafeTarget, path, reason);

    private static InstallTargetClassification Invalid(string path, string reason) =>
        new(InstallTargetKind.InvalidTarget, path, reason);
}
