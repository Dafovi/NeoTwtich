using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using WpfClipboard = System.Windows.Clipboard;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void ToggleTwitchConnection()
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
                AddLog(_text.Get(UiTextKeys.TwitchDisconnectedLog));
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
            _dialog.ShowWarning(_text.Get(UiTextKeys.TwitchTitle), ex.Message);
        }
    }

    private async Task SignInToTwitchAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchMissingClientId));
        }

        _isTwitchAuthorizing = true;
        UpdateStatusText();

        try
        {
            var session = await _authService.BeginDeviceFlowAsync(_config.TwitchClientId, CancellationToken.None);
            WpfClipboard.SetText(session.UserCode);
            _authService.OpenVerificationPage(session);
            _dialog.ShowInformation(
                _text.Get(UiTextKeys.TwitchLoginTitle),
                _text.Format(UiTextKeys.TwitchAuthorizePrompt, session.UserCode));

            _config.Token = await _authService.PollForTokenAsync(_config.TwitchClientId, session, AddLog, CancellationToken.None);
            _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
            SaveConfig();
            AddLog(_text.Format(UiTextKeys.TwitchAuthorizedLog, _config.Channel.DisplayName));
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
                throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchMissingScopes, string.Join(", ", missingScopes)));
            }

            try
            {
                await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            }
            catch (Exception ex) when (allowInteractiveReauth && TwitchConnectionRecoveryService.IsRecoverableRefreshError(ex))
            {
                AddLog(_text.Get(UiTextKeys.TwitchReauthRequiredLog), ActivityLogKind.Twitch);
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
            AddLog(_text.Get(UiTextKeys.TwitchListeningLog));
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

}
