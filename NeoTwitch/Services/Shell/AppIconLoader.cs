using System.Drawing;
using System.IO;
using NeoTwitch.Shared;
using DrawingIcon = System.Drawing.Icon;

namespace NeoTwitch.Services.Shell;

public static class AppIconLoader
{
    public static DrawingIcon Load()
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
}
