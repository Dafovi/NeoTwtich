using System.IO;
using Microsoft.Win32;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly IUiTextService _text;

    public WindowsStartupService()
        : this(UiTextService.CreateDefault())
    {
    }

    public WindowsStartupService(IUiTextService text)
    {
        _text = text;
    }

    public void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (runKey is null)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.WindowsStartupOpenRunKeyFailure));
        }

        if (enabled)
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                throw new InvalidOperationException(_text.Get(UiTextKeys.WindowsStartupExecutablePathFailure));
            }

            runKey.SetValue(NeoTwitchProduct.StartupValueName, $"\"{executablePath}\"");
            return;
        }

        runKey.DeleteValue(NeoTwitchProduct.StartupValueName, throwOnMissingValue: false);
    }
}
