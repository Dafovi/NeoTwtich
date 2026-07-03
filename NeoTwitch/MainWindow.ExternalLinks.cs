using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void OpenTwitchConsole()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.TwitchDeveloperApps);
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

        _externalLauncher.Open(NeoTwitchProduct.Links.TwitchChannel(channel));
        AddLog($"Twitch: abriendo perfil de {channel}.", ActivityLogKind.Twitch);
    }

    private void OpenAlexaConsole()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.AlexaDeveloperConsole);
        AddLog("Alexa Developer Console abierta.", ActivityLogKind.Alexa);
    }

    private void OpenArduinoSketch()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.ArduinoSketch);
        AddLog("Arduino: abriendo sketch NeoPixel.", ActivityLogKind.Arduino);
    }

    private void OpenArduinoGuide()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.ArduinoGuide);
        AddLog("Arduino: abriendo guia de conexion.", ActivityLogKind.Arduino);
    }
}
