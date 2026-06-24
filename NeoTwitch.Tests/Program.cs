using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Configuration;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Status;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Status;

var tests = new (string Name, Action Body)[]
{
    ("ConfigurationItemFactory creates inactive action defaults", ConfigurationFactoryTests.CreateRuleUsesSafeDefaults),
    ("ConfigurationItemFactory suggests first available pin", ConfigurationFactoryTests.CreateLedStripSuggestsFirstAvailablePin),
    ("AppConfig default rules keep expected starter alerts", AppConfigTests.DefaultRulesKeepStarterAlerts),
    ("AppConfig default services keep optional integrations disabled", AppConfigTests.DefaultServicesKeepOptionalIntegrationsDisabled),
    ("AppConfigNormalizer trims and clamps loaded settings", AppConfigNormalizerTests.TrimsAndClampsLoadedSettings),
    ("AppConfigNormalizer migrates legacy rule audio paths", AppConfigNormalizerTests.MigratesLegacyRuleAudioPaths),
    ("EventRuleFilterService filters status and category", EventRuleFilterTests.FiltersStatusAndCategory),
    ("EventRuleFilterService searches editable text", EventRuleFilterTests.SearchesEditableText),
    ("EventRuleSnapshotService clones editable values independently", EventRuleSnapshotTests.CloneCopiesEditableValues),
    ("EventRuleSnapshotService detects editable changes", EventRuleSnapshotTests.DetectsEditableChanges),
    ("EventRuleMatcherService resolves normal event matches", EventRuleMatcherTests.ResolvesNormalEventMatches),
    ("EventRuleMatcherService keeps highest bits threshold", EventRuleMatcherTests.KeepsHighestBitsThreshold),
    ("AlertDurationService resolves maximum positive duration", AlertDurationTests.ResolvesMaximumPositiveDuration),
    ("AlertDurationService clamps synchronized durations", AlertDurationTests.ClampsSynchronizedDurations),
    ("LedPreviewService calculates responsive dot counts", LedPreviewTests.CalculatesResponsiveDotCounts),
    ("LedPreviewService builds solid frames with brightness floor", LedPreviewTests.BuildsSolidFramesWithBrightnessFloor),
    ("LedPreviewService builds rainbow frames", LedPreviewTests.BuildsRainbowFrames),
    ("AudioRuleAssetService resolves single assets", AudioRuleAssetTests.ResolvesSingleAssets),
    ("AudioRuleAssetService resolves group assets with existing files", AudioRuleAssetTests.ResolvesGroupAssetsWithExistingFiles),
    ("AudioRuleAssetService detects rule asset usage", AudioRuleAssetTests.DetectsRuleAssetUsage),
    ("LibraryRowFilterService filters audio rows", LibraryRowFilterTests.FiltersAudioRows),
    ("LibraryRowFilterService filters media rows", LibraryRowFilterTests.FiltersMediaRows),
    ("MediaRuleAssetService resolves single media assets", MediaRuleAssetTests.ResolvesSingleMediaAssets),
    ("MediaRuleAssetService resolves group media assets", MediaRuleAssetTests.ResolvesGroupMediaAssets),
    ("MediaRuleAssetService resolves image and video durations", MediaRuleAssetTests.ResolvesImageAndVideoDurations),
    ("ConnectionStateService resolves service states", ConnectionStateTests.ResolvesServiceStates),
    ("ConnectionStateService maps visual metadata", ConnectionStateTests.MapsVisualMetadata),
    ("RuleSimulationService normalizes chat command matching", RuleSimulationTests.MatchesChatCommandWithNormalization),
    ("RuleSimulationService builds representative test events", RuleSimulationTests.BuildsRepresentativeEvents)
};

var failures = 0;

foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FAIL {test.Name}");
        Console.ResetColor();
        Console.WriteLine(ex.Message);
    }
    finally
    {
        Console.ResetColor();
    }
}

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} test(s) fallaron.");
    return 1;
}

Console.WriteLine($"{tests.Length} test(s) pasaron.");
return 0;

static class AppConfigTests
{
    public static void DefaultRulesKeepStarterAlerts()
    {
        var config = AppConfig.CreateDefault();

        TestAssert.Equal(6, config.Rules.Count);
        TestAssert.Equal("Seguidor", config.Rules[0].Name);
        TestAssert.True(config.Rules[0].IsEnabled);
        TestAssert.Equal(TwitchEventKind.Follow, config.Rules[0].EventKind);
        TestAssert.Equal("Suscripcion", config.Rules[1].Name);
        TestAssert.Equal(TwitchEventKind.Subscription, config.Rules[1].EventKind);
        TestAssert.Equal("Raid", config.Rules[2].Name);
        TestAssert.Equal(TwitchEventKind.Raid, config.Rules[2].EventKind);
        TestAssert.Equal("Bits", config.Rules[3].Name);
        TestAssert.Equal(TwitchEventKind.Cheer, config.Rules[3].EventKind);
        TestAssert.Equal("Comando chat", config.Rules[4].Name);
        TestAssert.Equal(TwitchEventKind.ChatCommand, config.Rules[4].EventKind);
        TestAssert.Equal("Canje personalizado", config.Rules[5].Name);
        TestAssert.Equal(TwitchEventKind.ChannelPointRedemption, config.Rules[5].EventKind);
    }

    public static void DefaultServicesKeepOptionalIntegrationsDisabled()
    {
        var config = AppConfig.CreateDefault();

        TestAssert.True(config.AutoConnectTwitch);
        TestAssert.False(config.ArduinoEnabled);
        TestAssert.False(config.AutoConnectArduino);
        TestAssert.False(config.Alexa.Enabled);
        TestAssert.False(config.Obs.Enabled);
        TestAssert.Equal(1, config.LedStrips.Count);
        TestAssert.Equal(6, config.LedStrips[0].Pin);
    }
}

static class ConfigurationFactoryTests
{
    public static void CreateRuleUsesSafeDefaults()
    {
        var rule = ConfigurationItemFactory.CreateRule();

        TestAssert.Equal("Nueva regla", rule.Name);
        TestAssert.Equal(TwitchEventKind.Follow, rule.EventKind);
        TestAssert.True(rule.IsEnabled);
        TestAssert.False(rule.UseLights);
        TestAssert.False(rule.PlayAudio);
        TestAssert.False(rule.SendChatMessage);
        TestAssert.False(rule.SendAlexaEvent);
        TestAssert.Equal(1, rule.MinimumBits);
    }

    public static void CreateLedStripSuggestsFirstAvailablePin()
    {
        var strip = ConfigurationItemFactory.CreateLedStrip(
        [
            new LedStripConfig { Pin = 2 },
            new LedStripConfig { Pin = 3 }
        ]);

        TestAssert.Equal("Nueva tira", strip.Name);
        TestAssert.Equal(4, strip.Pin);
        TestAssert.Equal(30, strip.LedCount);
    }
}

static class AppConfigNormalizerTests
{
    public static void TrimsAndClampsLoadedSettings()
    {
        var config = new AppConfig
        {
            ThemeMode = "Oscuro raro",
            BaudRate = 5,
            AlertVolumePercent = 300,
            VideoVolumePercent = -20,
            RecentColors = ["#ff0000", "#FF0000", "bad", "#00ff00", "#111111", "#222222", "#333333", "#444444", "#555555", "#666666"],
            LedStrips = [],
            Rules =
            [
                new EventRule
                {
                    Id = "",
                    Name = "  ",
                    PrimaryColor = "bad",
                    SecondaryColor = "#00ff00",
                    TertiaryColor = "#0000ff"
                }
            ]
        };

        var normalized = AppConfigNormalizer.Normalize(config);

        TestAssert.Equal("System", normalized.ThemeMode);
        TestAssert.Equal(ApplicationLimits.MinBaudRate, normalized.BaudRate);
        TestAssert.Equal(ApplicationLimits.MaxVolumePercent, normalized.AlertVolumePercent);
        TestAssert.Equal(ApplicationLimits.MinVolumePercent, normalized.VideoVolumePercent);
        TestAssert.Equal(ApplicationLimits.MaxRecentColors, normalized.RecentColors.Count);
        TestAssert.Equal(1, normalized.LedStrips.Count);
        TestAssert.Equal("Alerta sin nombre", normalized.Rules[0].Name);
        TestAssert.False(string.IsNullOrWhiteSpace(normalized.Rules[0].Id));
        TestAssert.Equal("#FFFFFF", normalized.Rules[0].PrimaryColor);
        TestAssert.Equal("#00FF00", normalized.Rules[0].SecondaryColor);
    }

    public static void MigratesLegacyRuleAudioPaths()
    {
        var config = new AppConfig
        {
            Rules =
            [
                new EventRule
                {
                    Name = "Legacy audio",
                    EventKind = TwitchEventKind.Follow,
                    PlayAudio = true,
                    AudioPath = @"C:\stream\follow.mp3"
                }
            ]
        };

        var normalized = AppConfigNormalizer.Normalize(config);

        TestAssert.Equal(1, normalized.AudioLibrary.Count);
        TestAssert.Equal("follow", normalized.AudioLibrary[0].Name);
        TestAssert.Equal(@"C:\stream\follow.mp3", normalized.AudioLibrary[0].FilePath);
        TestAssert.Equal(AudioSourceMode.Single, normalized.Rules[0].AudioSourceMode);
        TestAssert.Equal(normalized.AudioLibrary[0].Id, normalized.Rules[0].AudioAssetId);
    }
}

static class EventRuleFilterTests
{
    public static void FiltersStatusAndCategory()
    {
        var activeFollow = new EventRule
        {
            Name = "Follower",
            EventKind = TwitchEventKind.Follow,
            IsEnabled = true
        };
        var inactiveBits = new EventRule
        {
            Name = "Bits",
            EventKind = TwitchEventKind.Cheer,
            IsEnabled = false
        };

        TestAssert.True(EventRuleFilterService.Matches(activeFollow, EventRuleFilterService.ActiveStatus, "", ""));
        TestAssert.False(EventRuleFilterService.Matches(inactiveBits, EventRuleFilterService.ActiveStatus, "", ""));
        TestAssert.True(EventRuleFilterService.Matches(inactiveBits, EventRuleFilterService.InactiveStatus, "", ""));
        TestAssert.True(EventRuleFilterService.Matches(activeFollow, EventRuleFilterService.AllStatus, nameof(TwitchEventKind.Follow), ""));
        TestAssert.False(EventRuleFilterService.Matches(activeFollow, EventRuleFilterService.AllStatus, nameof(TwitchEventKind.Cheer), ""));
    }

    public static void SearchesEditableText()
    {
        var rule = new EventRule
        {
            Name = "Rave azul",
            EventKind = TwitchEventKind.ChatCommand,
            ChatCommand = "baile",
            ChatMessageTemplate = "Gracias @{user}"
        };

        TestAssert.True(EventRuleFilterService.Matches(rule, EventRuleFilterService.AllStatus, "", "rave"));
        TestAssert.True(EventRuleFilterService.Matches(rule, EventRuleFilterService.AllStatus, "", "!BAILE"));
        TestAssert.True(EventRuleFilterService.Matches(rule, EventRuleFilterService.AllStatus, "", "gracias"));
        TestAssert.False(EventRuleFilterService.Matches(rule, EventRuleFilterService.AllStatus, "", "inexistente"));
    }
}

static class EventRuleSnapshotTests
{
    public static void CloneCopiesEditableValues()
    {
        var source = CreateRichRule();
        var clone = EventRuleSnapshotService.Clone(source);

        TestAssert.NotSame(source, clone);
        TestAssert.Equal(source.Id, clone.Id);
        TestAssert.True(EventRuleSnapshotService.HaveSameEditableValues(source, clone));

        clone.Name = "Otro nombre";
        TestAssert.Equal("Regla completa", source.Name);
    }

    public static void DetectsEditableChanges()
    {
        var first = CreateRichRule();
        var second = EventRuleSnapshotService.Clone(first);

        TestAssert.True(EventRuleSnapshotService.HaveSameEditableValues(first, second));

        second.Brightness = first.Brightness + 1;

        TestAssert.False(EventRuleSnapshotService.HaveSameEditableValues(first, second));
    }

    private static EventRule CreateRichRule()
    {
        return new EventRule
        {
            Id = "rule-1",
            Name = "Regla completa",
            EventKind = TwitchEventKind.Cheer,
            MinimumBits = 100,
            UseLights = true,
            PlayAudio = true,
            SendChatMessage = true,
            SendAlexaEvent = true,
            SendObsScene = true,
            SendObsMedia = true,
            ChatMessageTemplate = "Gracias @{user}",
            AlexaEventName = "bits100",
            ObsSceneName = "Gameplay",
            ObsMediaAssetId = "media-1",
            Pattern = LightPattern.Rave,
            TargetPins = "2,3",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            TertiaryColor = "#0000FF",
            Brightness = 120,
            DurationMs = 4500,
            CycleMs = 90,
            StepMs = 140
        };
    }
}

static class RuleSimulationTests
{
    public static void MatchesChatCommandWithNormalization()
    {
        var rule = new EventRule
        {
            EventKind = TwitchEventKind.ChatCommand,
            ChatCommand = "baile"
        };

        TestAssert.True(RuleSimulationService.MatchesChatCommand(rule, "!BAILE con mensaje"));
        TestAssert.False(RuleSimulationService.MatchesChatCommand(rule, "!otro"));
        TestAssert.True(RuleSimulationService.MatchesChatCommand(new EventRule { EventKind = TwitchEventKind.Follow }, null));
    }

    public static void BuildsRepresentativeEvents()
    {
        var cheer = RuleSimulationService.BuildEvent(new EventRule
        {
            EventKind = TwitchEventKind.Cheer,
            MinimumBits = 250
        });

        TestAssert.Equal(TwitchEventKind.Cheer, cheer.Kind);
        TestAssert.Equal(250, cheer.Bits);

        var test = RuleSimulationService.BuildEvent(new EventRule
        {
            EventKind = TwitchEventKind.Test
        });

        TestAssert.Equal(TwitchEventKind.Follow, test.Kind);
        TestAssert.Contains("Simulacion", test.Title);
    }
}

static class EventRuleMatcherTests
{
    public static void ResolvesNormalEventMatches()
    {
        var rules = new[]
        {
            new EventRule { Name = "Follow activo", EventKind = TwitchEventKind.Follow, IsEnabled = true },
            new EventRule { Name = "Follow inactivo", EventKind = TwitchEventKind.Follow, IsEnabled = false },
            new EventRule { Name = "Raid activo", EventKind = TwitchEventKind.Raid, IsEnabled = true }
        };

        var matches = EventRuleMatcherService.ResolveMatches(rules, new TwitchEvent { Kind = TwitchEventKind.Follow });

        TestAssert.Equal(1, matches.Length);
        TestAssert.Equal("Follow activo", matches[0].Name);
    }

    public static void KeepsHighestBitsThreshold()
    {
        var rules = new[]
        {
            new EventRule { Name = "Bits 1", EventKind = TwitchEventKind.Cheer, MinimumBits = 1 },
            new EventRule { Name = "Bits 100", EventKind = TwitchEventKind.Cheer, MinimumBits = 100 },
            new EventRule { Name = "Bits 500", EventKind = TwitchEventKind.Cheer, MinimumBits = 500 }
        };

        var matches = EventRuleMatcherService.ResolveMatches(rules, new TwitchEvent
        {
            Kind = TwitchEventKind.Cheer,
            Bits = 250
        });

        TestAssert.Equal(1, matches.Length);
        TestAssert.Equal("Bits 100", matches[0].Name);
    }
}

static class AlertDurationTests
{
    public static void ResolvesMaximumPositiveDuration()
    {
        var result = AlertDurationService.ResolveMaxEffectDuration(
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(5));

        TestAssert.Equal(TimeSpan.FromSeconds(7), result);
    }

    public static void ClampsSynchronizedDurations()
    {
        var noDuration = AlertDurationService.ResolveSynchronizedEffectDurationMs(null, TimeSpan.Zero);
        TestAssert.Equal<int?>(null, noDuration);

        var tooShort = AlertDurationService.ResolveSynchronizedEffectDurationMs(TimeSpan.FromMilliseconds(10));
        TestAssert.Equal(ApplicationLimits.MinAlertDurationMs, tooShort);

        var tooLong = AlertDurationService.ResolveSynchronizedEffectDurationMs(TimeSpan.FromMilliseconds(ApplicationLimits.MaxAlertDurationMs + 10_000));
        TestAssert.Equal(ApplicationLimits.MaxAlertDurationMs, tooLong);
    }
}

static class LedPreviewTests
{
    public static void CalculatesResponsiveDotCounts()
    {
        TestAssert.Equal(24, LedPreviewService.CalculateDotCount(double.NaN));
        TestAssert.Equal(8, LedPreviewService.CalculateDotCount(10));
        TestAssert.Equal(24, LedPreviewService.CalculateDotCount(768));
        TestAssert.Equal(36, LedPreviewService.CalculateDotCount(5000));
    }

    public static void BuildsSolidFramesWithBrightnessFloor()
    {
        var primary = LedPreviewService.ParseColor("#FF0000", "#000000");
        var frame = LedPreviewService.BuildFrame(
            LightPattern.Solid,
            step: 1,
            count: 2,
            brightness: 0,
            primary,
            LedPreviewService.ParseColor("#00FF00", "#000000"),
            LedPreviewService.ParseColor("#0000FF", "#000000"),
            new Random(1));

        TestAssert.Equal(2, frame.Length);
        TestAssert.Equal((byte)20, frame[0].R);
        TestAssert.Equal((byte)0, frame[0].G);
        TestAssert.Equal((byte)0, frame[0].B);
    }

    public static void BuildsRainbowFrames()
    {
        var frame = LedPreviewService.BuildFrame(
            LightPattern.Rainbow,
            step: 0,
            count: 3,
            brightness: 1,
            LedPreviewService.ParseColor("#FF0000", "#000000"),
            LedPreviewService.ParseColor("#00FF00", "#000000"),
            LedPreviewService.ParseColor("#0000FF", "#000000"),
            new Random(1));

        TestAssert.Equal(3, frame.Length);
        TestAssert.Equal((byte)255, frame[0].R);
        TestAssert.Equal((byte)0, frame[0].B);
        TestAssert.True(frame[1].G > 0);
        TestAssert.True(frame[2].B > 0);
    }
}

static class AudioRuleAssetTests
{
    public static void ResolvesSingleAssets()
    {
        var audio = new AudioAssetConfig
        {
            Id = "a1",
            FilePath = @"C:\stream\follow.mp3"
        };
        var rule = new EventRule
        {
            PlayAudio = true,
            AudioSourceMode = AudioSourceMode.Single,
            AudioAssetId = "a1"
        };

        var resolved = AudioRuleAssetService.ResolveRuleAudioAsset(rule, [audio], new Random(1), _ => true);

        TestAssert.Same(audio, resolved);
        TestAssert.True(AudioRuleAssetService.HasValidAudio(rule, [audio], new Random(1), _ => true));
    }

    public static void ResolvesGroupAssetsWithExistingFiles()
    {
        var missing = new AudioAssetConfig
        {
            Id = "missing",
            GroupId = "g1",
            FilePath = @"C:\stream\missing.mp3"
        };
        var existing = new AudioAssetConfig
        {
            Id = "existing",
            GroupId = "g1",
            FilePath = @"C:\stream\ok.mp3"
        };
        var rule = new EventRule
        {
            PlayAudio = true,
            AudioSourceMode = AudioSourceMode.Group,
            AudioGroupId = "g1"
        };

        var resolved = AudioRuleAssetService.ResolveRuleAudioAsset(
            rule,
            [missing, existing],
            new Random(1),
            path => path.EndsWith("ok.mp3", StringComparison.OrdinalIgnoreCase));

        TestAssert.Same(existing, resolved);
    }

    public static void DetectsRuleAssetUsage()
    {
        var audio = new AudioAssetConfig
        {
            Id = "a1",
            GroupId = "g1",
            FilePath = @"C:\stream\follow.mp3"
        };

        TestAssert.True(AudioRuleAssetService.RuleUsesAudioAsset(
            new EventRule
            {
                PlayAudio = true,
                AudioSourceMode = AudioSourceMode.Single,
                AudioAssetId = "a1"
            },
            audio));

        TestAssert.True(AudioRuleAssetService.RuleUsesAudioAsset(
            new EventRule
            {
                PlayAudio = true,
                AudioSourceMode = AudioSourceMode.Group,
                AudioGroupId = "g1"
            },
            audio));

        TestAssert.False(AudioRuleAssetService.RuleUsesAudioAsset(
            new EventRule
            {
                PlayAudio = false,
                AudioAssetId = "a1"
            },
            audio));
    }
}

static class LibraryRowFilterTests
{
    public static void FiltersAudioRows()
    {
        var row = new AudioLibraryRow(
            "a1",
            "Cheer corto",
            @"C:\audio\cheer.mp3",
            "g1",
            "Bits",
            "Reacciones",
            "00:03",
            true,
            false,
            System.Windows.Media.Brushes.Cyan,
            System.Windows.Media.Brushes.Transparent,
            0);

        TestAssert.True(LibraryRowFilterService.MatchesAudio(row, "", LibraryRowFilterService.AllFilter, "cheer", "Sin grupo"));
        TestAssert.True(LibraryRowFilterService.MatchesAudio(row, "g1", LibraryRowFilterService.AudioWithAlertFilter, "bits", "Sin grupo"));
        TestAssert.False(LibraryRowFilterService.MatchesAudio(row, "g2", LibraryRowFilterService.AllFilter, "", "Sin grupo"));
        TestAssert.False(LibraryRowFilterService.MatchesAudio(row, "", LibraryRowFilterService.AudioNoGroupFilter, "", "Sin grupo"));
    }

    public static void FiltersMediaRows()
    {
        var row = new MediaLibraryRow(
            "m1",
            "Raid gif",
            @"C:\media\raid.gif",
            "g1",
            "Especiales",
            "800 x 600",
            "Assets/Icons/media_image.png",
            System.Windows.Media.Brushes.Cyan,
            System.Windows.Media.Brushes.Transparent,
            0,
            true,
            false);

        TestAssert.True(LibraryRowFilterService.MatchesMedia(row, "", LibraryRowFilterService.AllFilter, "raid"));
        TestAssert.True(LibraryRowFilterService.MatchesMedia(row, "g1", LibraryRowFilterService.MediaWithGroupFilter, "800"));
        TestAssert.False(LibraryRowFilterService.MatchesMedia(row, "g2", LibraryRowFilterService.AllFilter, ""));
        TestAssert.False(LibraryRowFilterService.MatchesMedia(row, "", LibraryRowFilterService.MediaNoGroupFilter, ""));
    }
}

static class MediaRuleAssetTests
{
    public static void ResolvesSingleMediaAssets()
    {
        var image = new MediaAssetConfig
        {
            Id = "image1",
            FilePath = @"C:\media\follow.png"
        };
        var rule = new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Image,
            ObsMediaSourceMode = MediaSourceMode.Single,
            ObsMediaAssetId = "image1"
        };

        var resolved = MediaRuleAssetService.ResolveRuleMediaAsset(rule, [image], [], new Random(1), _ => true);

        TestAssert.Same(image, resolved);
    }

    public static void ResolvesGroupMediaAssets()
    {
        var missing = new MediaAssetConfig
        {
            Id = "missing",
            GroupId = "g1",
            FilePath = @"C:\media\missing.mp4"
        };
        var existing = new MediaAssetConfig
        {
            Id = "existing",
            GroupId = "g1",
            FilePath = @"C:\media\ok.mp4"
        };
        var rule = new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Video,
            ObsMediaSourceMode = MediaSourceMode.Group,
            ObsMediaGroupId = "g1"
        };

        var resolved = MediaRuleAssetService.ResolveRuleMediaAsset(
            rule,
            [],
            [missing, existing],
            new Random(1),
            path => path.EndsWith("ok.mp4", StringComparison.OrdinalIgnoreCase));

        TestAssert.Same(existing, resolved);
    }

    public static void ResolvesImageAndVideoDurations()
    {
        var imageRule = new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Image,
            ObsMediaDurationMs = 2000
        };
        var videoRule = new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Video
        };
        var video = new MediaAssetConfig { DurationMs = 7000 };

        TestAssert.Equal(TimeSpan.FromSeconds(2), MediaRuleAssetService.ResolveRuleMediaDuration(imageRule, new MediaAssetConfig()));
        TestAssert.Equal(TimeSpan.FromSeconds(7), MediaRuleAssetService.ResolveRuleMediaDuration(videoRule, video));
        TestAssert.Equal(TimeSpan.FromSeconds(5), MediaRuleAssetService.ResolveRuleMediaDuration(videoRule, new MediaAssetConfig()));
    }
}

static class ConnectionStateTests
{
    public static void ResolvesServiceStates()
    {
        TestAssert.Equal(ConnectionVisualState.Connecting, ConnectionStateService.ResolveTwitch(false, true, false, false));
        TestAssert.Equal(ConnectionVisualState.Warning, ConnectionStateService.ResolveTwitch(false, false, true, true));
        TestAssert.Equal(ConnectionVisualState.Connected, ConnectionStateService.ResolveTwitch(false, false, false, true));
        TestAssert.Equal(ConnectionVisualState.Disabled, ConnectionStateService.ResolveArduino(false, false, false, false, false));
        TestAssert.Equal(ConnectionVisualState.Connecting, ConnectionStateService.ResolveArduino(true, false, false, false, true));
        TestAssert.Equal(ConnectionVisualState.Connected, ConnectionStateService.ResolveArduino(true, false, true, false, false));
        TestAssert.Equal(ConnectionVisualState.Warning, ConnectionStateService.ResolveAlexa(true, false, true, false));
        TestAssert.Equal(ConnectionVisualState.Warning, ConnectionStateService.ResolveObs(true, false, false, true));
    }

    public static void MapsVisualMetadata()
    {
        var connected = ConnectionStateService.GetVisual(ConnectionVisualState.Connected, connectedText: "Listo");
        var disabled = ConnectionStateService.GetVisual(ConnectionVisualState.Disabled);

        TestAssert.Equal("Listo", connected.Text);
        TestAssert.Equal("#22C55E", connected.Color);
        TestAssert.Contains("status_ok.png", connected.IconPath);
        TestAssert.Equal("Desactivado", disabled.Text);
        TestAssert.Contains("status_empty.png", disabled.IconPath);

        var appWarning = ConnectionStateService.GetAppStateVisual(ConnectionVisualState.Warning);
        TestAssert.Equal("Estado: Hay puntos por revisar", appWarning.Text);
        TestAssert.Contains("appstate_warning.png", appWarning.IconPath);
    }
}

static class TestAssert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Se esperaba verdadero.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new InvalidOperationException(message ?? "Se esperaba falso.");
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Esperado: {expected}. Actual: {actual}.");
        }
    }

    public static void Contains(string expectedFragment, string? actual)
    {
        if (actual?.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException($"No se encontro '{expectedFragment}' en '{actual}'.");
        }
    }

    public static void NotSame(object expectedDifferent, object actual)
    {
        if (ReferenceEquals(expectedDifferent, actual))
        {
            throw new InvalidOperationException("Se esperaba una instancia diferente.");
        }
    }

    public static void Same(object? expected, object? actual)
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidOperationException("Se esperaba la misma instancia.");
        }
    }
}
