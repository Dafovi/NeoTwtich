using System.Windows;
using System.Windows.Threading;
using NeoTwitch.Services.Integrations;

namespace NeoTwitch.Views;

public partial class IntegrationsView : NeoTwitchView
{
    private readonly CatCamInstallation _installation = new();
    private readonly CatCamSession _session = new();
    private readonly DispatcherTimer _monitor = new() { Interval = TimeSpan.FromSeconds(2) };
    private CancellationTokenSource? _operation;
    private Task _pending = Task.CompletedTask;
    private string? _activeScene;
    private bool _shuttingDown;
    private bool _preferencesLoaded;

    public IntegrationsView()
    {
        InitializeComponent();
        _monitor.Tick += Monitor_Tick;
        StatusText.Text = _installation.IsInstalled ? "Instalada. Conecta OBS para activar." : "No instalada.";
        UpdateButtons();
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_preferencesLoaded && Host is not null)
        {
            OutputBox.SelectedIndex = Host.CatCamOutput;
            _preferencesLoaded = true;
        }
        if (Host is not null && _operation is null && !_session.IsRunning)
            await RunAsync(async token => await RefreshScenesAsync(token));
    }

    private void UpdateButtons()
    {
        var busy = _operation is not null || _shuttingDown;
        SetupActions.IsEnabled = !busy && !_session.IsRunning;
        InstallButton.Content = _installation.IsInstalled ? "Reparar instalación" : "Instalar";
        UninstallButton.IsEnabled = _installation.IsInstalled;
        OptionsPanel.IsEnabled = !busy && !_session.IsRunning;
        StartButton.IsEnabled = !busy && _installation.IsInstalled && !_session.IsRunning;
        StopButton.IsEnabled = !busy && (_session.IsRunning || _activeScene is not null);
        CancelButton.Visibility = busy && !_shuttingDown ? Visibility.Visible : Visibility.Collapsed;
        ProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (_operation is not null || _shuttingDown) return Task.CompletedTask;
        return _pending = RunCoreAsync(action);
    }

    private async Task RunCoreAsync(Func<CancellationToken, Task> action)
    {
        using var operation = new CancellationTokenSource();
        _operation = operation;
        UpdateButtons();
        try { await action(operation.Token); }
        catch (OperationCanceledException) { StatusText.Text = "Operación cancelada o tiempo de espera agotado. Puedes volver a intentarlo."; }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { _operation = null; UpdateButtons(); }
    }

    private async void Install_Click(object sender, RoutedEventArgs e) => await RunAsync(token =>
        _installation.InstallAsync(new Progress<string>(message => StatusText.Text = message), token));

    private async void Uninstall_Click(object sender, RoutedEventArgs e) => await RunAsync(async token =>
    {
        await StopAsync();
        await _installation.UninstallAsync(token);
        StatusText.Text = "Integración desinstalada. Las fuentes de OBS quedan ocultas para conservar tu composición.";
    });

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(RefreshScenesAsync);

    private async Task RefreshScenesAsync(CancellationToken token)
    {
        if (Host is null) return;
        var previous = SceneBox.SelectedItem as string ?? Host.CatCamScene;
        var scenes = await Host.GetIntegrationScenesAsync(token);
        SceneBox.ItemsSource = scenes;
        SceneBox.SelectedItem = scenes.Contains(previous) ? previous : scenes.FirstOrDefault();
    }

    private async void Start_Click(object sender, RoutedEventArgs e) => await RunAsync(async token =>
    {
        if (Host is null || SceneBox.SelectedItem is not string scene)
            throw new InvalidOperationException("Conecta OBS y selecciona una escena. Pulsa Actualizar escenas.");
        StatusText.Text = "Iniciando cámara y detección de gestos…";
        try
        {
            var windows = await _session.StartAsync(_installation, token);
            _activeScene = scene;
            await Host.ShowCatCamAsync(scene, windows, OutputBox.SelectedIndex, token);
            StatusText.Text = "Activa en OBS. Puedes ajustar el tamaño y la posición de las fuentes en tu escena.";
            _monitor.Start();
        }
        catch { await StopAsync(); throw; }
    });

    private async void Stop_Click(object sender, RoutedEventArgs e) => await RunAsync(async _ =>
    {
        await StopAsync();
        StatusText.Text = "Detenida. Cámara liberada.";
    });

    private async Task StopAsync()
    {
        _monitor.Stop();
        _session.Dispose();
        if (Host is not null && _activeScene is string scene)
        {
            await Host.HideCatCamAsync(scene);
            _activeScene = null;
        }
    }

    private async void Monitor_Tick(object? sender, EventArgs e)
    {
        if (_session.IsRunning || _operation is not null) return;
        await RunAsync(async _ =>
        {
            var detail = _session.Error;
            await StopAsync();
            StatusText.Text = "La aplicación de cámara se cerró. Puedes volver a activarla. " + detail;
        });
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _operation?.Cancel();
    private void Project_Click(object sender, RoutedEventArgs e) => Host?.OpenCatCamProject();

    public async Task ShutdownAsync()
    {
        _shuttingDown = true;
        _operation?.Cancel();
        await _pending;
        await StopAsync();
    }
}
