using System.IO;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class ObsOverlayService
{
    private const string HtmlFileName = "obs-overlay.html";
    private const string StateFileName = "obs-overlay-state.json";
    private readonly string _directory;
    private readonly TimeProvider _timeProvider;

    public ObsOverlayService(TimeProvider timeProvider, string? directory = null)
    {
        _timeProvider = timeProvider;
        _directory = string.IsNullOrWhiteSpace(directory)
            ? ApplicationPaths.ObsOverlayDirectory
            : directory;
    }

    public string BuildOverlayUrl()
    {
        EnsureFiles();
        return new Uri(GetHtmlPath()).AbsoluteUri;
    }

    public void WriteState(MediaAssetConfig asset, ObsMediaKind kind, ObsIntegrationConfig config, TimeSpan duration)
    {
        EnsureFiles();
        var mediaWidth = Math.Clamp(
            config.OverlayMediaWidth,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayWidth));
        var mediaHeight = Math.Clamp(
            config.OverlayMediaHeight,
            ApplicationLimits.MinObsOverlayMediaSize,
            Math.Max(ApplicationLimits.MinObsOverlayMediaSize, config.OverlayHeight));
        var (x, y) = ResolvePosition(config, mediaWidth, mediaHeight);
        var state = new
        {
            visible = true,
            kind = kind == ObsMediaKind.Image ? "image" : "video",
            fileUri = new Uri(asset.FilePath).AbsoluteUri,
            displayName = asset.DisplayName,
            width = mediaWidth,
            height = mediaHeight,
            x,
            y,
            hideAt = _timeProvider.GetUtcNow().Add(duration).ToUnixTimeMilliseconds()
        };

        File.WriteAllText(GetStatePath(), JsonSerializer.Serialize(state));
    }

    public void ClearState()
    {
        EnsureFiles();
        File.WriteAllText(GetStatePath(), "{\"visible\":false}");
    }

    private string GetDirectory()
    {
        return _directory;
    }

    private string GetHtmlPath()
    {
        return Path.Combine(GetDirectory(), HtmlFileName);
    }

    private string GetStatePath()
    {
        return Path.Combine(GetDirectory(), StateFileName);
    }

    private void EnsureFiles()
    {
        var directory = GetDirectory();
        Directory.CreateDirectory(directory);
        var htmlPath = GetHtmlPath();
        if (!File.Exists(htmlPath))
        {
            File.WriteAllText(htmlPath, OverlayHtml);
        }

        var statePath = GetStatePath();
        if (!File.Exists(statePath))
        {
            File.WriteAllText(statePath, "{}");
        }
    }

    private static (int X, int Y) ResolvePosition(ObsIntegrationConfig config, int mediaWidth, int mediaHeight)
    {
        var maxX = Math.Max(0, config.OverlayWidth - mediaWidth);
        var maxY = Math.Max(0, config.OverlayHeight - mediaHeight);
        return config.OverlayPositionMode switch
        {
            "Custom" => (Math.Clamp(config.OverlayX, 0, maxX), Math.Clamp(config.OverlayY, 0, maxY)),
            "Random" => (Random.Shared.Next(0, maxX + 1), Random.Shared.Next(0, maxY + 1)),
            _ => (maxX / 2, maxY / 2)
        };
    }

    private static string OverlayHtml => $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>{{NeoTwitchProduct.Obs.OverlayWindowTitle}}</title>
  <style>
    html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: transparent; }
    #media { position: absolute; object-fit: contain; opacity: 0; transition: opacity 180ms ease; }
    #media.visible { opacity: 1; }
  </style>
</head>
<body>
  <img id="image" alt="">
  <video id="video" playsinline></video>
  <script>
    const image = document.getElementById('image');
    const video = document.getElementById('video');
    let lastKey = '';
    function applyLayout(element, state) {
      element.id = 'media';
      element.style.left = `${state.x || 0}px`;
      element.style.top = `${state.y || 0}px`;
      element.style.width = `${state.width || 720}px`;
      element.style.height = `${state.height || 420}px`;
    }
    function hideAll() {
      image.className = '';
      video.className = '';
      video.pause();
    }
    async function tick() {
      try {
        const res = await fetch(`obs-overlay-state.json?t=${Date.now()}`);
        const state = await res.json();
        if (!state.visible || Date.now() > Number(state.hideAt || 0)) {
          hideAll();
          return;
        }
        const key = `${state.kind}|${state.fileUri}|${state.hideAt}`;
        if (key === lastKey) return;
        lastKey = key;
        hideAll();
        if (state.kind === 'video') {
          applyLayout(video, state);
          video.src = state.fileUri;
          video.currentTime = 0;
          video.className = 'visible';
          await video.play().catch(() => {});
        } else {
          applyLayout(image, state);
          image.src = state.fileUri;
          image.className = 'visible';
        }
      } catch {
        hideAll();
      }
    }
    setInterval(tick, {{ApplicationLimits.ObsOverlayPollMs}});
    tick();
  </script>
</body>
</html>
""";
}
