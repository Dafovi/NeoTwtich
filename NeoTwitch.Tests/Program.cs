using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;

var tests = new (string Name, Action Body)[]
{
    ("ConfigurationItemFactory creates inactive action defaults", ConfigurationFactoryTests.CreateRuleUsesSafeDefaults),
    ("ConfigurationItemFactory suggests first available pin", ConfigurationFactoryTests.CreateLedStripSuggestsFirstAvailablePin),
    ("EventRuleFilterService filters status and category", EventRuleFilterTests.FiltersStatusAndCategory),
    ("EventRuleFilterService searches editable text", EventRuleFilterTests.SearchesEditableText),
    ("EventRuleSnapshotService clones editable values independently", EventRuleSnapshotTests.CloneCopiesEditableValues),
    ("EventRuleSnapshotService detects editable changes", EventRuleSnapshotTests.DetectsEditableChanges),
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
}
