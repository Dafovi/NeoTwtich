using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NeoTwitch.Services.Integrations;

/// <summary>Downloads upstream software only on request; no third-party files are shipped by NeoTwitch.</summary>
public sealed class CatCamInstallation
{
    public const string Repository = "https://github.com/catherpiee/meowmeowcatcam";
    public const string Revision = "9cae127fb922df948a1348f2fcfc7083f7746895";
    private const string RuntimeUrl = "https://api.nuget.org/v3-flatcontainer/python/3.12.10/python.3.12.10.nupkg";
    private const string RuntimeHash = "0EB85C2DFCCCCF1B17352DE4C397F69194035B7D37149EACC16F1147D93DE3B8";
    private const string ProjectHash = "C336AE36F257A403F91CA5C56E2F471E876EBFA1D84FB9360074C6157A925DC0";
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string Root { get; }
    public string Current => Path.Combine(Root, "current");
    public string Python => Path.Combine(Current, "runtime", "tools", "python.exe");
    public string Project => Path.Combine(Current, "source", "meowmeowcatcam-" + Revision);
    public bool IsInstalled => File.Exists(Path.Combine(Current, "ready")) && File.Exists(Python)
        && File.Exists(Path.Combine(Project, "gesture_meme.py"));

    public CatCamInstallation(string? root = null) => Root = Path.GetFullPath(root ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeoTwitch", "integrations", "meowmeowcatcam"));

    public async Task InstallAsync(IProgress<string> progress, CancellationToken token)
    {
        if (RuntimeInformation.OSArchitecture != Architecture.X64)
            throw new NotSupportedException("Esta integración requiere Windows de 64 bits (x64).");
        await _gate.WaitAsync(token);
        var stage = Path.Combine(Root, "staging");
        var backup = Path.Combine(Root, "previous");
        var ownsInstallation = false;
        FileStream? installationLock = null;
        try
        {
            Directory.CreateDirectory(Root);
            installationLock = new FileStream(Path.Combine(Root, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            ownsInstallation = true;
            DeleteOwnedDirectory(stage);
            Directory.CreateDirectory(stage);
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            await DownloadAsync(http, RuntimeUrl, Path.Combine(stage, "python.zip"), RuntimeHash, progress, "Python privado", token);
            await DownloadAsync(http, $"https://codeload.github.com/catherpiee/meowmeowcatcam/zip/{Revision}",
                Path.Combine(stage, "project.zip"), ProjectHash, progress, "MeowMeowCatCam de catherpiee", token);
            progress.Report("Preparando los archivos…");
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Combine(stage, "python.zip"), Path.Combine(stage, "runtime"));
                token.ThrowIfCancellationRequested();
                ZipFile.ExtractToDirectory(Path.Combine(stage, "project.zip"), Path.Combine(stage, "source"));
            }, token);
            var python = Path.Combine(stage, "runtime", "tools", "python.exe");
            var project = Path.Combine(stage, "source", "meowmeowcatcam-" + Revision);
            progress.Report("Instalando dependencias. Puede tardar varios minutos…");
            await RunAsync(python, stage, token, "-m", "pip", "--isolated", "install", "--disable-pip-version-check",
                "--no-warn-script-location", "--only-binary=:all:", "--index-url", "https://pypi.org/simple",
                "-r", Path.Combine(project, "requirements.txt"));
            progress.Report("Comprobando modelos, imágenes y dependencias…");
            await RunAsync(python, project, token, "-c",
                "import gesture_meme as g; from mediapipe.tasks.python import BaseOptions; " +
                "from mediapipe.tasks.python.vision import HandLandmarker,HandLandmarkerOptions,FaceLandmarker,FaceLandmarkerOptions; " +
                "g.load_memes(); " +
                "h=HandLandmarker.create_from_options(HandLandmarkerOptions(base_options=BaseOptions(model_asset_path=str(g.MODELS/'hand_landmarker.task')))); h.close(); " +
                "f=FaceLandmarker.create_from_options(FaceLandmarkerOptions(base_options=BaseOptions(model_asset_path=str(g.MODELS/'face_landmarker.task')))); f.close()");
            await File.WriteAllTextAsync(Path.Combine(stage, "ready"), Revision, token);
            await File.WriteAllTextAsync(Path.Combine(stage, "CREDITS.txt"),
                $"MeowMeowCatCam por catherpiee\n{Repository}\nRevision: {Revision}\nDescargado del repositorio original a petición del usuario.\nPython: Python Software Foundation (licencia en runtime/tools).\n", token);
            File.Delete(Path.Combine(stage, "python.zip"));
            File.Delete(Path.Combine(stage, "project.zip"));
            token.ThrowIfCancellationRequested();
            DeleteOwnedDirectory(backup);
            if (Directory.Exists(Current)) Directory.Move(Current, backup);
            try { Directory.Move(stage, Current); }
            catch
            {
                if (Directory.Exists(backup)) Directory.Move(backup, Current);
                throw;
            }
            progress.Report("Instalación lista. Ya puedes activar la cámara en OBS.");
        }
        finally
        {
            try { if (ownsInstallation) DeleteOwnedDirectory(stage); }
            finally { installationLock?.Dispose(); _gate.Release(); }
        }
    }

    public async Task UninstallAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(Root);
            using var installationLock = new FileStream(Path.Combine(Root, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            foreach (var name in new[] { "current", "previous", "staging" })
                DeleteOwnedDirectory(Path.Combine(Root, name));
        }
        finally { _gate.Release(); }
    }

    internal void DeleteOwnedDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La carpeta está fuera de la integración.");
        if (Directory.Exists(full)) Directory.Delete(full, true);
    }

    internal static async Task DownloadAsync(HttpClient http, string url, string path, string hash,
        IProgress<string> progress, string label, CancellationToken token)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(token);
        await using (var output = File.Create(path))
        {
            var buffer = new byte[81920];
            long total = 0;
            var last = -1;
            int count;
            while ((count = await input.ReadAsync(buffer, token)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, count), token);
                total += count;
                var mb = (int)(total / 1048576);
                if (mb != last) { progress.Report($"Descargando {label}: {mb} MB…"); last = mb; }
            }
        }
        await using var file = File.OpenRead(path);
        if (!Convert.ToHexString(await SHA256.HashDataAsync(file, token)).Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"La descarga de {label} no coincide con la versión verificada. Vuelve a intentarlo.");
    }

    internal static ProcessStartInfo StartInfo(string executable, string directory, params string[] args)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) info.ArgumentList.Add(arg);
        info.Environment.Remove("PYTHONPATH");
        info.Environment.Remove("PYTHONHOME");
        info.Environment["PYTHONNOUSERSITE"] = "1";
        info.Environment["PYTHONUTF8"] = "1";
        info.Environment["MPLCONFIGDIR"] = Path.Combine(directory, ".matplotlib");
        return info;
    }

    internal static async Task RunAsync(string executable, string directory, CancellationToken token, params string[] args)
    {
        using var process = Process.Start(StartInfo(executable, directory, args)) ?? throw new IOException("No se pudo iniciar Python.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }
        var detail = (await stderr) + (await stdout);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("No se pudo preparar la integración. " + detail[..Math.Min(detail.Length, 2500)]);
    }
}
