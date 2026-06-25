using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using NeoTwitch.Shared;
using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace NeoTwitch;

public partial class MainWindow
{
    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Salir", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = LoadAppIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = NeoTwitchProduct.DisplayName,
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private static DrawingIcon LoadAppIcon()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            {
                var icon = DrawingIcon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch
        {
            // Try the bundled WPF resource below.
        }

        try
        {
            var resource = System.Windows.Application.GetResourceStream(new Uri(NeoTwitchProduct.AppIconIcoResource, UriKind.Relative));
            if (resource?.Stream is not null)
            {
                using var stream = resource.Stream;
                using var icon = new DrawingIcon(stream);
                return (DrawingIcon)icon.Clone();
            }
        }
        catch
        {
            // Fall back to a generic app icon only if the bundled icon cannot be loaded.
        }

        return (DrawingIcon)SystemIcons.Application.Clone();
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
                AddLog("Ventana oculta en segundo plano.");
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

        try
        {
            _notifyIcon.BalloonTipTitle = $"{NeoTwitchProduct.DisplayName} sigue activo";
            _notifyIcon.BalloonTipText = "La app quedo en segundo plano. Abrela desde el icono de la bandeja cuando la necesites.";
            _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(_hasShownTrayNotice ? 2500 : 4000);
            _hasShownTrayNotice = true;
        }
        catch
        {
            // Windows can suppress tray notifications; the app still remains available in the tray.
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
