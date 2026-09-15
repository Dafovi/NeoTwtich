using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace NeoTwitch.Services.Integrations;

public sealed class CatCamSession : IDisposable
{
    private Process? _process;
    private FileStream? _installationLock;
    private readonly StringBuilder _errors = new();
    public bool IsRunning => _process is { HasExited: false };
    public string Error { get { lock (_errors) return _errors.ToString(); } }

    public async Task<IReadOnlyDictionary<string, string>> StartAsync(CatCamInstallation installation, CancellationToken token)
    {
        if (IsRunning) throw new InvalidOperationException("La cámara ya está activa.");
        if (!installation.IsInstalled) throw new InvalidOperationException("Instala primero la integración.");
        Dispose();
        lock (_errors) _errors.Clear();
        var process = new Process { StartInfo = CatCamInstallation.StartInfo(installation.Python,
            installation.Project, "-u", "gesture_meme.py") };
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (_errors)
            {
                _errors.AppendLine(e.Data);
                if (_errors.Length > 3000) _errors.Remove(0, _errors.Length - 3000);
            }
        };
        _process = process;
        try
        {
            _installationLock = new FileStream(Path.Combine(installation.Root, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            while (true)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (process.HasExited)
                    throw new InvalidOperationException("La cámara no pudo iniciarse. Revisa los permisos de cámara de Windows y que no esté ocupada. " + Error);
                var windows = FindWindows(process.Id);
                if (windows.Count == 2) return windows;
                await Task.Delay(250, timeout.Token);
            }
        }
        catch { Dispose(); throw; }
    }

    private static Dictionary<string, string> FindWindows(int processId)
    {
        var result = new Dictionary<string, string>();
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var pid);
            if (pid != processId || !IsWindowVisible(window)) return true;
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            if (title.ToString() is not ("Camera" or "Meme")) return true;
            var className = new StringBuilder(256);
            GetClassName(window, className, className.Capacity);
            result[title.ToString()] = $"{title}:{className}:python.exe";
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public void Dispose()
    {
        var process = _process;
        _process = null;
        if (process is null) { _installationLock?.Dispose(); _installationLock = null; return; }
        try
        {
            if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); }
        }
        catch (InvalidOperationException) { }
        finally { process.Dispose(); _installationLock?.Dispose(); _installationLock = null; }
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int maxCount);
}
