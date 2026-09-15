using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed partial class ObsWebSocketService
{
    public async Task ShowIntegrationWindowAsync(string scene, string source, string window, double x, double width, double height,
        CancellationToken cancellationToken)
    {
        await ExecuteExclusiveAsync(cancellationToken, RequestTimeout, UiTextKeys.ObsShowMediaTimeout, async token =>
        {
            EnsureConnected();
            var settings = new Dictionary<string, object?>
            {
                ["window"] = window, ["priority"] = 0, ["method"] = 2,
                ["cursor"] = false, ["client_area"] = true
            };
            // Query first: an unrelated CreateInput failure must not overwrite another input.
            using var inputs = await SendRequestAsync("GetInputList", IntegrationRequest(new { inputKind = "window_capture" }), token);
            var exists = inputs.RootElement.GetProperty("d").GetProperty("responseData").GetProperty("inputs")
                .EnumerateArray().Any(input => input.GetProperty("inputName").GetString() == source);
            var alreadyInScene = false;
            if (exists)
            {
                try { _ = await GetSceneItemIdAsync(scene, source, token); alreadyInScene = true; }
                catch (InvalidOperationException) { }
            }
            if (!exists)
            {
                using var created = await SendRequestAsync("CreateInput", IntegrationRequest(new
                {
                    sceneName = scene, inputName = source, inputKind = "window_capture", inputSettings = settings, sceneItemEnabled = false
                }), token);
            }
            else
            {
                using var updated = await SendRequestAsync("SetInputSettings", IntegrationRequest(new { inputName = source, inputSettings = settings, overlay = true }), token);
                await EnsureSceneItemAsync(scene, source, token);
            }
            if (!alreadyInScene)
            {
                var id = await GetSceneItemIdAsync(scene, source, token);
                using var transformed = await SendRequestAsync("SetSceneItemTransform", IntegrationRequest(new
                {
                    sceneName = scene, sceneItemId = id,
                    sceneItemTransform = new { positionX = x, positionY = 0, boundsType = "OBS_BOUNDS_SCALE_INNER", boundsWidth = width, boundsHeight = height }
                }), token);
            }
            await SetSceneItemEnabledAsync(scene, source, true, token);
            return Snapshot();
        });
    }

    private static Dictionary<string, object?> IntegrationRequest(object value) =>
        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(value))!;
}
