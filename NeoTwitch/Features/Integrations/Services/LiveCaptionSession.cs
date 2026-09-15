using System.Diagnostics;
using System.IO;

namespace NeoTwitch.Services.Integrations;

public sealed class LiveCaptionSession : IDisposable
{
    private Process? _process;
    private FileStream? _lease;
    public static string FindChrome()
    {
        var paths = new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) };
        return paths.Select(root => Path.Combine(root, "Google", "Chrome", "Application", "chrome.exe")).FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Live Captions necesita Google Chrome instalado. Instálalo desde google.com/chrome y vuelve a activar.");
    }
    public void Start(SayonariInstallation installation, string url)
    {
        var chrome = FindChrome();
        _lease = installation.AcquireLease();
        try
        {
            var info = new ProcessStartInfo(chrome) { UseShellExecute = false };
            info.ArgumentList.Add("--user-data-dir=" + Path.Combine(installation.Root, "chrome-profile"));
            info.ArgumentList.Add("--no-first-run");
            info.ArgumentList.Add("--disable-background-mode");
            info.ArgumentList.Add("--disable-background-timer-throttling");
            info.ArgumentList.Add("--disable-renderer-backgrounding");
            info.ArgumentList.Add("--disable-backgrounding-occluded-windows");
            info.ArgumentList.Add("--app=" + url);
            _process = Process.Start(info) ?? throw new IOException("No se pudo abrir Chrome.");
        }
        catch { Dispose(); throw; }
    }
    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false }) { _process.Kill(true); _process.WaitForExit(5000); }
        }
        finally { _process?.Dispose(); _process = null; _lease?.Dispose(); _lease = null; }
    }
}
