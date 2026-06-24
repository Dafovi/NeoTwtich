using System.IO;
using Microsoft.Win32;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (runKey is null)
        {
            throw new InvalidOperationException("No pude abrir la clave de inicio de Windows.");
        }

        if (enabled)
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                throw new InvalidOperationException("No pude detectar la ruta del ejecutable actual.");
            }

            runKey.SetValue(NeoTwitchProduct.StartupValueName, $"\"{executablePath}\"");
            return;
        }

        runKey.DeleteValue(NeoTwitchProduct.StartupValueName, throwOnMissingValue: false);
    }
}
