using NeoTwitch.Models;
using NeoTwitch.ViewModels.Ui;
using FormsScreen = System.Windows.Forms.Screen;

namespace NeoTwitch.Services;

public sealed class VirtualScreenService
{
    public IReadOnlyList<UiOption<string>> CreateScreenChoices()
    {
        var screens = FormsScreen.AllScreens;
        if (screens.Length == 0)
        {
            return [new UiOption<string>("Pantalla principal", "")];
        }

        var options = new List<UiOption<string>>
        {
            new(BuildScreenLabel(FormsScreen.PrimaryScreen ?? screens[0], 1, "Pantalla principal"), "")
        };

        for (var i = 0; i < screens.Length; i++)
        {
            options.Add(new UiOption<string>(BuildScreenLabel(screens[i], i + 1), screens[i].DeviceName));
        }

        return options;
    }

    public VirtualScreenInfo ResolveScreen(string? screenId)
    {
        var screens = FormsScreen.AllScreens;
        var selected = screens.FirstOrDefault(screen =>
            !string.IsNullOrWhiteSpace(screenId)
            && string.Equals(screen.DeviceName, screenId, StringComparison.OrdinalIgnoreCase));

        selected ??= FormsScreen.PrimaryScreen ?? screens.FirstOrDefault();
        if (selected is null)
        {
            var fallbackWidth = Math.Max(640d, System.Windows.SystemParameters.PrimaryScreenWidth);
            var fallbackHeight = Math.Max(480d, System.Windows.SystemParameters.PrimaryScreenHeight);
            return new VirtualScreenInfo("", "Pantalla principal", 0, 0, fallbackWidth, fallbackHeight, true);
        }

        var index = Math.Max(0, Array.IndexOf(screens, selected)) + 1;
        var bounds = selected.Bounds;
        return new VirtualScreenInfo(
            selected.DeviceName,
            BuildScreenLabel(selected, index),
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            selected.Primary);
    }

    private static string BuildScreenLabel(FormsScreen screen, int index, string? prefix = null)
    {
        var title = prefix ?? $"Pantalla {index}";
        var primary = screen.Primary ? " (Principal)" : "";
        return $"{title}{primary} - {screen.Bounds.Width}x{screen.Bounds.Height}";
    }
}
