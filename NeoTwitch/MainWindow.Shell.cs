using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Activity;
using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateConnectionButtons()
    {
        var twitchBusy = _isTwitchAuthorizing || _isTwitchConnecting;
        TwitchButton.IsEnabled = !twitchBusy;
        TwitchButton.Content = _isTwitchAuthorizing
            ? "Autorizando..."
            : _isTwitchConnecting
                ? "Conectando..."
                : _eventSubClient.IsRunning
                    ? "Desconectar Twitch"
                    : "Conectar Twitch";

        ConnectArduinoButton.IsEnabled = !_isArduinoConnecting && _config.ArduinoEnabled;
        ConnectArduinoButton.Content = _isArduinoConnecting
            ? "Conectando..."
            : "Conectar Arduino";

        TestAlexaButton.IsEnabled = !_isAlexaConnecting && _config.Alexa.Enabled;
        TestAlexaButton.Content = _isAlexaConnecting
            ? "Probando..."
            : "Probar Alexa";

        ConnectObsButton.IsEnabled = !_isObsConnecting && _config.Obs.Enabled;
        ConnectObsButton.Content = _isObsConnecting
            ? "Conectando..."
            : _obsService.IsConnected
                ? "Desconectar OBS"
                : "Conectar OBS";
        ConnectObsButtonPanel.IsEnabled = ConnectObsButton.IsEnabled;
        ConnectObsButtonPanel.Content = ConnectObsButton.Content;

        TestObsButton.IsEnabled = !_isObsConnecting && _config.Obs.Enabled;
        TestObsButton.Content = _isObsConnecting
            ? "Actualizando..."
            : "Actualizar escenas";
        TestObsButtonPanel.IsEnabled = TestObsButton.IsEnabled;
        TestObsButtonPanel.Content = TestObsButton.Content;
    }

    private string BuildAlexaSidebarStatusText()
    {
        var background = _config.BackgroundAlexaEnabled
            ? $"Fondo: {_config.BackgroundAlexaOnEventName}"
            : "Fondo sin mantener";

        var endBehavior = _config.BackgroundAlexaTurnOffAfterEvent
            ? $"Al finalizar: {_config.BackgroundAlexaOffEventName}"
            : "Al finalizar: conserva estado";

        return $"{background}. {endBehavior}.";
    }

    private void UpdateTwitchLiveIndicator()
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        if (_streamStatus is { IsLive: true })
        {
            var liveBrush = FrozenBrushFrom("#FF2D55");
            TwitchLiveDot.Fill = liveBrush;
            TwitchLiveDot.Stroke = liveBrush;
            TwitchLiveStateText.Text = "En directo";
            TwitchLiveStateText.Foreground = liveBrush;
            TopProfileText.Text = "Perfil";
            TopProfileText.Foreground = palette.Text;
            return;
        }

        TwitchLiveDot.Fill = System.Windows.Media.Brushes.Transparent;
        TwitchLiveDot.Stroke = palette.SidebarText;
        TwitchLiveStateText.Text = "No esta en directo";
        TwitchLiveStateText.Foreground = palette.SidebarText;
        TopProfileText.Text = "Perfil";
        TopProfileText.Foreground = palette.Text;
    }

    private void UpdateChannelAvatar()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_config.Channel.ProfileImageUrl))
            {
                ChannelAvatarImage.Source = new BitmapImage(new Uri(_config.Channel.ProfileImageUrl, UriKind.Absolute));
                return;
            }
        }
        catch
        {
            // Use the bundled app icon when Twitch has no image available.
        }

        ChannelAvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon.png", UriKind.Absolute));
    }

    private void ApplyWindowChromeColor()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var captionColor = AppCaptionColor;
            var borderColor = AppCaptionColor;
            var textColor = AppCaptionTextColor;
            var size = Marshal.SizeOf<int>();
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeCaptionColor, ref captionColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeBorderColor, ref borderColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeTextColor, ref textColor, size);
        }
        catch
        {
            // Older Windows builds ignore custom title bar colors.
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string FormatNameList(IReadOnlyList<string> names)
    {
        var visibleNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(5)
            .ToArray();

        var text = visibleNames.Length == 0
            ? "sin nombre"
            : string.Join(", ", visibleNames);
        var remaining = names.Count - visibleNames.Length;
        return remaining > 0 ? $"{text} y {remaining} mas" : text;
    }

    private static string NormalizeThemeMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };
    }

    private static bool ResolveDarkMode(string? themeMode)
    {
        return NormalizeThemeMode(themeMode) switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsWindowsAppsDarkMode()
        };
    }

    private static bool IsWindowsAppsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeEventName(string text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }

    private void UpdateSliderLabels()
    {
        BrightnessValueText.Text = ((int)Math.Round(BrightnessSlider.Value)).ToString();
        DurationValueText.Text = $"{(int)Math.Round(DurationSlider.Value)} ms";
        CycleValueText.Text = $"{(int)Math.Round(CycleSlider.Value)} ms";
        StepValueText.Text = $"{(int)Math.Round(StepSlider.Value)} ms";
        BackgroundBrightnessValueText.Text = ((int)Math.Round(BackgroundBrightnessSlider.Value)).ToString();
        BackgroundCycleValueText.Text = $"{(int)Math.Round(BackgroundCycleSlider.Value)} ms";
        BackgroundStepValueText.Text = $"{(int)Math.Round(BackgroundStepSlider.Value)} ms";
        AlertVolumeValueText.Text = $"{(int)Math.Round(AlertVolumeSlider.Value)}%";
    }

    private void UpdateColorButtons()
    {
        PrimaryColorButton.Background = ToBrush(PrimaryColorBox.Text);
        SecondaryColorButton.Background = ToBrush(SecondaryColorBox.Text);
        TertiaryColorButton.Background = ToBrush(TertiaryColorBox.Text);
        BackgroundPrimaryColorButton.Background = ToBrush(BackgroundPrimaryColorBox.Text);
        BackgroundSecondaryColorButton.Background = ToBrush(BackgroundSecondaryColorBox.Text);
        BackgroundTertiaryColorButton.Background = ToBrush(BackgroundTertiaryColorBox.Text);
    }

    private void UpdateSensitiveFieldVisibility()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateSensitiveField(ClientIdBox, ClientIdMaskText, ClientIdRevealButton, _showClientId);
        UpdateSensitiveField(ClientSecretBox, ClientSecretMaskText, ClientSecretRevealButton, _showClientSecret);
        UpdateSensitiveField(AlexaRelayUrlBox, AlexaRelayUrlMaskText, AlexaRelayUrlRevealButton, _showAlexaRelayUrl);
        UpdateSensitiveField(AlexaAuthTokenBox, AlexaAuthTokenMaskText, AlexaAuthTokenRevealButton, _showAlexaAuthToken);
        UpdateSensitiveField(ObsPasswordBox, ObsPasswordMaskText, ObsPasswordRevealButton, _showObsPassword);
    }

    private static void UpdateSensitiveField(
        System.Windows.Controls.TextBox textBox,
        TextBlock maskText,
        System.Windows.Controls.Button revealButton,
        bool isVisible)
    {
        var shouldMask = !isVisible && !string.IsNullOrWhiteSpace(textBox.Text);
        textBox.IsHitTestVisible = !shouldMask;
        maskText.Visibility = shouldMask ? Visibility.Visible : Visibility.Collapsed;
        maskText.Text = shouldMask ? BuildMask(textBox.Text) : "";
        revealButton.Content = isVisible ? "Ocultar" : "Ver";
    }

    private static string BuildMask(string value)
    {
        var length = Math.Clamp(value.Trim().Length, 8, 20);
        return new string('*', length);
    }

    private static SolidColorBrush ToBrush(string color)
    {
        try
        {
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color)));
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    private void PickColor(System.Windows.Controls.TextBox target)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        target.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Salir", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = LoadAppIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Neo Twitch",
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
            var resource = System.Windows.Application.GetResourceStream(new Uri("Assets/AppIcon.ico", UriKind.Relative));
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
        _isExiting = true;
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();
        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();
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
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
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
            _notifyIcon.BalloonTipTitle = "Neo Twitch sigue activo";
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

    private void Window_StateChanged(object? sender, EventArgs e)
    {
    }

    private void CustomTitleDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            ToggleWindowState();
            return;
        }

        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
        }

        try
        {
            DragMove();
        }
        catch
        {
            // DragMove can throw if the pointer is released before the drag starts.
        }
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
            return;
        }

        MaximizeWindowToWorkArea();
    }

    private void MaximizeWindowToWorkArea()
    {
        _restoreWindowBounds = new Rect(Left, Top, Width, Height);

        var handle = new WindowInteropHelper(this).Handle;
        var area = Forms.Screen.FromHandle(handle).WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new System.Windows.Point(area.Left, area.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(area.Right, area.Bottom));

        WindowState = WindowState.Normal;
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(MinWidth, bottomRight.X - topLeft.X);
        Height = Math.Max(MinHeight, bottomRight.Y - topLeft.Y);
        _isCustomMaximized = true;
    }

    private void RestoreWindowFromWorkArea()
    {
        WindowState = WindowState.Normal;
        _isCustomMaximized = false;

        if (_restoreWindowBounds.IsEmpty)
        {
            return;
        }

        Left = _restoreWindowBounds.Left;
        Top = _restoreWindowBounds.Top;
        Width = _restoreWindowBounds.Width;
        Height = _restoreWindowBounds.Height;
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.Button)
            {
                return true;
            }
        }

        return false;
    }
}