using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task StopMediaPreviewAsync()
    {
        var cts = _mediaPreviewCts;
        var request = _mediaPreviewHideRequest;
        var kind = _previewingMediaKind;

        _mediaPreviewCts = null;
        _mediaPreviewHideRequest = null;
        _previewingMediaKind = null;
        _previewingMediaId = "";

        try
        {
            cts?.Cancel();
            if (request is not null && _obsService.IsConnected)
            {
                await HideRuleObsMediaAsync(request, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo detener la prueba de medio OBS.");
            AddLog($"OBS prueba: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            cts?.Dispose();
            if (kind is MediaLibraryKind mediaKind)
            {
                RefreshMediaLibraryView(mediaKind);
            }
        }
    }
}
