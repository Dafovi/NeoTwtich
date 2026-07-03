using System.Diagnostics;
using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
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
            _dialog.ShowInformation("Twitch", "Conecta Twitch primero para abrir el perfil del canal.");
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

    private void OpenArduinoSketch()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoSketch,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo sketch NeoPixel.", ActivityLogKind.Arduino);
    }

    private void OpenArduinoGuide()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoGuide,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo guia de conexion.", ActivityLogKind.Arduino);
    }
}
