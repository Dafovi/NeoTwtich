using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void TwitchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isTwitchAuthorizing || _isTwitchConnecting)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();

            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
                _twitchConnectionError = "";
                AddLog("Twitch desconectado.");
                UpdateStatusText();
                return;
            }

            if (!_config.Token.HasToken || TwitchAuthService.GetMissingScopes(_config.Token).Count > 0)
            {
                await SignInToTwitchAsync();
            }

            await StartTwitchAsync(allowInteractiveReauth: true);
        }
        catch (Exception ex)
        {
            _twitchConnectionError = ex.Message;
            UpdateStatusText();
            AddLog($"Twitch: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Twitch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SignInToTwitchAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            throw new InvalidOperationException("Escribe primero el Client ID de Twitch.");
        }

        _isTwitchAuthorizing = true;
        UpdateStatusText();

        try
        {
            var session = await _authService.BeginDeviceFlowAsync(_config.TwitchClientId, CancellationToken.None);
            WpfClipboard.SetText(session.UserCode);
            _authService.OpenVerificationPage(session);
            WpfMessageBox.Show(
                this,
                $"Autoriza la app en Twitch con el codigo {session.UserCode}. El codigo ya quedo copiado al portapapeles.",
                "Login Twitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _config.Token = await _authService.PollForTokenAsync(_config.TwitchClientId, session, AddLog, CancellationToken.None);
            _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
            SaveConfig();
            AddLog($"Twitch autorizado como {_config.Channel.DisplayName}.");
        }
        finally
        {
            _isTwitchAuthorizing = false;
            UpdateStatusText();
        }
    }

    private async Task StartTwitchAsync(bool allowInteractiveReauth = false)
    {
        _isTwitchConnecting = true;
        _twitchConnectionError = "";
        UpdateStatusText();

        try
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(_config.Token);
            if (missingScopes.Count > 0)
            {
                throw new InvalidOperationException($"Twitch necesita autorizar permisos nuevos: {string.Join(", ", missingScopes)}. Presiona Conectar Twitch para iniciar sesion otra vez.");
            }

            try
            {
                await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            }
            catch (Exception ex) when (allowInteractiveReauth && IsRecoverableTwitchRefreshError(ex))
            {
                AddLog("Twitch necesita autorizar de nuevo porque el token guardado no se pudo refrescar.", ActivityLogKind.Twitch);
                _config.Token = new TwitchTokenInfo();
                _config.Channel = new TwitchChannelInfo();
                SaveConfig();
                await SignInToTwitchAsync();
            }

            if (!_config.Channel.IsReady)
            {
                _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
                SaveConfig();
            }

            await _eventSubClient.StartAsync();
            _eventSubscriptionSignature = BuildEventSubscriptionSignature();
            await RefreshTwitchStreamStatusAsync();
            AddLog("Twitch escuchando eventos.");
        }
        catch (Exception ex)
        {
            _twitchConnectionError = ex.Message;
            throw;
        }
        finally
        {
            _isTwitchConnecting = false;
            UpdateStatusText();
        }
    }

    private string BuildEventSubscriptionSignature()
    {
        var activeKinds = _config.Rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => rule.EventKind)
            .Where(kind => kind != TwitchEventKind.Test)
            .Distinct()
            .OrderBy(kind => kind)
            .Select(kind => kind.ToString());

        return string.Join("|", activeKinds);
    }

    private void ScheduleTwitchSubscriptionRefreshIfNeeded()
    {
        if (_initializingComponent || _loadingRule || !_eventSubClient.IsRunning)
        {
            return;
        }

        var signature = BuildEventSubscriptionSignature();
        if (string.Equals(signature, _eventSubscriptionSignature, StringComparison.Ordinal))
        {
            return;
        }

        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();

        var cts = new CancellationTokenSource();
        _twitchSubscriptionRefreshDebounce = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(900, cts.Token);
                var operation = Dispatcher.InvokeAsync(() => RefreshTwitchSubscriptionsAsync(signature));
                await await operation.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, "No se pudieron refrescar las suscripciones de Twitch.");
                AddLog($"Twitch: {ex.Message}", ActivityLogKind.Important);
                _ = Dispatcher.InvokeAsync(() =>
                {
                    _twitchConnectionError = ex.Message;
                    UpdateStatusText();
                });
            }
        });
    }

    private async Task RefreshTwitchSubscriptionsAsync(string signature)
    {
        if (!_eventSubClient.IsRunning)
        {
            _eventSubscriptionSignature = signature;
            return;
        }

        AddLog("Twitch: actualizando suscripciones por cambios en reglas.", ActivityLogKind.Twitch);
        await _eventSubClient.StopAsync();
        await _eventSubClient.StartAsync();
        _eventSubscriptionSignature = signature;
        _twitchConnectionError = "";
        AddLog("Twitch: suscripciones actualizadas.", ActivityLogKind.Twitch);
        UpdateStatusText();
    }

    private static bool IsRecoverableTwitchRefreshError(Exception exception)
    {
        var message = exception.Message;
        return message.Contains("No pude refrescar Twitch", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("missing client secret", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid client", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid refresh token", StringComparison.OrdinalIgnoreCase));
    }

    private async Task RefreshTwitchStreamStatusAsync()
    {
        if (!_config.Token.HasToken || !_config.Channel.IsReady)
        {
            _streamStatus = null;
            UpdateStatusText();
            return;
        }

        try
        {
            await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            _streamStatus = await _authService.GetStreamStatusAsync(_config, CancellationToken.None);
            _twitchConnectionError = "";
            SaveConfig();
        }
        catch (Exception ex)
        {
            _streamStatus = null;
            _twitchConnectionError = ex.Message;
            AddLog($"Twitch estado: {ex.Message}");
        }

        UpdateStatusText();
    }
}
