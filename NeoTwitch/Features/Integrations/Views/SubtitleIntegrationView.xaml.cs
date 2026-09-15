using System.Windows;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services.Integrations;

namespace NeoTwitch.Views;

public partial class SubtitleIntegrationView : NeoTwitchView
{
    public bool CaptionMode { get; set; }
    private SayonariInstallation? _installation;
    private SubtitleServer? _server;
    private LiveCaptionSession? _captions;
    private ChatTranslationSession? _chat;
    private SubtitleIntegrationConfig? _activeConfig;
    private readonly DispatcherTimer _monitor = new() { Interval = TimeSpan.FromSeconds(2) };
    private CancellationTokenSource? _operation;
    private Task _pending = Task.CompletedTask;
    private bool _shutdown;
    private DateTimeOffset _started;
    public bool IsActive => _server is not null;
    private sealed record LanguageChoice(string Code, string Label)
    {
        public override string ToString() => Label;
    }
    public SubtitleIntegrationView() { InitializeComponent(); _monitor.Tick += Monitor_Tick; }
    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if (_installation is not null || Host is null) return;
        InitializeConfiguration(Host.GetSubtitleConfig(CaptionMode));
        await RunAsync(RefreshScenesAsync);
    }
    internal void InitializeConfiguration(SubtitleIntegrationConfig cfg)
    {
        _installation = new SayonariInstallation(CaptionMode);
        SectionTitle.Text = CaptionMode ? "Live Captions · Voz a subtítulos" : "Chat Translator · Traducción del chat";
        Description.Text = CaptionMode ? "Subtítulos de tu micrófono en OBS con jimakuChan. Se activa de forma independiente del traductor del chat."
            : "Traduce mensajes de Twitch con twitchTransFreeNext y muéstralos en OBS. Se activa de forma independiente del micrófono.";
        Requirements.Text = CaptionMode ? "Necesita Google Chrome. Al activar se abre una ventana para permitir e iniciar el micrófono. El reconocimiento envía audio al servicio de voz de Google; la traducción usa modelos locales de Chrome."
            : "Instala automáticamente un Python privado. Usa la conexión de Twitch de Neo Twitch. El texto se envía al servicio de traducción de Google y el proyecto conserva una caché local. No necesitas otro bot ni copiar tokens.";
        SourceLabel.Text = CaptionMode ? "Idioma hablado" : "Idioma principal del canal";
        TranslateBox.Visibility = CaptionMode ? Visibility.Visible : Visibility.Collapsed;
        ChatOptions.Visibility = CaptionMode ? Visibility.Collapsed : Visibility.Visible;
        SourceBox.ItemsSource = new[] { new LanguageChoice("es-CO", "Español (Colombia)"), new("es-ES", "Español (España)"), new("es-MX", "Español (México)"),
            new("en-US", "English"), new("pt-BR", "Português"), new("fr-FR", "Français"), new("de-DE", "Deutsch"), new("ru-RU", "Ruso (Русский)"), new("ja", "日本語"), new("ko", "한국어") };
        TargetBox.ItemsSource = new[] { new LanguageChoice("en", "English"), new("es", "Español"), new("pt", "Português"), new("fr", "Français"), new("de", "Deutsch"), new("ru", "Ruso (Русский)"), new("ja", "日本語"), new("ko", "한국어") };
        SourceBox.SelectedValue = cfg.SourceLanguage;
        TargetBox.SelectedValue = cfg.TargetLanguage;
        TranslateBox.IsChecked = cfg.Translate;
        OriginalBox.IsChecked = cfg.ShowOriginal;
        TopBox.IsChecked = cfg.PlaceAtTop;
        PublishBox.IsChecked = cfg.PublishToChat;
        FontBox.Text = cfg.FontSize.ToString(); ColorBox.Text = cfg.Color; DurationBox.Text = cfg.DurationSeconds.ToString(); IgnoreBox.Text = cfg.IgnoredUsers;
        StatusText.Text = _installation.IsInstalled ? "Instalada. Lista para configurar." : "No instalada.";
        UpdateButtons();
    }
    private void UpdateButtons()
    {
        var busy = _operation is not null || _shutdown;
        SetupPanel.IsEnabled = !busy && !IsActive;
        InstallButton.Content = _installation?.IsInstalled == true ? "Reparar instalación" : "Instalar";
        UninstallButton.IsEnabled = _installation?.IsInstalled == true;
        OptionsPanel.IsEnabled = !busy && !IsActive;
        StartButton.IsEnabled = !busy && !IsActive && _installation?.IsInstalled == true;
        StopButton.IsEnabled = !busy && IsActive;
        TestButton.IsEnabled = !busy && IsActive;
        CancelButton.Visibility = busy && !_shutdown ? Visibility.Visible : Visibility.Collapsed;
        Progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }
    private Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (_operation is not null || _shutdown) return Task.CompletedTask;
        return _pending = RunCoreAsync(action);
    }
    private async Task RunCoreAsync(Func<CancellationToken, Task> action)
    {
        using var cts = new CancellationTokenSource(); _operation = cts; UpdateButtons();
        try { await action(cts.Token); }
        catch (OperationCanceledException) { StatusText.Text = "Operación cancelada o tiempo de espera agotado."; }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { _operation = null; UpdateButtons(); }
    }
    private async void Install_Click(object sender, RoutedEventArgs e) => await RunAsync(token => _installation!.InstallAsync(new Progress<string>(s => StatusText.Text = s), token));
    private async void Uninstall_Click(object sender, RoutedEventArgs e) => await RunAsync(async token =>
    { await Task.Run(() => _installation!.Uninstall(), token); StatusText.Text = "Desinstalada. La otra integración mantiene su estado."; });
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(RefreshScenesAsync);
    private async Task RefreshScenesAsync(CancellationToken token)
    {
        if (Host is null) return;
        var selected = SceneBox.SelectedItem as string ?? Host.GetSubtitleConfig(CaptionMode).Scene;
        var scenes = await Host.GetIntegrationScenesAsync(token);
        SceneBox.ItemsSource = scenes; SceneBox.SelectedItem = scenes.Contains(selected) ? selected : scenes.FirstOrDefault();
    }
    private SubtitleIntegrationConfig ReadOptions()
    {
        if (SceneBox.SelectedItem is not string scene || SourceBox.SelectedValue is not string source || TargetBox.SelectedValue is not string target)
            throw new InvalidOperationException("Selecciona una escena de OBS y los idiomas.");
        if (!int.TryParse(FontBox.Text, out var font) || font is <16 or >100 || !int.TryParse(DurationBox.Text, out var duration) || duration is <2 or >60)
            throw new InvalidOperationException("Usa un tamaño entre 16 y 100 y una duración entre 2 y 60 segundos.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(ColorBox.Text, "^#[0-9a-fA-F]{6}$")) throw new InvalidOperationException("Escribe un color como #FFFFFF.");
        if (CaptionMode && TranslateBox.IsChecked != true && OriginalBox.IsChecked != true)
            throw new InvalidOperationException("Activa el texto original o la traducción para mostrar subtítulos.");
        if (!CaptionMode && source.Split('-')[0] == target)
            throw new InvalidOperationException("Elige un idioma secundario distinto del idioma principal del canal.");
        return new SubtitleIntegrationConfig { Scene = scene, SourceLanguage = source, TargetLanguage = target, Translate = !CaptionMode || TranslateBox.IsChecked == true,
            ShowOriginal = OriginalBox.IsChecked == true, PublishToChat = !CaptionMode && PublishBox.IsChecked == true,
            FontSize = font, Color = ColorBox.Text, DurationSeconds = duration, IgnoredUsers = IgnoreBox.Text, PlaceAtTop = TopBox.IsChecked == true };
    }
    private async void Start_Click(object sender, RoutedEventArgs e) => await RunAsync(async token =>
    {
        Host!.ValidateSubtitleConnections(CaptionMode);
        var cfg = ReadOptions();
        try
        {
            _activeConfig = cfg;
            _server = new SubtitleServer(cfg, CaptionMode ? _installation!.Project : null); _server.Start();
            if (CaptionMode)
            {
                _captions = new LiveCaptionSession(); _captions.Start(_installation!, _server.BaseUrl + "captions");
            }
            else
            {
                _chat = new ChatTranslationSession();
                var server = _server;
                var host = Host;
                await _chat.StartAsync(_installation!, cfg, async (user, original, translation) =>
                {
                    server.SetText($"{user}: {original}", $"{user}: {translation}");
                    if (cfg.PublishToChat && !_shutdown && _activeConfig == cfg)
                        await host!.PublishTranslationAsync(user, translation);
                }, token);
            }
            await Host.ShowSubtitleSourceAsync(CaptionMode, cfg, _server.OverlayUrl, token);
            Host.SaveSubtitleConfig(CaptionMode, cfg);
            _started = DateTimeOffset.UtcNow;
            _monitor.Start();
            StatusText.Text = CaptionMode ? "Abre la ventana de Chrome y pulsa Iniciar micrófono. Permite el micrófono cuando Chrome lo solicite." : "Activo. Esperando mensajes de Twitch…";
        }
        catch { await StopAsync(); throw; }
    });
    private async void Stop_Click(object sender, RoutedEventArgs e) => await RunAsync(async _ => { await StopAsync(); StatusText.Text = "Detenida."; });
    private async Task StopAsync()
    {
        _monitor.Stop();
        var cfg = _activeConfig; _activeConfig = null;
        Host?.DisableSubtitleIntegration(CaptionMode);
        try
        {
            _captions?.Dispose(); _captions = null;
            if (_chat is not null) { await _chat.DisposeAsync(); _chat = null; }
        }
        finally
        {
            if (_server is not null) { await _server.DisposeAsync(); _server = null; }
            if (cfg is not null && Host is not null) await Host.HideSubtitleSourceAsync(CaptionMode, cfg.Scene);
        }
    }
    public void ReceiveChat(TwitchEvent evt) { if (evt.Message is not null) _chat?.Enqueue(evt.UserName ?? "viewer", evt.Message); }
    private void Test_Click(object sender, RoutedEventArgs e) => _server?.SetText("Así se verán tus subtítulos", "This is how your subtitles will look");
    private async void Monitor_Tick(object? sender, EventArgs e)
    {
        if (_operation is not null || _server is null) return;
        if (!CaptionMode && _chat is { IsRunning: false })
        { var status = _chat.Status; await RunAsync(async _ => { await StopAsync(); StatusText.Text = status; }); return; }
        StatusText.Text = CaptionMode
            ? (_server.HasContact ? _server.Status : DateTimeOffset.UtcNow - _started > TimeSpan.FromSeconds(15)
                ? "Sin respuesta de Chrome. Revisa su ventana y permite el micrófono, o detén y vuelve a activar." : "Esperando Chrome y permiso del micrófono…")
            : _chat?.Status ?? "Detenida";
    }
    private void Project_Click(object sender, RoutedEventArgs e) => Host?.OpenSayonariProject(CaptionMode);
    private void Cancel_Click(object sender, RoutedEventArgs e) => _operation?.Cancel();
    public async Task ShutdownAsync() { _shutdown = true; _operation?.Cancel(); await _pending; await StopAsync(); }
}
