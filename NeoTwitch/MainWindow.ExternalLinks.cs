using System.Diagnostics;
using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void OpenTwitchConsole()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.TwitchDeveloperApps,
            UseShellExecute = true
        });
        AddLog("Twitch Console abierta para revisar el Client ID.", ActivityLogKind.Twitch);
    }

    private void OpenTwitchProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var channel = FirstNonEmpty(_config.Channel.Login, _config.Channel.DisplayName)
            .Trim()
            .TrimStart('@');

        if (string.IsNullOrWhiteSpace(channel))
        {
            WpfMessageBox.Show(
                this,
                "Conecta Twitch primero para abrir el perfil del canal.",
                "Twitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.TwitchChannel(channel),
            UseShellExecute = true
        });
        AddLog($"Twitch: abriendo perfil de {channel}.", ActivityLogKind.Twitch);
    }

    private void OpenAlexaConsole()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.AlexaDeveloperConsole,
            UseShellExecute = true
        });
        AddLog("Alexa Developer Console abierta.", ActivityLogKind.Alexa);
    }

    internal void OpenArduinoSketchButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoSketch,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo sketch NeoPixel.", ActivityLogKind.Arduino);
    }

    internal void OpenArduinoGuideButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoGuide,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo guia de conexion.", ActivityLogKind.Arduino);
    }
}
