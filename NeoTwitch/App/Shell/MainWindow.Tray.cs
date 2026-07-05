using System.ComponentModel;
using System.Windows;
using NeoTwitch.Services.Shell;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using Forms = System.Windows.Forms;

namespace NeoTwitch;

public partial class MainWindow
{
    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Salir", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = AppIconLoader.Load();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = NeoTwitchProduct.DisplayName,
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async Task ExitApplicationAsync()
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        _isExiting = true;
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();
        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();
        _arduinoMonitorTimer.Stop();
        await _eventSubClient.StopAsync();
        _chatService.Dispose();
        _lightController.Dispose();
        DisposeTrayIcon();
        Close();
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            if (!ResolvePendingRuleChanges())
            {
                e.Cancel = true;
                return;
            }

            SaveGlobalSettingsFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();

            if (_config.CloseToTray)
            {
                e.Cancel = true;
                _twitchSubscriptionRefreshDebounce?.Cancel();
                Hide();
                ShowTrayBackgroundNotice();
                AddLog(_text.Get(UiTextKeys.TrayHiddenLog));
                return;
            }

            _isExiting = true;
        }

        await _eventSubClient.StopAsync();
        _arduinoMonitorTimer.Stop();
        _chatService.Dispose();
        _lightController.Dispose();
        DisposeTrayIcon();
    }

    private void ShowTrayBackgroundNotice()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        if (TrayNotificationService.TryShowBackgroundNotice(
            _notifyIcon,
            _text.Format(UiTextKeys.TrayBackgroundTitle, NeoTwitchProduct.DisplayName),
            _text.Get(UiTextKeys.TrayBackgroundText),
            _hasShownTrayNotice))
        {
            _hasShownTrayNotice = true;
        }
    }

    private void DisposeTrayIcon()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
