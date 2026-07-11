using System.IO;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class VirtualLightsOverlayService
{
    private const string HtmlFileName = "virtual-lights-overlay.html";
    private const string StateFileName = "virtual-lights-state.json";
    private const string ActiveHtmlPrefix = "virtual-lights-active-";
    private const int DefaultObsOpacity = 35;
    private readonly string _directory;
    private readonly TimeProvider _timeProvider;

    public VirtualLightsOverlayService(TimeProvider timeProvider, string? directory = null)
    {
        _timeProvider = timeProvider;
        _directory = string.IsNullOrWhiteSpace(directory)
            ? ApplicationPaths.VirtualLightsOverlayDirectory
            : directory;
    }

    public string BuildOverlayUrl()
    {
        EnsureFiles();
        return new Uri(GetHtmlPath()).AbsoluteUri;
    }

    public string BuildActiveOverlayUrl(VirtualLightCommand command, TimeSpan duration)
    {
        EnsureFiles();
        var json = BuildStateJson(command, duration);
        var activePath = GetActiveHtmlPath();
        File.WriteAllText(activePath, OverlayHtml.Replace("const embeddedState = null;", $"const embeddedState = {json};"));
        DeleteOldActiveHtmlFiles(activePath);
        return new Uri(activePath).AbsoluteUri;
    }

    public void WriteState(VirtualLightCommand command, TimeSpan duration)
    {
        EnsureFiles();
        File.WriteAllText(GetStatePath(), BuildStateJson(command, duration));
    }

    public void ClearState()
    {
        EnsureFiles();
        File.WriteAllText(GetStatePath(), "{\"visible\":false}");
    }

    private string GetHtmlPath()
    {
        return Path.Combine(_directory, HtmlFileName);
    }

    private string GetStatePath()
    {
        return Path.Combine(_directory, StateFileName);
    }

    private string GetActiveHtmlPath()
    {
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        return Path.Combine(_directory, $"{ActiveHtmlPrefix}{timestamp}.html");
    }

    private void DeleteOldActiveHtmlFiles(string keepPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(_directory, $"{ActiveHtmlPrefix}*.html"))
            {
                if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(keepPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Old temporary overlays are harmless if Windows keeps one locked briefly.
        }
    }

    private void EnsureFiles()
    {
        Directory.CreateDirectory(_directory);
        if (!File.Exists(GetHtmlPath()) || !string.Equals(File.ReadAllText(GetHtmlPath()), OverlayHtml, StringComparison.Ordinal))
        {
            File.WriteAllText(GetHtmlPath(), OverlayHtml);
        }

        if (!File.Exists(GetStatePath()))
        {
            File.WriteAllText(GetStatePath(), "{\"visible\":false}");
        }
    }

    private string BuildStateJson(VirtualLightCommand command, TimeSpan duration)
    {
        var obsOpacity = Math.Clamp(command.ObsOpacity, 0, 100);
        if (obsOpacity <= 0)
        {
            obsOpacity = DefaultObsOpacity;
        }

        var state = new
        {
            visible = true,
            pattern = command.Pattern.ToString(),
            primaryColor = command.PrimaryColor,
            secondaryColor = command.SecondaryColor,
            tertiaryColor = command.TertiaryColor,
            brightness = command.Brightness,
            cycleMs = command.CycleMs,
            stepMs = command.StepMs,
            opacity = obsOpacity,
            hideAt = _timeProvider.GetUtcNow().Add(duration).ToUnixTimeMilliseconds()
        };

        return JsonSerializer.Serialize(state);
    }

    private static string OverlayHtml => $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>Neo Twitch - Luces virtuales</title>
  <style>
    html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: rgba(0,0,0,0); }
    canvas { width: 100vw; height: 100vh; display: block; background: rgba(0,0,0,0); opacity: 0; transition: opacity 160ms ease; }
    canvas.visible { opacity: 1; }
  </style>
</head>
<body>
  <canvas id="lights"></canvas>
  <script>
    const canvas = document.getElementById('lights');
    const ctx = canvas.getContext('2d');
    let state = { visible: false };
    let lastState = '';
    let lastPoll = 0;
    let phase = 0;
    let stateFromUrl = false;
    const embeddedState = null;

    function resize() {
      const scale = window.devicePixelRatio || 1;
      canvas.width = Math.max(1, Math.floor(window.innerWidth * scale));
      canvas.height = Math.max(1, Math.floor(window.innerHeight * scale));
      ctx.setTransform(scale, 0, 0, scale, 0, 0);
    }

    function hexToRgb(hex) {
      const clean = String(hex || '#14B8A6').replace('#', '');
      const value = parseInt(clean.length === 3
        ? clean.split('').map(c => c + c).join('')
        : clean.padEnd(6, '0').slice(0, 6), 16);
      return { r: (value >> 16) & 255, g: (value >> 8) & 255, b: value & 255 };
    }

    function rgbToCss(color, alpha = 1) {
      return `rgba(${color.r}, ${color.g}, ${color.b}, ${alpha})`;
    }

    function blend(a, b, t) {
      return {
        r: Math.round(a.r + ((b.r - a.r) * t)),
        g: Math.round(a.g + ((b.g - a.g) * t)),
        b: Math.round(a.b + ((b.b - a.b) * t))
      };
    }

    function rainbow(t) {
      const h = ((t % 1) + 1) % 1 * 6;
      const x = 1 - Math.abs((h % 2) - 1);
      const [r, g, b] = h < 1 ? [1, x, 0]
        : h < 2 ? [x, 1, 0]
        : h < 3 ? [0, 1, x]
        : h < 4 ? [0, x, 1]
        : h < 5 ? [x, 0, 1]
        : [1, 0, x];
      return { r: Math.round(r * 255), g: Math.round(g * 255), b: Math.round(b * 255) };
    }

    function colorAt(pattern, index, count, primary, secondary, tertiary) {
      const t = (index / Math.max(1, count)) + phase;
      switch (pattern) {
        case 'Rainbow': return rainbow(t);
        case 'Pulse': return blend(primary, secondary, (Math.sin((phase * 8) + (index * 0.32)) + 1) / 2);
        case 'Chase': return ((index + Math.floor(phase * count * 2)) % 7) < 3 ? primary : blend(secondary, { r: 0, g: 0, b: 0 }, 0.78);
        case 'Theater': return (index + Math.floor(phase * count)) % 3 === 0 ? primary : ((index % 3) === 1 ? secondary : tertiary);
        case 'Sparkle': return Math.random() > 0.72 ? [primary, secondary, tertiary][Math.floor(Math.random() * 3)] : blend(primary, { r: 0, g: 0, b: 0 }, 0.82);
        case 'Rave': return [primary, secondary, tertiary, rainbow(Math.random())][Math.floor(Math.random() * 4)];
        default: return primary;
      }
    }

    function draw() {
      const w = window.innerWidth;
      const h = window.innerHeight;
      ctx.clearRect(0, 0, w, h);
      if (!state.visible || Date.now() > Number(state.hideAt || 0)) {
        canvas.className = '';
        requestAnimationFrame(draw);
        return;
      }

      canvas.className = 'visible';
      const primary = hexToRgb(state.primaryColor);
      const secondary = hexToRgb(state.secondaryColor);
      const tertiary = hexToRgb(state.tertiaryColor);
      const brightness = Math.max(0.04, Math.min(1, Number(state.brightness || 180) / 255));
      const opacity = Math.max(0, Math.min(1, Number(state.opacity ?? 35) / 100));
      const alpha = Math.max(0, Math.min(1, brightness * opacity));
      const base = colorAt(state.pattern, Math.floor(phase * 100), 100, primary, secondary, tertiary);
      const accent = colorAt(state.pattern, Math.floor((phase + 0.35) * 100), 100, primary, secondary, tertiary);

      ctx.fillStyle = rgbToCss(base, alpha);
      ctx.fillRect(0, 0, w, h);

      const glow = ctx.createRadialGradient(w * 0.5, h * 0.5, 0, w * 0.5, h * 0.5, Math.max(w, h) * 0.72);
      glow.addColorStop(0, rgbToCss(accent, Math.min(0.55, alpha * 1.4)));
      glow.addColorStop(0.72, rgbToCss(base, Math.min(0.32, alpha)));
      glow.addColorStop(1, rgbToCss(base, 0));
      ctx.fillStyle = glow;
      ctx.fillRect(0, 0, w, h);

      phase += Math.max(0.002, Math.min(0.06, 35 / Math.max(30, Number(state.stepMs || 120))));
      requestAnimationFrame(draw);
    }

    function pointAt(distance, w, h, inset) {
      const top = Math.max(1, w - inset * 2);
      const right = Math.max(1, h - inset * 2);
      const bottom = top;
      const d = ((distance % (top + right + bottom + right)) + (top + right + bottom + right)) % (top + right + bottom + right);
      if (d < top) return { x: inset + d, y: inset };
      if (d < top + right) return { x: w - inset, y: inset + (d - top) };
      if (d < top + right + bottom) return { x: w - inset - (d - top - right), y: h - inset };
      return { x: inset, y: h - inset - (d - top - right - bottom) };
    }

    function drawPerimeterSegment(start, end, w, h, inset) {
      const parts = 8;
      ctx.beginPath();
      for (let i = 0; i <= parts; i++) {
        const p = pointAt(start + ((end - start) * i / parts), w, h, inset);
        if (i === 0) ctx.moveTo(p.x, p.y); else ctx.lineTo(p.x, p.y);
      }
      ctx.stroke();
    }

    async function poll() {
      if (stateFromUrl) return;
      if (Date.now() - lastPoll < {{ApplicationLimits.ObsOverlayPollMs}}) return;
      lastPoll = Date.now();
      try {
        const res = await fetch(`virtual-lights-state.json?t=${Date.now()}`);
        const next = await res.text();
        if (next !== lastState) {
          lastState = next;
          state = JSON.parse(next);
        }
      } catch {
        state = { visible: false };
      }
    }

    function loadStateFromUrl() {
      const encoded = new URLSearchParams(window.location.search).get('state');
      if (!encoded) return;
      try {
        state = JSON.parse(atob(encoded));
        lastState = JSON.stringify(state);
        stateFromUrl = true;
      } catch {
        state = { visible: false };
      }
    }

    function loadEmbeddedState() {
      if (!embeddedState) return;
      state = embeddedState;
      lastState = JSON.stringify(state);
      stateFromUrl = true;
    }

    setInterval(poll, {{ApplicationLimits.ObsOverlayPollMs}});
    window.addEventListener('resize', resize);
    resize();
    loadEmbeddedState();
    if (!stateFromUrl) {
      loadStateFromUrl();
    }
    if (!stateFromUrl) {
      poll();
    }
    draw();
  </script>
</body>
</html>
""";
}
