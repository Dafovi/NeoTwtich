using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Integrations;

public sealed class ChatTranslationSession : IAsyncDisposable
{
    private readonly Channel<(string User, string Text)> _queue = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(20)
        { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    private readonly CancellationTokenSource _stop = new();
    private Process? _process;
    private FileStream? _lease;
    private Task? _runner;
    private volatile bool _running;
    private int _disposed;
    private SubtitleIntegrationConfig _config = new();
    public string Status { get; private set; } = "Detenida";
    public bool IsRunning => _running && _runner is { IsCompleted: false };
    public void Enqueue(string user, string text)
    {
        if (!IsRunning || string.IsNullOrWhiteSpace(text) || text.StartsWith('!') || text.StartsWith("[NT Traducción]", StringComparison.Ordinal)) return;
        if (_config.IgnoredUsers.Split(',').Any(u => string.Equals(u.Trim(), user, StringComparison.OrdinalIgnoreCase))) return;
        _queue.Writer.TryWrite((user, text[..Math.Min(text.Length, 500)]));
    }
    public async Task StartAsync(SayonariInstallation installation, SubtitleIntegrationConfig config,
        Func<string, string, string, Task> onTranslation, CancellationToken token)
    {
        _config = config.Snapshot();
        _lease = installation.AcquireLease();
        try
        {
            var script = Path.Combine(installation.Project, "neotwitch-chat-bridge.py");
            await File.WriteAllTextAsync(script, SubtitleServer.Asset("chat-bridge.py"), token);
            var info = CatCamInstallation.StartInfo(installation.Python, installation.Project, "-u", script);
            info.RedirectStandardInput = true;
            _process = Process.Start(info) ?? throw new IOException("No se pudo iniciar Chat Translator.");
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) =>
            {
                if (_running) Status = "El traductor se cerró. Vuelve a activar la integración.";
                _running = false;
            };
            _process.ErrorDataReceived += (_, _) => { };
            _process.BeginErrorReadLine();
            await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(_config, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            var ready = await _process.StandardOutput.ReadLineAsync(token).AsTask().WaitAsync(TimeSpan.FromSeconds(30), token);
            if (ready is null || !ready.Contains("\"ready\"", StringComparison.Ordinal)) throw new IOException("Chat Translator no pudo iniciar. Repara su instalación.");
            Status = "Activo. Esperando mensajes del chat…";
            _running = true;
            _runner = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in _queue.Reader.ReadAllAsync(_stop.Token))
                    {
                        await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { user = item.User, text = item.Text }));
                        var line = await _process.StandardOutput.ReadLineAsync(_stop.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(25), _stop.Token);
                        if (line is null) throw new IOException("El traductor se cerró.");
                        using var response = JsonDocument.Parse(line);
                        var text = response.RootElement.GetProperty("text").GetString() ?? "";
                        if (response.RootElement.GetProperty("kind").GetString() == "error") Status = text;
                        else if (!string.IsNullOrWhiteSpace(text))
                        {
                            _stop.Token.ThrowIfCancellationRequested();
                            await onTranslation(item.User, item.Text, text);
                            Status = "Activo. Traducción recibida.";
                        }
                        else Status = "Activo. Mensaje filtrado o sin traducción disponible.";
                        await Task.Delay(2000, _stop.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { Status = "El traductor dejó de responder. Detén y vuelve a activar la integración."; }
            });
        }
        catch { await DisposeAsync(); throw; }
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _running = false;
        _stop.Cancel();
        _queue.Writer.TryComplete();
        try
        {
            if (_process is { HasExited: false }) _process.Kill(true);
            if (_runner is not null) await _runner;
            if (_process is not null) { await _process.WaitForExitAsync(); _process.Dispose(); _process = null; }
        }
        finally { _lease?.Dispose(); _lease = null; _stop.Dispose(); }
    }
}
