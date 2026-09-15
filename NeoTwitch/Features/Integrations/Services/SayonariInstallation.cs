using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace NeoTwitch.Services.Integrations;

public sealed class SayonariInstallation
{
    public const string CaptionsRevision = "4a31ac35042e7dd6587ae201e094fed260c07395";
    public const string ChatRevision = "b55e75aebdc0a8b9becf48e6dd062d7be5862bf6";
    public bool IsCaptions { get; }
    public string Repository => IsCaptions ? "jimakuChan" : "twitchTransFreeNext";
    public string Revision => IsCaptions ? CaptionsRevision : ChatRevision;
    public string Root { get; }
    public string Current => Path.Combine(Root, "current");
    public string Project => Path.Combine(Current, "source", Repository + "-" + Revision);
    public string Python => Path.Combine(Current, "runtime", "tools", "python.exe");
    public bool IsInstalled => File.Exists(Path.Combine(Current, "ready")) && File.Exists(Path.Combine(Project,
        IsCaptions ? "v2/js/recognizer.js" : "twitchTransFN.py")) && (IsCaptions || File.Exists(Python));
    public SayonariInstallation(bool captions, string? root = null)
    {
        IsCaptions = captions;
        Root = Path.GetFullPath(root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NeoTwitch", "integrations", captions ? "live-captions" : "chat-translator"));
    }
    public FileStream AcquireLease()
    {
        Directory.CreateDirectory(Root);
        return new FileStream(Path.Combine(Root, "session.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    public async Task InstallAsync(IProgress<string> progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsCaptions && RuntimeInformation.OSArchitecture != Architecture.X64)
            throw new NotSupportedException("Chat Translator requiere Windows x64.");
        using var lease = AcquireLease();
        var stage = Path.Combine(Root, "staging");
        var previous = Path.Combine(Root, "previous");
        try
        {
            DeleteManaged(stage);
            Directory.CreateDirectory(stage);
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            await CatCamInstallation.DownloadAsync(http, $"https://codeload.github.com/sayonari/{Repository}/zip/{Revision}",
                Path.Combine(stage, "source.zip"), IsCaptions
                    ? "CF08CDFAF6ED2B81F557700BCD3485461AF5360C2F7E6B7588B43CC78892A687"
                    : "8992ECD4A9433A30A5D1B767A2AC237427A4CF3C8AEE3D1537AC685641CBEBDD", progress, Repository, token);
            await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(stage, "source.zip"), Path.Combine(stage, "source")), token);
            var project = Path.Combine(stage, "source", Repository + "-" + Revision);
            if (!IsCaptions)
            {
                await CatCamInstallation.DownloadAsync(http, "https://api.nuget.org/v3-flatcontainer/python/3.10.11/python.3.10.11.nupkg",
                    Path.Combine(stage, "python.zip"), "7C6F99B160A36A7E09492DFCFF2B0A3A60BB5229CA44CDCC3ECB32871A6144D0", progress, "Python privado", token);
                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(stage, "python.zip"), Path.Combine(stage, "runtime")), token);
                var python = Path.Combine(stage, "runtime", "tools", "python.exe");
                progress.Report("Instalando dependencias de Chat Translator…");
                await CatCamInstallation.RunAsync(python, project, token, "-m", "pip", "--isolated", "install",
                    "--disable-pip-version-check", "--no-warn-script-location", "--index-url", "https://pypi.org/simple",
                    "-r", Path.Combine(project, "requirements.txt"));
                await CatCamInstallation.RunAsync(python, project, token, "-c", "import twitchTransFN; twitchTransFN.db.close()");
                File.Delete(Path.Combine(stage, "python.zip"));
            }
            if (!File.Exists(Path.Combine(project, "LICENSE"))) throw new IOException("Falta la licencia del proyecto original.");
            File.Delete(Path.Combine(stage, "source.zip"));
            await File.WriteAllTextAsync(Path.Combine(stage, "CREDITS.txt"),
                $"{Repository} — さぁたん & さよなりω (sayonari / Ryota Nishimura)\nhttps://github.com/sayonari/{Repository}\nMIT: source/{Repository}-{Revision}/LICENSE\nIntegración para Neo Twitch por Dafovi.\n", token);
            await File.WriteAllTextAsync(Path.Combine(stage, "ready"), Revision, token);
            token.ThrowIfCancellationRequested();
            DeleteManaged(previous);
            if (Directory.Exists(Current)) Directory.Move(Current, previous);
            try { Directory.Move(stage, Current); }
            catch { if (Directory.Exists(previous)) Directory.Move(previous, Current); throw; }
            progress.Report("Instalación lista.");
        }
        finally { DeleteManaged(stage); }
    }
    public void Uninstall()
    {
        using var lease = AcquireLease();
        foreach (var name in new[] { "current", "staging", "previous" }) DeleteManaged(Path.Combine(Root, name));
    }
    private void DeleteManaged(string path)
    {
        if (!Path.GetFullPath(path).StartsWith(Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Carpeta fuera de la integración.");
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
