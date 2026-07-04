using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Configuration;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Navigation;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Alexa;
using NeoTwitch.ViewModels.Alerts;
using NeoTwitch.ViewModels.Connections;
using NeoTwitch.ViewModels.Dashboard;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Lights;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Settings;
using NeoTwitch.ViewModels.Shell;
using NeoTwitch.ViewModels.Status;

var tests = new (string Name, Action Body)[]
{
    ("ConfigurationItemFactory creates inactive action defaults", ConfigurationFactoryTests.CreateRuleUsesSafeDefaults),
    ("ConfigurationItemFactory suggests first available pin", ConfigurationFactoryTests.CreateLedStripSuggestsFirstAvailablePin),
    ("InputValueParser parses preferred COM ports", InputValueParserTests.ParsesPreferredComPorts),
    ("InputValueParser prefers detected Arduino ports", InputValueParserTests.PrefersDetectedArduinoPorts),
    ("InputValueParser clamps integer values", InputValueParserTests.ClampsIntegerValues),
    ("AppConfig default rules keep expected starter alerts", AppConfigTests.DefaultRulesKeepStarterAlerts),
    ("AppConfig default services keep optional integrations disabled", AppConfigTests.DefaultServicesKeepOptionalIntegrationsDisabled),
    ("AppConfigNormalizer trims and clamps loaded settings", AppConfigNormalizerTests.TrimsAndClampsLoadedSettings),
    ("AppConfigNormalizer migrates legacy rule audio paths", AppConfigNormalizerTests.MigratesLegacyRuleAudioPaths),
    ("GlobalSettingsFormService applies normalized values", GlobalSettingsFormTests.AppliesNormalizedValues),
    ("EventRuleFilterService filters status and category", EventRuleFilterTests.FiltersStatusAndCategory),
    ("EventRuleFilterService searches editable text", EventRuleFilterTests.SearchesEditableText),
    ("AlertsViewModel maps filters and count", AlertsViewModelTests.MapsFiltersAndCount),
    ("AlertsViewModel executes editor selector commands", AlertsViewModelTests.ExecutesEditorSelectorCommands),
    ("RuleEditorViewModel maps basic fields", RuleEditorViewModelTests.MapsBasicFields),
    ("EventRulePresentationService builds row display metadata", EventRulePresentationTests.BuildsRowDisplayMetadata),
    ("EventRuleSnapshotService clones editable values independently", EventRuleSnapshotTests.CloneCopiesEditableValues),
    ("EventRuleSnapshotService detects editable changes", EventRuleSnapshotTests.DetectsEditableChanges),
    ("RuleEditorValueService resolves fallback names", RuleEditorValueTests.ResolvesFallbackNames),
    ("RuleEditorValueService resolves legacy audio paths", RuleEditorValueTests.ResolvesLegacyAudioPaths),
    ("RuleEditorFormService applies normalized values", RuleEditorFormTests.AppliesNormalizedValues),
    ("RuleObsMediaChoiceService resolves image and video libraries", RuleObsMediaChoiceTests.ResolvesImageAndVideoLibraries),
    ("EventRuleMatcherService resolves normal event matches", EventRuleMatcherTests.ResolvesNormalEventMatches),
    ("EventRuleMatcherService keeps highest bits threshold", EventRuleMatcherTests.KeepsHighestBitsThreshold),
    ("TwitchEventSubSubscriptionPlanner builds unique definitions", TwitchEventSubSubscriptionPlannerTests.BuildsUniqueDefinitions),
    ("TwitchEventSubMessageParser parses welcome and events", TwitchEventSubMessageParserTests.ParsesWelcomeAndEvents),
    ("AlertDurationService resolves maximum positive duration", AlertDurationTests.ResolvesMaximumPositiveDuration),
    ("AlertDurationService clamps synchronized durations", AlertDurationTests.ClampsSynchronizedDurations),
    ("AlertExecutionPlanService disables lights when Arduino is disabled", AlertExecutionPlanTests.DisablesLightsWhenArduinoIsDisabled),
    ("AlertExecutionPlanService resolves light command and reconnect state", AlertExecutionPlanTests.ResolvesLightCommandAndReconnectState),
    ("ObsRulePlanService resolves scene restore", ObsRulePlanTests.ResolvesSceneRestore),
    ("ObsRulePlanService resolves media plans", ObsRulePlanTests.ResolvesMediaPlans),
    ("ObsRulePlanService builds media execution plan", ObsRulePlanTests.BuildsMediaExecutionPlan),
    ("ObsWebSocketRequestFactory builds protocol requests", ObsWebSocketRequestFactoryTests.BuildsProtocolRequests),
    ("ObsWebSocketResponseReader parses protocol responses", ObsWebSocketResponseReaderTests.ParsesProtocolResponses),
    ("LightControlInputService resolves presets", LightControlInputTests.ResolvesPresets),
    ("LightControlInputService parses and clamps values", LightControlInputTests.ParsesAndClampsValues),
    ("BackgroundLightRestoreService resolves retry attempts", BackgroundLightRestoreTests.ResolvesRetryAttempts),
    ("BackgroundLightRestoreService resolves apply plan", BackgroundLightRestoreTests.ResolvesApplyPlan),
    ("BackgroundLightRestoreService resolves restore plan", BackgroundLightRestoreTests.ResolvesRestorePlan),
    ("RulePinChoiceService builds pin choices", RulePinChoiceTests.BuildsPinChoices),
    ("SerialPortNameService cleans friendly port names", SerialPortNameTests.CleansFriendlyPortNames),
    ("SerialLightProtocol resolves commands", SerialLightProtocolTests.ResolvesCommands),
    ("SerialLightProtocol detects responses", SerialLightProtocolTests.DetectsResponses),
    ("LedPreviewService calculates responsive dot counts", LedPreviewTests.CalculatesResponsiveDotCounts),
    ("LedPreviewService builds solid frames with brightness floor", LedPreviewTests.BuildsSolidFramesWithBrightnessFloor),
    ("LedPreviewService builds rainbow frames", LedPreviewTests.BuildsRainbowFrames),
    ("AudioRuleAssetService resolves single assets", AudioRuleAssetTests.ResolvesSingleAssets),
    ("AudioRuleAssetService resolves group assets with existing files", AudioRuleAssetTests.ResolvesGroupAssetsWithExistingFiles),
    ("AudioRuleAssetService detects rule asset usage", AudioRuleAssetTests.DetectsRuleAssetUsage),
    ("AudioLibraryMutationService removes assets and cleans rules", AudioLibraryMutationTests.RemovesAssetsAndCleansRules),
    ("MediaLibraryMutationService removes assets and cleans rules", MediaLibraryMutationTests.RemovesAssetsAndCleansRules),
    ("LibraryAssetUsageService marks asset usage", LibraryAssetUsageTests.MarksAssetUsage),
    ("LibraryGroupService creates and reuses groups", LibraryGroupServiceTests.CreatesAndReusesGroups),
    ("LibraryGroupService clears group references", LibraryGroupServiceTests.ClearsGroupReferences),
    ("LibraryGroupRowFactoryService builds audio and media groups", LibraryGroupRowFactoryTests.BuildsAudioAndMediaGroups),
    ("LibrarySummaryService formats counts and last usage", LibrarySummaryTests.FormatsCountsAndLastUsage),
    ("LibraryScreenViewModel updates rows and summary", LibraryScreenViewModelTests.UpdatesRowsAndSummary),
    ("SettingsViewModel executes configured actions", SettingsViewModelTests.ExecutesConfiguredActions),
    ("MediaLibraryKindCatalog maps media metadata", MediaLibraryKindCatalogTests.MapsMediaMetadata),
    ("MediaPreviewPlanService builds OBS preview plans", MediaPreviewPlanTests.BuildsPreviewPlans),
    ("LibraryRowFactoryService builds audio rows", LibraryRowFactoryTests.BuildsAudioRows),
    ("LibraryRowFactoryService builds media rows", LibraryRowFactoryTests.BuildsMediaRows),
    ("LibraryRowFilterService filters audio rows", LibraryRowFilterTests.FiltersAudioRows),
    ("LibraryRowFilterService filters media rows", LibraryRowFilterTests.FiltersMediaRows),
    ("MediaRuleAssetService resolves single media assets", MediaRuleAssetTests.ResolvesSingleMediaAssets),
    ("MediaRuleAssetService resolves group media assets", MediaRuleAssetTests.ResolvesGroupMediaAssets),
    ("MediaRuleAssetService resolves image and video durations", MediaRuleAssetTests.ResolvesImageAndVideoDurations),
    ("ConnectionStateService resolves service states", ConnectionStateTests.ResolvesServiceStates),
    ("ConnectionStateService maps visual metadata", ConnectionStateTests.MapsVisualMetadata),
    ("ConnectionButtonStateService disables Twitch while busy", ConnectionButtonStateTests.DisablesTwitchWhileBusy),
    ("ConnectionButtonStateService maps OBS buttons", ConnectionButtonStateTests.MapsObsButtons),
    ("TwitchConnectionRecoveryService detects recoverable refresh errors", TwitchConnectionRecoveryTests.DetectsRecoverableRefreshErrors),
    ("ServiceNavigationVisibilityService hides optional service tabs", ServiceNavigationVisibilityTests.HidesOptionalServiceTabs),
    ("ShellViewModel maps navigation visibility", ShellViewModelTests.MapsNavigationVisibility),
    ("ShellViewModel maps profile and live state", ShellViewModelTests.MapsProfileAndLiveState),
    ("ObsStatusTextService builds display values", ObsStatusTextTests.BuildsDisplayValues),
    ("ObsSceneViewService builds rows and choices", ObsSceneViewTests.BuildsRowsAndChoices),
    ("ObsViewModel updates status and scenes", ObsViewModelTests.UpdatesStatusAndScenes),
    ("ObsViewModel executes configured actions", ObsViewModelTests.ExecutesConfiguredActions),
    ("DiagnosticReportService builds report without network", DiagnosticReportServiceTests.BuildsReportWithoutNetwork),
    ("DiagnosticReportService reports missing audio", DiagnosticReportServiceTests.ReportsMissingAudio),
    ("VersionComparisonService compares normalized tags", VersionComparisonTests.ComparesNormalizedTags),
    ("ActivityLogService trims activity and dashboard entries", ActivityLogServiceTests.TrimsActivityAndDashboardEntries),
    ("ActivityLogService filters entries and search text", ActivityLogServiceTests.FiltersEntriesAndSearchText),
    ("ActivityLogClassifier resolves sources and categories", ActivityLogClassifierTests.ResolvesSourcesAndCategories),
    ("ActivityLogPresentationService classifies display metadata", ActivityLogPresentationTests.ClassifiesDisplayMetadata),
    ("ActivityViewModel filters entries view", ActivityViewModelTests.FiltersEntriesView),
    ("ActivityViewModel maps filter properties", ActivityViewModelTests.MapsFilterProperties),
    ("ConnectionsViewModel maps badges and helper text", ConnectionsViewModelTests.MapsBadgesAndHelperText),
    ("ConnectionsViewModel maps button states", ConnectionsViewModelTests.MapsButtonStates),
    ("ConnectionsViewModel executes configured actions", ConnectionsViewModelTests.ExecutesConfiguredActions),
    ("AlexaViewModel executes configured actions", AlexaViewModelTests.ExecutesConfiguredActions),
    ("LightsViewModel executes configured actions", LightsViewModelTests.ExecutesConfiguredActions),
    ("DashboardConnectionStateService resolves all services", DashboardConnectionStateTests.ResolvesAllServices),
    ("DashboardSummaryService counts Twitch events", DashboardSummaryTests.CountsTwitchEvents),
    ("DashboardSummaryService counts matched rules safely", DashboardSummaryTests.CountsMatchedRulesSafely),
    ("DashboardSummaryDisplayService formats summary metrics", DashboardSummaryDisplayTests.FormatsSummaryMetrics),
    ("DashboardViewModel updates summary metrics", DashboardViewModelTests.UpdatesSummaryMetrics),
    ("DashboardViewModel updates connection states", DashboardViewModelTests.UpdatesConnectionStates),
    ("DashboardStatusTextLabelFactory builds labels", DashboardStatusTextLabelFactoryTests.BuildsLabels),
    ("DashboardStatusTextService formats live Twitch status", DashboardStatusTextTests.FormatsLiveTwitchStatus),
    ("DashboardStatusTextService formats connection labels", DashboardStatusTextTests.FormatsConnectionLabels),
    ("DashboardStatusTextService formats Arduino status", DashboardStatusTextTests.FormatsArduinoStatus),
    ("DashboardStatusTextService formats Alexa background status", DashboardStatusTextTests.FormatsAlexaBackgroundStatus),
    ("SpanishUiTextCatalog contains all text keys", UiTextCatalogTests.ContainsAllTextKeys),
    ("UiTextFormatter formats fallback text", UiTextFormatterTests.FormatsFallbackText),
    ("UiTextFormatter builds bounded secret masks", UiTextFormatterTests.BuildsBoundedSecretMasks),
    ("CircularProgressGeometryService calculates percentages", CircularProgressGeometryTests.CalculatesPercentages),
    ("CircularProgressGeometryService builds arc geometry", CircularProgressGeometryTests.BuildsArcGeometry),
    ("IconPathCatalog returns known icons and fallback", IconPathCatalogTests.ReturnsKnownIconsAndFallback),
    ("ButtonIconCatalog maps button labels", ButtonIconCatalogTests.MapsButtonLabels),
    ("ButtonIconContentService builds icon button content", ButtonIconContentTests.BuildsIconButtonContent),
    ("VisualTreeTraversalService finds descendants", VisualTreeTraversalTests.FindsDescendants),
    ("FilterButtonThemeService applies active and inactive colors", FilterButtonThemeTests.AppliesActiveAndInactiveColors),
    ("NavigationButtonThemeService applies selected colors", NavigationButtonThemeTests.AppliesSelectedColors),
    ("ThemeElementApplicationService applies common controls", ThemeElementApplicationTests.AppliesCommonControls),
    ("ColorConversionService converts hex and HSV values", ColorConversionTests.ConvertsHexAndHsvValues),
    ("UiVisibilityService toggles multiple elements", UiVisibilityTests.TogglesMultipleElements),
    ("OptionVisibilityService resolves rule panels", OptionVisibilityTests.ResolvesRulePanels),
    ("OptionVisibilityService resolves background panels", OptionVisibilityTests.ResolvesBackgroundPanels),
    ("UiAccentCatalog maps event and pattern colors", UiAccentCatalogTests.MapsEventAndPatternColors),
    ("UiBrushFactory creates frozen brushes", UiBrushFactoryTests.CreatesFrozenBrushes),
    ("ThemeResourceService applies palette resources", ThemeResourceTests.AppliesPaletteResources),
    ("RuleTestValidationService blocks missing audio", RuleTestValidationTests.BlocksMissingAudio),
    ("RuleTestValidationService reports non blocking issues", RuleTestValidationTests.ReportsNonBlockingIssues),
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

static class InputValueParserTests
{
    public static void ParsesPreferredComPorts()
    {
        TestAssert.Equal("", InputValueParser.ParsePort(""));
        TestAssert.Equal("COM3", InputValueParser.ParsePort("COM1, COM3"));
        TestAssert.Equal("COM4", InputValueParser.ParsePort("texto COM4 otro"));
        TestAssert.Equal("COM1", InputValueParser.ParsePort("COM1"));
    }

    public static void PrefersDetectedArduinoPorts()
    {
        var ports = new[]
        {
            new SerialPortInfo("COM1", "Puerto del sistema", false, 1),
            new SerialPortInfo("COM7", "USB Serial", false, 7),
            new SerialPortInfo("COM3", "Arduino Uno", true, 3)
        };

        TestAssert.Equal("COM3", InputValueParser.ChoosePreferredPort(ports));
        TestAssert.Equal("COM7", InputValueParser.ChoosePreferredPort(ports[..2]));
    }

    public static void ClampsIntegerValues()
    {
        TestAssert.Equal(10, InputValueParser.ParseInt("nope", 10, 1, 20));
        TestAssert.Equal(1, InputValueParser.ParseInt("-20", 10, 1, 20));
        TestAssert.Equal(20, InputValueParser.ParseInt("200", 10, 1, 20));
        TestAssert.Equal(12, InputValueParser.ParseInt("12", 10, 1, 20));
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

static class GlobalSettingsFormTests
{
    public static void AppliesNormalizedValues()
    {
        var config = AppConfig.CreateDefault();

        GlobalSettingsFormService.Apply(
            config,
            new GlobalSettingsFormValues(
                " client ",
                " secret ",
                " COM7 ",
                "999999",
                ArduinoEnabled: true,
                AutoConnectTwitch: false,
                AutoConnectArduino: true,
                StartHidden: true,
                StartWithWindows: true,
                ThemeMode: "dark",
                CloseToTray: false,
                AlertVolumePercent: 42.6,
                VideoVolumePercent: 12.2,
                MaxQueuedSameRuleAlerts: "999",
                SameRuleQueueCooldownMs: "-1",
                MaxQueuedDifferentRuleAlerts: "abc",
                DifferentRuleQueueCooldownMs: "700000",
                AlexaEnabled: true,
                AlexaRelayUrl: " https://relay ",
                AlexaAuthToken: " token ",
                ObsEnabled: true,
                ObsHost: "",
                ObsPort: "999999",
                ObsPassword: "pass",
                ObsAutoReconnect: true,
                ObsOverlayWidth: "10",
                ObsOverlayHeight: "99999",
                ObsOverlayMediaWidth: "bad",
                ObsOverlayMediaHeight: "5",
                ObsOverlayPositionMode: "",
                ObsOverlayX: "-1",
                ObsOverlayY: "99999"));

        TestAssert.Equal("client", config.TwitchClientId);
        TestAssert.Equal("COM7", config.SerialPort);
        TestAssert.Equal(ApplicationLimits.MaxBaudRate, config.BaudRate);
        TestAssert.True(config.ArduinoEnabled);
        TestAssert.False(config.AutoConnectTwitch);
        TestAssert.Equal("Dark", config.ThemeMode);
        TestAssert.Equal(43, config.AlertVolumePercent);
        TestAssert.Equal(12, config.VideoVolumePercent);
        TestAssert.Equal(100, config.MaxQueuedSameRuleAlerts);
        TestAssert.Equal(0, config.SameRuleQueueCooldownMs);
        TestAssert.Equal(3, config.MaxQueuedDifferentRuleAlerts);
        TestAssert.Equal(600000, config.DifferentRuleQueueCooldownMs);
        TestAssert.Equal("https://relay", config.Alexa.RelayUrl);
        TestAssert.Equal("127.0.0.1", config.Obs.Host);
        TestAssert.Equal(ApplicationLimits.MaxNetworkPort, config.Obs.Port);
        TestAssert.Equal(320, config.Obs.OverlayWidth);
        TestAssert.Equal(4320, config.Obs.OverlayHeight);
        TestAssert.Equal(720, config.Obs.OverlayMediaWidth);
        TestAssert.Equal(32, config.Obs.OverlayMediaHeight);
        TestAssert.Equal("Center", config.Obs.OverlayPositionMode);
        TestAssert.Equal(0, config.Obs.OverlayX);
        TestAssert.Equal(4320, config.Obs.OverlayY);
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

static class AlertsViewModelTests
{
    public static void MapsFiltersAndCount()
    {
        var changes = 0;
        var viewModel = new AlertsViewModel(UiOptionCatalog.RuleCategoryOptions);
        viewModel.FiltersChanged += (_, _) => changes++;

        viewModel.SearchText = "raid";
        viewModel.CategoryFilter = nameof(TwitchEventKind.Raid);
        viewModel.SelectStatusFilter(EventRuleFilterService.ActiveStatus);
        viewModel.UpdateRulesCount(2, 5);
        var activeRaid = new EventRule
        {
            Name = "Raid grande",
            EventKind = TwitchEventKind.Raid,
            IsEnabled = true
        };
        var inactiveRaid = new EventRule
        {
            Name = "Raid apagada",
            EventKind = TwitchEventKind.Raid,
            IsEnabled = false
        };
        viewModel.SetRulesSource([activeRaid, inactiveRaid]);

        TestAssert.Equal("raid", viewModel.SearchText);
        TestAssert.Equal(nameof(TwitchEventKind.Raid), viewModel.CategoryFilter);
        TestAssert.Equal(EventRuleFilterService.ActiveStatus, viewModel.StatusFilter);
        TestAssert.False(viewModel.IsAllStatusSelected);
        TestAssert.True(viewModel.IsActiveStatusSelected);
        TestAssert.Equal("Mostrando 1 de 2 alertas", viewModel.RulesCountText);
        TestAssert.True(viewModel.ContainsRule(activeRaid));
        TestAssert.False(viewModel.ContainsRule(inactiveRaid));
        TestAssert.Same(activeRaid, viewModel.FirstVisibleRule());
        var selectionChanges = 0;
        viewModel.SelectedRuleChanged += (_, _) => selectionChanges++;
        viewModel.SelectedRule = activeRaid;
        TestAssert.Same(activeRaid, viewModel.SelectedRule);
        TestAssert.Equal(1, selectionChanges);
        TestAssert.True(changes >= 3);

        viewModel.SetEditorEnabled(true);
        viewModel.SetDirtyState(true);

        TestAssert.True(viewModel.IsEditorEnabled);
        TestAssert.True(viewModel.HasUnsavedChanges);
        TestAssert.Equal(1d, viewModel.SaveButtonOpacity);
        TestAssert.Contains("pendientes", viewModel.SaveButtonToolTip);

        viewModel.ClearFilters();
        viewModel.SetDirtyState(false);

        TestAssert.Equal("", viewModel.SearchText);
        TestAssert.Equal("", viewModel.CategoryFilter);
        TestAssert.Equal(EventRuleFilterService.AllStatus, viewModel.StatusFilter);
        TestAssert.True(viewModel.IsAllStatusSelected);
        TestAssert.Equal(0.68d, viewModel.SaveButtonOpacity);

        var eventChoices = new[] { "evento" };
        var patternChoices = new[] { "patron" };
        var audioAssets = new[] { "audio" };
        var audioGroups = new[] { "grupo-audio" };
        var obsScenes = new[] { "escena" };
        var obsKinds = new[] { "obs-kind" };
        var obsModes = new[] { "obs-mode" };
        viewModel.UpdateEditorChoices(eventChoices, patternChoices, audioAssets, audioGroups, obsScenes, obsKinds, obsModes);

        TestAssert.Same(eventChoices, viewModel.EventKindChoices);
        TestAssert.Same(patternChoices, viewModel.LightPatternChoices);
        TestAssert.Same(audioAssets, viewModel.AudioAssetChoices);
        TestAssert.Same(audioGroups, viewModel.AudioGroupChoices);
        TestAssert.Same(obsScenes, viewModel.ObsSceneChoices);
        TestAssert.Same(obsKinds, viewModel.ObsMediaKindChoices);
        TestAssert.Same(obsModes, viewModel.ObsMediaSourceModeChoices);
        var targetPins = new[] { "pin 6" };
        viewModel.UpdateTargetPinChoices(targetPins);
        TestAssert.Same(targetPins, viewModel.TargetPinChoices);
        var obsAssets = new[] { "imagen" };
        var obsGroups = new[] { "grupo-imagen" };
        viewModel.UpdateObsMediaChoices(obsAssets, obsGroups);
        TestAssert.Same(obsAssets, viewModel.ObsMediaAssetChoices);
        TestAssert.Same(obsGroups, viewModel.ObsMediaGroupChoices);
        TestAssert.Equal(0, viewModel.LedPreviewDots.Count);

        var actions = new List<string>();
        viewModel.ConfigureActions(
            () => actions.Add("add"),
            () => actions.Add("duplicate"),
            () => actions.Add("test"),
            () => actions.Add("save"),
            () => actions.Add("remove"));

        viewModel.AddRuleCommand.Execute(null);
        viewModel.DuplicateRuleCommand.Execute(null);
        viewModel.TestRuleCommand.Execute(null);
        viewModel.SaveRuleCommand.Execute(null);
        viewModel.RemoveRuleCommand.Execute(null);

        TestAssert.Equal("add,duplicate,test,save,remove", string.Join(",", actions));
    }

    public static void ExecutesEditorSelectorCommands()
    {
        var actions = new List<string>();
        var viewModel = new AlertsViewModel(UiOptionCatalog.RuleCategoryOptions);

        viewModel.ConfigureEditorActions(
            parameter => actions.Add($"event:{parameter}"),
            parameter => actions.Add($"pattern:{parameter}"),
            parameter => actions.Add($"preset:{parameter}"),
            parameter => actions.Add($"adjust:{parameter}"),
            parameter => actions.Add($"color:{parameter}"),
            parameter => actions.Add($"audio:{parameter}"),
            parameter => actions.Add($"obs-kind:{parameter}"),
            parameter => actions.Add($"obs-mode:{parameter}"));

        viewModel.SelectEventKindCommand.Execute(TwitchEventKind.Follow);
        viewModel.SelectLightPatternCommand.Execute(LightPattern.Rave);
        viewModel.SelectLightPresetCommand.Execute("Fast");
        viewModel.AdjustLightValueCommand.Execute("Brightness:15");
        viewModel.PickLightColorCommand.Execute("Primary");
        viewModel.SelectAudioModeCommand.Execute(AudioSourceMode.Group);
        viewModel.SelectObsMediaKindCommand.Execute(ObsMediaKind.Video);
        viewModel.SelectObsMediaSourceModeCommand.Execute(MediaSourceMode.Single);

        TestAssert.Equal(
            "event:Follow,pattern:Rave,preset:Fast,adjust:Brightness:15,color:Primary,audio:Group,obs-kind:Video,obs-mode:Single",
            string.Join(",", actions));
    }
}

static class EventRulePresentationTests
{
    public static void BuildsRowDisplayMetadata()
    {
        var rule = new EventRule
        {
            Name = "Bits cien",
            EventKind = TwitchEventKind.Cheer,
            MinimumBits = 100,
            IsEnabled = false,
            UseLights = true,
            PlayAudio = true,
            SendObsMedia = true,
            ObsActionAvailable = false
        };

        TestAssert.Equal("Bits cien - Bits >= 100 bits", EventRulePresentationService.BuildDisplayLabel(rule));
        TestAssert.Equal("Inactiva", EventRulePresentationService.BuildStatusText(rule));
        TestAssert.Equal("#37C7F3", EventRulePresentationService.BuildEventAccentColor(rule));
        TestAssert.Equal("Luces / Audio / OBS", EventRulePresentationService.BuildActionsSummary(rule));
        TestAssert.Equal("OBS configurado, pero esta desactivado o incompleto", EventRulePresentationService.BuildObsToolTip(rule));
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

static class RuleEditorValueTests
{
    public static void ResolvesFallbackNames()
    {
        TestAssert.Equal(
            "Mi alerta",
            RuleEditorValueService.ResolveRuleName("  Mi alerta  ", "Anterior", TwitchEventKind.Follow));
        TestAssert.Equal(
            "Anterior",
            RuleEditorValueService.ResolveRuleName(" ", "  Anterior  ", TwitchEventKind.Follow));
        TestAssert.Equal(
            DisplayNames.For(TwitchEventKind.Follow),
            RuleEditorValueService.ResolveRuleName("", "", TwitchEventKind.Follow));
    }

    public static void ResolvesLegacyAudioPaths()
    {
        var library = new[]
        {
            new AudioAssetConfig { Id = "asset-1", FilePath = @"C:\audios\follow.mp3" }
        };

        TestAssert.Equal(
            @"C:\audios\follow.mp3",
            RuleEditorValueService.ResolveLegacyAudioPath(AudioSourceMode.Single, "ASSET-1", library));
        TestAssert.Equal(
            "",
            RuleEditorValueService.ResolveLegacyAudioPath(AudioSourceMode.Group, "asset-1", library));
        TestAssert.Equal(
            "",
            RuleEditorValueService.ResolveLegacyAudioPath(AudioSourceMode.Single, "missing", library));
    }
}

static class RuleEditorFormTests
{
    public static void AppliesNormalizedValues()
    {
        var rule = new EventRule { Name = "Existente" };
        var library = new[]
        {
            new AudioAssetConfig { Id = "audio-1", FilePath = @"C:\audios\alerta.mp3" }
        };

        RuleEditorFormService.Apply(
            rule,
            new RuleEditorFormValues(
                IsEnabled: true,
                RuleNameText: " ",
                EventKind: TwitchEventKind.Cheer,
                CustomRewardTitle: " reward ",
                ChatCommand: " !rave ",
                MinimumBitsText: "0",
                SendChatMessage: true,
                ChatMessageTemplate: " hola @{user} ",
                SendAlexaEvent: true,
                SendObsScene: true,
                ObsSceneName: " Recortes ",
                ObsSceneDelayText: "-1",
                ObsReturnToPreviousScene: true,
                ObsReturnDelayText: "",
                SendObsMedia: true,
                ObsMediaKind: ObsMediaKind.Video,
                ObsMediaSourceMode: MediaSourceMode.Group,
                ObsMediaAssetId: " media-1 ",
                ObsMediaGroupId: " group-1 ",
                ObsMediaDurationText: "10",
                UseLights: true,
                PlayAudio: true,
                AudioSourceMode: AudioSourceMode.Single,
                AudioAssetId: "AUDIO-1",
                AudioGroupId: " group-a ",
                Pattern: LightPattern.Rave,
                TargetPins: "2, 3",
                PrimaryColor: "00ff00",
                SecondaryColor: "#bad",
                TertiaryColor: "",
                Brightness: 50.6,
                DurationMs: 1234.4,
                CycleMs: 88.8,
                StepMs: 9.2),
            library);

        TestAssert.True(rule.IsEnabled);
        TestAssert.Equal("Existente", rule.Name);
        TestAssert.Equal(TwitchEventKind.Cheer, rule.EventKind);
        TestAssert.Equal(1, rule.MinimumBits);
        TestAssert.Equal("Recortes", rule.ObsSceneName);
        TestAssert.Equal(0, rule.ObsSceneDelayMs);
        TestAssert.Equal(15000, rule.ObsReturnDelayMs);
        TestAssert.Equal(250, rule.ObsMediaDurationMs);
        TestAssert.Equal(@"C:\audios\alerta.mp3", rule.AudioPath);
        TestAssert.Equal("2, 3", rule.TargetPins);
        TestAssert.Equal("#00FF00", rule.PrimaryColor);
        TestAssert.Equal("#FFFFFF", rule.SecondaryColor);
        TestAssert.Equal("#FFFFFF", rule.TertiaryColor);
        TestAssert.Equal(51, rule.Brightness);
        TestAssert.Equal(1234, rule.DurationMs);
        TestAssert.Equal(89, rule.CycleMs);
        TestAssert.Equal(10, rule.StepMs);
    }
}

static class RuleObsMediaChoiceTests
{
    public static void ResolvesImageAndVideoLibraries()
    {
        var images = new[] { new MediaAssetConfig { Id = "image-1" } };
        var videos = new[] { new MediaAssetConfig { Id = "video-1" } };
        var imageGroups = new[] { new MediaGroupConfig { Id = "image-group" } };
        var videoGroups = new[] { new MediaGroupConfig { Id = "video-group" } };

        var imageChoice = RuleObsMediaChoiceService.Resolve(
            ObsMediaKind.Image,
            images,
            videos,
            imageGroups,
            videoGroups);
        var videoChoice = RuleObsMediaChoiceService.Resolve(
            ObsMediaKind.Video,
            images,
            videos,
            imageGroups,
            videoGroups);

        TestAssert.Same(images, imageChoice.Assets);
        TestAssert.Same(imageGroups, imageChoice.Groups);
        TestAssert.True(imageChoice.HasAssets);
        TestAssert.True(imageChoice.HasGroups);
        TestAssert.Same(videos, videoChoice.Assets);
        TestAssert.Same(videoGroups, videoChoice.Groups);
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
        var service = new RuleSimulationService(UiTextService.CreateDefault());
        var cheer = service.BuildEvent(new EventRule
        {
            EventKind = TwitchEventKind.Cheer,
            MinimumBits = 250
        });

        TestAssert.Equal(TwitchEventKind.Cheer, cheer.Kind);
        TestAssert.Equal(250, cheer.Bits);

        var test = service.BuildEvent(new EventRule
        {
            EventKind = TwitchEventKind.Test
        });

        TestAssert.Equal(TwitchEventKind.Follow, test.Kind);
        TestAssert.Contains("Simulacion", test.Title);
    }
}

static class RuleTestValidationTests
{
    public static void BlocksMissingAudio()
    {
        var result = RuleTestValidationService.Validate(
            new EventRule { PlayAudio = true },
            new TwitchEvent { Kind = TwitchEventKind.Follow, UserName = "user" },
            AppConfig.CreateDefault(),
            hasOpenArduinoPort: true,
            hasValidAudio: false);

        TestAssert.False(result.CanRun);
        TestAssert.Equal(1, result.Issues.Count);
        TestAssert.Equal(RuleTestValidationIssueKind.MissingAudio, result.Issues[0].Kind);
    }

    public static void ReportsNonBlockingIssues()
    {
        var config = AppConfig.CreateDefault();
        config.ArduinoEnabled = true;
        config.SerialPort = "COM3";

        var result = RuleTestValidationService.Validate(
            new EventRule
            {
                EventKind = TwitchEventKind.ChatCommand,
                ChatCommand = "!rave",
                UseLights = true,
                TargetPins = "pin-roto",
                SendAlexaEvent = true
            },
            new TwitchEvent { Kind = TwitchEventKind.ChatCommand, UserName = "user", Message = "!baile" },
            config,
            hasOpenArduinoPort: false,
            hasValidAudio: true);

        TestAssert.True(result.CanRun);
        TestAssert.Equal(4, result.Issues.Count);
        TestAssert.Equal(RuleTestValidationIssueKind.ArduinoDisconnected, result.Issues[0].Kind);
        TestAssert.Equal(RuleTestValidationIssueKind.InvalidPins, result.Issues[1].Kind);
        TestAssert.Equal(RuleTestValidationIssueKind.AlexaNotConfigured, result.Issues[2].Kind);
        TestAssert.Equal(RuleTestValidationIssueKind.ChatCommandMismatch, result.Issues[3].Kind);
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

static class TwitchEventSubSubscriptionPlannerTests
{
    public static void BuildsUniqueDefinitions()
    {
        var config = AppConfig.CreateDefault();
        config.Channel.UserId = "broadcaster-1";
        config.Rules =
        [
            new EventRule { IsEnabled = true, EventKind = TwitchEventKind.Follow },
            new EventRule { IsEnabled = true, EventKind = TwitchEventKind.Subscription },
            new EventRule { IsEnabled = true, EventKind = TwitchEventKind.Subscription },
            new EventRule { IsEnabled = true, EventKind = TwitchEventKind.ChatCommand },
            new EventRule { IsEnabled = false, EventKind = TwitchEventKind.Cheer },
            new EventRule { IsEnabled = true, EventKind = TwitchEventKind.Test }
        ];

        var definitions = TwitchEventSubSubscriptionPlanner.BuildDefinitions(config);

        TestAssert.Equal(5, definitions.Count);
        TestAssert.True(definitions.Any(definition => definition.Type == TwitchEventSubProtocol.Events.Follow));
        TestAssert.True(definitions.Any(definition => definition.Type == TwitchEventSubProtocol.Events.Subscribe));
        TestAssert.True(definitions.Any(definition => definition.Type == TwitchEventSubProtocol.Events.SubscriptionMessage));
        TestAssert.True(definitions.Any(definition => definition.Type == TwitchEventSubProtocol.Events.SubscriptionGift));
        TestAssert.True(definitions.Any(definition => definition.Type == TwitchEventSubProtocol.Events.ChatMessage));

        var follow = definitions.First(definition => definition.Type == TwitchEventSubProtocol.Events.Follow);
        TestAssert.Equal(TwitchEventSubProtocol.Versions.V2, follow.Version);
        TestAssert.Equal("broadcaster-1", follow.Condition[TwitchEventSubProtocol.Conditions.BroadcasterUserId]);
        TestAssert.Equal("broadcaster-1", follow.Condition[TwitchEventSubProtocol.Conditions.ModeratorUserId]);

        var chat = definitions.First(definition => definition.Type == TwitchEventSubProtocol.Events.ChatMessage);
        TestAssert.Equal("broadcaster-1", chat.Condition[TwitchEventSubProtocol.Conditions.UserId]);
    }
}

static class TwitchEventSubMessageParserTests
{
    public static void ParsesWelcomeAndEvents()
    {
        var parser = new TwitchEventSubMessageParser(UiTextService.CreateDefault());
        var sessionId = parser.ReadSessionId(
            """
            {
              "metadata": { "message_type": "session_welcome" },
              "payload": { "session": { "id": "session-123" } }
            }
            """);

        TestAssert.Equal("session-123", sessionId);

        using var cheerDoc = System.Text.Json.JsonDocument.Parse(
            """
            {
              "subscription": { "type": "channel.cheer" },
              "event": {
                "user_name": "Dafovii",
                "bits": 100,
                "message": "vamos!"
              }
            }
            """);
        var cheer = parser.ParseEvent(cheerDoc.RootElement)!;

        TestAssert.Equal(TwitchEventKind.Cheer, cheer.Kind);
        TestAssert.Equal("channel.cheer", cheer.RawType);
        TestAssert.Equal("Dafovii", cheer.UserName);
        TestAssert.Equal(100, cheer.Bits);
        TestAssert.Equal("vamos!", cheer.Message);
        TestAssert.Contains("100 bits", cheer.Title);

        using var giftDoc = System.Text.Json.JsonDocument.Parse(
            """
            {
              "subscription": { "type": "channel.subscription.gift" },
              "event": {
                "user_name": "",
                "total": 3
              }
            }
            """);
        var gift = parser.ParseEvent(giftDoc.RootElement)!;

        TestAssert.Equal(TwitchEventKind.Subscription, gift.Kind);
        TestAssert.Equal("Alguien", gift.UserName);
        TestAssert.Equal(3, gift.ViewerCount);
        TestAssert.Contains("regalo 3", gift.Title);
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

static class AlertExecutionPlanTests
{
    public static void DisablesLightsWhenArduinoIsDisabled()
    {
        var config = AppConfig.CreateDefault();
        config.ArduinoEnabled = false;
        var rule = new EventRule
        {
            UseLights = true,
            TargetPins = "6"
        };

        var plan = AlertExecutionPlanService.Build(
            rule,
            config,
            hasOpenArduinoPort: false,
            playbackDuration: TimeSpan.FromSeconds(3),
            obsMediaDuration: TimeSpan.FromSeconds(5));

        TestAssert.False(plan.UseLights);
        TestAssert.False(plan.ShouldReconnectArduino);
        TestAssert.False(plan.ShouldRestoreBackground);
        TestAssert.Equal(0, plan.AllLightTargets.Count);
        TestAssert.Equal(0, plan.RuleLightTargets.Count);
        TestAssert.Same(null, plan.LightCommand);
        TestAssert.Equal(5000, plan.SynchronizedDurationMs);
    }

    public static void ResolvesLightCommandAndReconnectState()
    {
        var config = AppConfig.CreateDefault();
        config.ArduinoEnabled = true;
        config.SerialPort = "COM3";
        config.LedStrips =
        [
            new LedStripConfig { Pin = 2, LedCount = 10 },
            new LedStripConfig { Pin = 4, LedCount = 20 }
        ];
        var rule = new EventRule
        {
            UseLights = true,
            TargetPins = "4",
            Pattern = LightPattern.Rave,
            Brightness = 75,
            DurationMs = 1000
        };

        var plan = AlertExecutionPlanService.Build(
            rule,
            config,
            hasOpenArduinoPort: false,
            playbackDuration: TimeSpan.FromSeconds(2),
            obsMediaDuration: TimeSpan.FromSeconds(4));

        TestAssert.True(plan.UseLights);
        TestAssert.True(plan.ShouldReconnectArduino);
        TestAssert.True(plan.ShouldRestoreBackground);
        TestAssert.Equal(2, plan.AllLightTargets.Count);
        TestAssert.Equal(1, plan.RuleLightTargets.Count);
        TestAssert.Equal(4, plan.RuleLightTargets[0].Pin);
        TestAssert.Equal(4000, plan.SynchronizedDurationMs);
        TestAssert.Equal(4000, plan.LightCommand!.DurationMs);
        TestAssert.Equal(LightPattern.Rave, plan.LightCommand.Pattern);
    }
}

static class ObsRulePlanTests
{
    public static void ResolvesSceneRestore()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rule = new EventRule
        {
            SendObsScene = true,
            ObsSceneName = " Recortes ",
            ObsReturnToPreviousScene = true,
            ObsReturnDelayMs = 1200
        };

        TestAssert.True(ObsRulePlanService.ShouldSendScene(rule, obsConfigured: true));
        TestAssert.False(ObsRulePlanService.ShouldSendScene(rule, obsConfigured: false));
        TestAssert.Equal("Recortes", ObsRulePlanService.ResolveTargetScene(rule));

        var restore = ObsRulePlanService.BuildSceneRestoreRequest(rule, " Gameplay ", "Recortes", startedAt);

        TestAssert.Equal("Gameplay", restore!.PreviousScene);
        TestAssert.Equal("Recortes", restore.TargetScene);
        TestAssert.Equal(1200d, restore.Delay.TotalMilliseconds);
        TestAssert.Equal(startedAt, restore.StartedAt);
        TestAssert.Same(null, ObsRulePlanService.BuildSceneRestoreRequest(rule, "Recortes", "Recortes", startedAt));
    }

    public static void ResolvesMediaPlans()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rule = new EventRule
        {
            SendObsMedia = true,
            SendObsScene = true,
            ObsSceneName = " BRB ",
            ObsMediaKind = ObsMediaKind.Video
        };

        TestAssert.True(ObsRulePlanService.ShouldSendMedia(rule, obsConfigured: true));
        TestAssert.Equal("BRB", ObsRulePlanService.ResolveMediaSceneName(rule, "Actual"));
        TestAssert.Equal(
            "video-source",
            ObsRulePlanService.ResolveAlertSourceName(ObsMediaKind.Video, "image-source", "video-source"));

        var media = ObsRulePlanService.BuildMediaHideRequest(" BRB ", " source ", TimeSpan.FromSeconds(3), startedAt);
        var restore = new ObsSceneRestoreRequest("Gameplay", "BRB", TimeSpan.FromSeconds(1), startedAt.AddSeconds(-1));
        var aligned = ObsRulePlanService.AlignSceneRestoreWithMedia(restore, media);

        TestAssert.Equal("BRB", media.SceneName);
        TestAssert.Equal("source", media.SourceName);
        TestAssert.Equal(media.Duration, aligned!.Delay);
        TestAssert.Equal(media.StartedAt, aligned.StartedAt);
    }

    public static void BuildsMediaExecutionPlan()
    {
        var config = AppConfig.CreateDefault();
        config.Obs.Enabled = true;
        config.Obs.Host = "127.0.0.1";
        config.VideoVolumePercent = 42;
        var asset = new MediaAssetConfig
        {
            Name = "Boom",
            FilePath = @"C:\stream\boom.mp4",
            DurationMs = 2500
        };
        var rule = new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Video,
            SendObsScene = false
        };

        var plan = ObsRulePlanService.BuildMediaExecutionPlan(
            rule,
            config,
            "Gameplay",
            asset,
            "image-source",
            "video-source");

        TestAssert.True(plan.IsReady);
        TestAssert.Equal(ObsRuleMediaPlanStatus.Ready, plan.Status);
        TestAssert.Same(asset, plan.Asset);
        TestAssert.Equal("Gameplay", plan.SceneName);
        TestAssert.Equal("video-source", plan.SourceName);
        TestAssert.Equal(2500d, plan.Duration.TotalMilliseconds);
        TestAssert.Equal(42, plan.VolumePercent);

        var disabledRule = new EventRule
        {
            SendObsMedia = false,
            ObsMediaKind = ObsMediaKind.Video
        };
        var disabled = ObsRulePlanService.BuildMediaExecutionPlan(
            disabledRule,
            config,
            "Gameplay",
            asset,
            "image-source",
            "video-source");

        TestAssert.Equal(ObsRuleMediaPlanStatus.Disabled, disabled.Status);

        var missingScene = ObsRulePlanService.BuildMediaExecutionPlan(
            rule,
            config,
            "",
            asset,
            "image-source",
            "video-source");

        TestAssert.Equal(ObsRuleMediaPlanStatus.MissingScene, missingScene.Status);
    }
}

static class ObsWebSocketRequestFactoryTests
{
    public static void BuildsProtocolRequests()
    {
        var config = new ObsIntegrationConfig
        {
            Host = "127.0.0.1",
            Port = 4455,
            OverlayWidth = 1920,
            OverlayHeight = 1080,
            OverlayMediaWidth = 400,
            OverlayMediaHeight = 300,
            OverlayPositionMode = "Custom",
            OverlayX = 2000,
            OverlayY = 100
        };

        TestAssert.Equal("ws://127.0.0.1:4455/", ObsWebSocketRequestFactory.BuildUri(config).ToString());
        config.Host = "wss://obs.example.test/socket";
        TestAssert.Equal("wss://obs.example.test/socket", ObsWebSocketRequestFactory.BuildUri(config).ToString());
        TestAssert.Equal(
            "EabUNw4z9EKKpEOC0yvqBO8dJPSIcTb82eo+adWKOvk=",
            ObsWebSocketRequestFactory.BuildAuthentication("pass", "salt", "challenge"));

        var imageSettings = ObsWebSocketRequestFactory.BuildMediaInputSettings(ObsMediaKind.Image, @"C:\stream\alert.png");
        TestAssert.Equal(@"C:\stream\alert.png", imageSettings[ObsWebSocketProtocol.ImageFile]);
        TestAssert.False(imageSettings.ContainsKey(ObsWebSocketProtocol.LocalFile));

        var videoSettings = ObsWebSocketRequestFactory.BuildMediaInputSettings(ObsMediaKind.Video, @"C:\stream\alert.mp4");
        TestAssert.Equal(true, videoSettings[ObsWebSocketProtocol.IsLocalFile]);
        TestAssert.Equal(@"C:\stream\alert.mp4", videoSettings[ObsWebSocketProtocol.LocalFile]);
        TestAssert.Equal(false, videoSettings[ObsWebSocketProtocol.Looping]);
        TestAssert.Equal(true, videoSettings[ObsWebSocketProtocol.RestartOnActivate]);

        var transformRequest = ObsWebSocketRequestFactory.BuildSceneItemTransformRequest(" Gameplay ", 22, config);
        TestAssert.Equal("Gameplay", transformRequest[ObsWebSocketProtocol.SceneName]);
        TestAssert.Equal(22, transformRequest[ObsWebSocketProtocol.SceneItemId]);
        var transform = (Dictionary<string, object?>)transformRequest[ObsWebSocketProtocol.SceneItemTransform]!;
        TestAssert.Equal(1520, transform[ObsWebSocketProtocol.PositionX]);
        TestAssert.Equal(100, transform[ObsWebSocketProtocol.PositionY]);
        TestAssert.Equal(400, transform[ObsWebSocketProtocol.BoundsWidth]);
        TestAssert.Equal(300, transform[ObsWebSocketProtocol.BoundsHeight]);

        var volumeRequest = ObsWebSocketRequestFactory.BuildInputVolumeRequest(" Video ", 125);
        TestAssert.Equal("Video", volumeRequest[ObsWebSocketProtocol.InputName]);
        TestAssert.Equal(1d, volumeRequest[ObsWebSocketProtocol.InputVolumeMul]);
    }
}

static class ObsWebSocketResponseReaderTests
{
    public static void ParsesProtocolResponses()
    {
        using var hello = System.Text.Json.JsonDocument.Parse(
            """
            {
              "op": 0,
              "d": {
                "rpcVersion": 1,
                "authentication": {
                  "salt": "salt-value",
                  "challenge": "challenge-value"
                }
              }
            }
            """);

        TestAssert.Equal(ObsWebSocketProtocol.OpHello, ObsWebSocketResponseReader.ReadOperation(hello));
        TestAssert.Equal(1, ObsWebSocketResponseReader.ReadRpcVersion(hello));
        TestAssert.True(ObsWebSocketResponseReader.TryReadAuthentication(hello, out var salt, out var challenge));
        TestAssert.Equal("salt-value", salt);
        TestAssert.Equal("challenge-value", challenge);

        using var version = System.Text.Json.JsonDocument.Parse(
            """
            {
              "d": {
                "responseData": {
                  "obsVersion": "30.1.2"
                }
              }
            }
            """);
        TestAssert.Equal("30.1.2", ObsWebSocketResponseReader.ReadVersion(version));

        using var scenes = System.Text.Json.JsonDocument.Parse(
            """
            {
              "d": {
                "responseData": {
                  "currentProgramSceneName": "Gameplay",
                  "scenes": [
                    { "sceneName": "Gameplay" },
                    { "sceneName": "" },
                    { "sceneName": "BRB" }
                  ]
                }
              }
            }
            """);
        using var studioMode = System.Text.Json.JsonDocument.Parse(
            """
            {
              "d": {
                "responseData": {
                  "studioModeEnabled": true
                }
              }
            }
            """);
        var snapshot = ObsWebSocketResponseReader.ReadSceneSnapshot(scenes, studioMode);

        TestAssert.Equal("Gameplay", snapshot.CurrentScene);
        TestAssert.True(snapshot.StudioMode);
        TestAssert.Equal(2, snapshot.Scenes.Count);
        TestAssert.Equal("BRB", snapshot.Scenes[1].Name);

        using var sceneItem = System.Text.Json.JsonDocument.Parse(
            """
            {
              "d": {
                "responseData": {
                  "sceneItemId": 42
                }
              }
            }
            """);
        TestAssert.Equal(42, ObsWebSocketResponseReader.ReadSceneItemId(sceneItem));

        using var request = System.Text.Json.JsonDocument.Parse(
            """
            {
              "op": 7,
              "d": {
                "requestId": "request-1",
                "requestStatus": {
                  "result": false,
                  "code": 601,
                  "comment": "Input already exists"
                }
              }
            }
            """);
        var status = ObsWebSocketResponseReader.ReadRequestStatus(request);

        TestAssert.Equal(ObsWebSocketProtocol.OpRequestResponse, ObsWebSocketResponseReader.ReadOperation(request));
        TestAssert.Equal("request-1", ObsWebSocketResponseReader.ReadRequestId(request));
        TestAssert.False(status.Succeeded);
        TestAssert.Equal(601, status.Code);
        TestAssert.Equal("Input already exists", status.Comment);
    }
}

static class LightControlInputTests
{
    public static void ResolvesPresets()
    {
        var normal = LightControlInputService.GetRulePreset("");
        var fast = LightControlInputService.GetRulePreset("Fast");
        var backgroundSoft = LightControlInputService.GetBackgroundPreset("Soft");
        var hasRuleRange = LightControlInputService.TryGetRuleRange("Duration", out var ruleRange);
        var hasBackgroundRange = LightControlInputService.TryGetBackgroundRange("Step", out var backgroundRange);
        var unknownRange = LightControlInputService.TryGetRuleRange("Nada", out _);

        TestAssert.Equal(180d, normal.Brightness);
        TestAssert.Equal(2200d, fast.DurationMs);
        TestAssert.Equal(110d, backgroundSoft.Brightness);
        TestAssert.Equal(260d, backgroundSoft.StepMs);
        TestAssert.True(hasRuleRange);
        TestAssert.Equal(250d, ruleRange.Minimum);
        TestAssert.Equal(60000d, ruleRange.Maximum);
        TestAssert.True(hasBackgroundRange);
        TestAssert.Equal(10d, backgroundRange.Minimum);
        TestAssert.Equal(5000d, backgroundRange.Maximum);
        TestAssert.False(unknownRange);
    }

    public static void ParsesAndClampsValues()
    {
        TestAssert.True(LightControlInputService.TryParseDelta("Brightness:-10", out var delta));
        TestAssert.Equal("Brightness", delta.Target);
        TestAssert.Equal(-10d, delta.Amount);

        TestAssert.False(LightControlInputService.TryParseDelta("Brightness", out _));
        TestAssert.Equal(100d, LightControlInputService.AdjustValue(95, 20, 0, 100));
        TestAssert.Equal(0d, LightControlInputService.AdjustValue(5, -20, 0, 100));

        TestAssert.True(LightControlInputService.TryParseSliderText(" 250 ", 0, 200, out var sliderValue));
        TestAssert.Equal(200d, sliderValue);
        TestAssert.False(LightControlInputService.TryParseSliderText("abc", 0, 200, out _));
    }
}

static class BackgroundLightRestoreTests
{
    public static void ResolvesRetryAttempts()
    {
        TestAssert.Equal(2, BackgroundLightRestoreService.ResolveArduinoRestoreAttempts(
            arduinoEnabled: true,
            backgroundEnabled: true,
            retryArduino: true));

        TestAssert.Equal(1, BackgroundLightRestoreService.ResolveArduinoRestoreAttempts(
            arduinoEnabled: true,
            backgroundEnabled: true,
            retryArduino: false));

        TestAssert.Equal(1, BackgroundLightRestoreService.ResolveArduinoRestoreAttempts(
            arduinoEnabled: true,
            backgroundEnabled: false,
            retryArduino: true));

        TestAssert.Equal(1, BackgroundLightRestoreService.ResolveArduinoRestoreAttempts(
            arduinoEnabled: false,
            backgroundEnabled: true,
            retryArduino: true));
    }

    public static void ResolvesApplyPlan()
    {
        var config = AppConfig.CreateDefault();
        var emptyPlan = BackgroundLightRestoreService.ResolveApplyPlan(config);

        TestAssert.False(emptyPlan.HasAnyAction);
        TestAssert.Equal(BackgroundArduinoAction.None, emptyPlan.ArduinoAction);
        TestAssert.Equal(BackgroundAlexaAction.None, emptyPlan.AlexaAction);

        config.ArduinoEnabled = true;
        config.BackgroundEnabled = true;
        config.BackgroundAlexaEnabled = true;

        var plan = BackgroundLightRestoreService.ResolveApplyPlan(config);

        TestAssert.True(plan.HasAnyAction);
        TestAssert.Equal(BackgroundArduinoAction.ApplyBackground, plan.ArduinoAction);
        TestAssert.Equal(BackgroundAlexaAction.SendOn, plan.AlexaAction);
    }

    public static void ResolvesRestorePlan()
    {
        var config = AppConfig.CreateDefault();
        config.ArduinoEnabled = true;
        config.BackgroundEnabled = true;
        config.BackgroundAlexaEnabled = true;

        var plan = BackgroundLightRestoreService.ResolveRestorePlan(config, retryArduino: true);

        TestAssert.Equal(2, plan.ArduinoAttempts);
        TestAssert.Equal(BackgroundArduinoAction.ApplyBackground, plan.ArduinoAction);
        TestAssert.Equal(BackgroundAlexaAction.SendOn, plan.AlexaAction);

        config.BackgroundEnabled = false;
        config.BackgroundAlexaTurnOffAfterEvent = true;

        plan = BackgroundLightRestoreService.ResolveRestorePlan(config, retryArduino: true);

        TestAssert.Equal(1, plan.ArduinoAttempts);
        TestAssert.Equal(BackgroundArduinoAction.StopLights, plan.ArduinoAction);
        TestAssert.Equal(BackgroundAlexaAction.SendOff, plan.AlexaAction);
    }
}

static class RulePinChoiceTests
{
    public static void BuildsPinChoices()
    {
        var choices = RulePinChoiceService.BuildChoices(
        [
            new LedStripConfig { Name = "Derecha", Pin = 7 },
            new LedStripConfig { Name = "", Pin = 3 }
        ], "7, 9");

        TestAssert.Equal("7, 9", choices.CurrentPins);
        TestAssert.Equal(4, choices.Options.Count);
        TestAssert.Equal("Todas las salidas", choices.Options[0].Label);
        TestAssert.Equal("Pin 3", choices.Options[1].Label);
        TestAssert.Equal("Derecha - Pin 7", choices.Options[2].Label);
        TestAssert.Equal("Personalizado (7, 9)", choices.Options[3].Label);
    }
}

static class SerialPortNameTests
{
    public static void CleansFriendlyPortNames()
    {
        TestAssert.Equal("COM3", SerialPortNameService.TryExtractPortName("Arduino Uno (COM3)"));
        TestAssert.Equal("COM12", SerialPortNameService.TryExtractPortName("USB-SERIAL CH340 (COM12)"));
        TestAssert.Equal<string?>(null, SerialPortNameService.TryExtractPortName("Dispositivo sin puerto"));

        TestAssert.Equal("Arduino Uno", SerialPortNameService.CleanFriendlyName("Arduino Uno (COM3)", "COM3"));
        TestAssert.Equal("USB Serial Device", SerialPortNameService.CleanFriendlyName("USB\\VID_2341;USB Serial Device (COM7)", "COM7"));
        TestAssert.Equal("COM9", SerialPortNameService.CleanFriendlyName(null, "COM9"));
    }
}

static class SerialLightProtocolTests
{
    public static void ResolvesCommands()
    {
        TestAssert.Equal(SerialLightProtocol.FxCommand, SerialLightProtocol.ResolveCommandName("FX|6:30|SOLID"));
        TestAssert.Equal(SerialLightProtocol.StopCommand, SerialLightProtocol.ResolveCommandName("STOP|6:30"));
        TestAssert.Equal<string?>(null, SerialLightProtocol.ResolveCommandName("PING"));
    }

    public static void DetectsResponses()
    {
        TestAssert.True(SerialLightProtocol.IsAckFor("ACK|FX", SerialLightProtocol.FxCommand));
        TestAssert.True(SerialLightProtocol.IsAckFor("ack|stop", SerialLightProtocol.StopCommand));
        TestAssert.False(SerialLightProtocol.IsAckFor("ACK|STOP", SerialLightProtocol.FxCommand));
        TestAssert.True(SerialLightProtocol.IsError("ERR|PIN"));
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

static class AudioLibraryMutationTests
{
    public static void RemovesAssetsAndCleansRules()
    {
        var config = AppConfig.CreateDefault();
        config.AudioLibrary.Clear();
        config.Rules.Clear();
        config.AudioLibrary.Add(new AudioAssetConfig { Id = "a1", FilePath = @"C:\audio\follow.mp3" });
        config.AudioLibrary.Add(new AudioAssetConfig { Id = "a2", FilePath = @"C:\audio\raid.mp3" });
        config.Rules.Add(new EventRule
        {
            PlayAudio = true,
            AudioSourceMode = AudioSourceMode.Single,
            AudioAssetId = "a1",
            AudioPath = @"C:\legacy\follow.mp3"
        });
        config.Rules.Add(new EventRule
        {
            PlayAudio = true,
            AudioSourceMode = AudioSourceMode.Group,
            AudioAssetId = "a1",
            AudioGroupId = "g1"
        });

        var result = AudioLibraryMutationService.RemoveAudioAsset(config, "A1");

        TestAssert.True(result.Removed);
        TestAssert.Equal(1, config.AudioLibrary.Count);
        TestAssert.Equal("a2", config.AudioLibrary[0].Id);
        TestAssert.Equal(2, result.UpdatedRuleCount);
        TestAssert.Equal("", config.Rules[0].AudioAssetId);
        TestAssert.Equal("", config.Rules[0].AudioPath);
        TestAssert.False(config.Rules[0].PlayAudio);
        TestAssert.Equal("", config.Rules[1].AudioAssetId);
        TestAssert.True(config.Rules[1].PlayAudio);
    }
}

static class MediaLibraryMutationTests
{
    public static void RemovesAssetsAndCleansRules()
    {
        var config = AppConfig.CreateDefault();
        config.ImageLibrary.Clear();
        config.VideoLibrary.Clear();
        config.Rules.Clear();
        config.ImageLibrary.Add(new MediaAssetConfig { Id = "img1", FilePath = @"C:\media\follow.png" });
        config.ImageLibrary.Add(new MediaAssetConfig { Id = "img2", FilePath = @"C:\media\raid.png" });
        config.Rules.Add(new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Image,
            ObsMediaSourceMode = MediaSourceMode.Single,
            ObsMediaAssetId = "img1"
        });
        config.Rules.Add(new EventRule
        {
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Image,
            ObsMediaSourceMode = MediaSourceMode.Group,
            ObsMediaAssetId = "img1",
            ObsMediaGroupId = "g1"
        });

        var result = MediaLibraryMutationService.RemoveMediaAsset(config, MediaLibraryKind.Image, "IMG1");

        TestAssert.True(result.Removed);
        TestAssert.Equal(1, config.ImageLibrary.Count);
        TestAssert.Equal("img2", config.ImageLibrary[0].Id);
        TestAssert.Equal(1, result.UpdatedRuleCount);
        TestAssert.False(config.Rules[0].SendObsMedia);
        TestAssert.Equal("", config.Rules[0].ObsMediaAssetId);
        TestAssert.True(config.Rules[1].SendObsMedia);
        TestAssert.Equal("img1", config.Rules[1].ObsMediaAssetId);
    }
}

static class LibraryAssetUsageTests
{
    public static void MarksAssetUsage()
    {
        var usedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var audio = new AudioAssetConfig();
        var media = new MediaAssetConfig();

        LibraryAssetUsageService.MarkAudioUsed(audio, TimeSpan.FromMilliseconds(1234.6), usedAt);
        LibraryAssetUsageService.MarkMediaUsed(media, usedAt);

        TestAssert.Equal(1235, audio.DurationMs);
        TestAssert.Equal(usedAt, audio.LastUsedAt);
        TestAssert.Equal(usedAt, media.LastUsedAt);
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

static class LibraryGroupServiceTests
{
    public static void CreatesAndReusesGroups()
    {
        var groups = new List<AudioGroupConfig>();

        var created = LibraryGroupService.GetOrCreate<AudioGroupConfig>(groups, " Reacciones ");
        var reused = LibraryGroupService.GetOrCreate<AudioGroupConfig>(groups, "reacciones");
        var invalid = LibraryGroupService.GetOrCreate<AudioGroupConfig>(groups, " ");

        TestAssert.True(created.IsValid);
        TestAssert.True(created.Created);
        TestAssert.Equal("Reacciones", created.Group?.Name);
        TestAssert.False(reused.Created);
        TestAssert.Same(created.Group, reused.Group);
        TestAssert.False(invalid.IsValid);
        TestAssert.Equal(1, groups.Count);
    }

    public static void ClearsGroupReferences()
    {
        var assets = new List<AudioAssetConfig>
        {
            new() { GroupId = "g1" },
            new() { GroupId = "g1" },
            new() { GroupId = "g2" }
        };
        var rules = new List<EventRule>
        {
            new() { AudioSourceMode = AudioSourceMode.Group, AudioGroupId = "g1", PlayAudio = true },
            new() { AudioSourceMode = AudioSourceMode.Single, AudioGroupId = "g1", PlayAudio = true },
            new() { AudioSourceMode = AudioSourceMode.Group, AudioGroupId = "g2", PlayAudio = true },
            new() { ObsMediaKind = ObsMediaKind.Image, ObsMediaSourceMode = MediaSourceMode.Group, ObsMediaGroupId = "g1", SendObsMedia = true },
            new() { ObsMediaKind = ObsMediaKind.Video, ObsMediaSourceMode = MediaSourceMode.Group, ObsMediaGroupId = "g1", SendObsMedia = true },
            new() { ObsMediaKind = ObsMediaKind.Image, ObsMediaSourceMode = MediaSourceMode.Single, ObsMediaGroupId = "g1", SendObsMedia = true }
        };

        var counted = LibraryGroupService.CountAssetsInGroup(assets, "g1");
        var clearedAssets = LibraryGroupService.ClearGroupFromAssets(assets, "g1");
        var clearedRules = LibraryGroupService.ClearAudioGroupFromRules(rules, "g1");
        var clearedMediaRules = LibraryGroupService.ClearMediaGroupFromRules(rules, ObsMediaKind.Image, "g1");

        TestAssert.Equal(2, counted);
        TestAssert.Equal(2, clearedAssets);
        TestAssert.Equal("", assets[0].GroupId);
        TestAssert.Equal("", assets[1].GroupId);
        TestAssert.Equal("g2", assets[2].GroupId);
        TestAssert.Equal(1, clearedRules);
        TestAssert.Equal("", rules[0].AudioGroupId);
        TestAssert.False(rules[0].PlayAudio);
        TestAssert.Equal("g1", rules[1].AudioGroupId);
        TestAssert.True(rules[1].PlayAudio);
        TestAssert.Equal(1, clearedMediaRules);
        TestAssert.Equal("", rules[3].ObsMediaGroupId);
        TestAssert.False(rules[3].SendObsMedia);
        TestAssert.Equal("g1", rules[4].ObsMediaGroupId);
        TestAssert.True(rules[4].SendObsMedia);
        TestAssert.Equal("g1", rules[5].ObsMediaGroupId);
        TestAssert.True(rules[5].SendObsMedia);
    }
}

static class LibraryGroupRowFactoryTests
{
    public static void BuildsAudioAndMediaGroups()
    {
        var audioGroups = new[]
        {
            new AudioGroupConfig { Id = "g1", Name = "Seguidores" },
            new AudioGroupConfig { Id = "g2", Name = "Raid" }
        };
        var audioRows = LibraryGroupRowFactoryService.CreateAudioGroupRows(
            audioGroups,
            new[]
            {
                new AudioAssetConfig { GroupId = "g1" },
                new AudioAssetConfig { GroupId = "G1" },
                new AudioAssetConfig { GroupId = "g2" }
            },
            count => $"{count} audio{(count == 1 ? "" : "s")}");

        TestAssert.Equal(2, audioRows.Count);
        TestAssert.Equal("2 audios", audioRows[0].CountText);
        TestAssert.Equal("1 audio", audioRows[1].CountText);

        var mediaRows = LibraryGroupRowFactoryService.CreateMediaGroupRows(
            new[] { new MediaGroupConfig { Id = "m1", Name = "Memes" } },
            new[] { new MediaAssetConfig { GroupId = "m1" }, new MediaAssetConfig { GroupId = "" } },
            count => $"{count} archivo{(count == 1 ? "" : "s")}");

        TestAssert.Equal(1, mediaRows.Count);
        TestAssert.Equal("1 archivo", mediaRows[0].CountText);
    }
}

static class LibrarySummaryTests
{
    public static void FormatsCountsAndLastUsage()
    {
        var assets = new[]
        {
            new AudioAssetConfig
            {
                Name = "Antiguo",
                GroupId = "g1",
                LastUsedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new AudioAssetConfig
            {
                Name = "Reciente",
                GroupId = "g1",
                LastUsedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
            },
            new AudioAssetConfig
            {
                Name = "Sin uso",
                GroupId = ""
            }
        };
        var groups = new[] { new AudioGroupConfig { Id = "g1", Name = "Seguidores" } };

        var summary = LibrarySummaryService.Create(
            assets,
            groups,
            visibleCount: 2,
            groupFilterId: "g1",
            new Dictionary<string, string> { ["g1"] = "Seguidores" },
            "audios",
            new LibrarySummaryLabels(
                "Mostrando {0} de {1} {2}{3}",
                " del grupo {0}",
                "Sin uso",
                "seleccionado"));

        TestAssert.Equal("3", summary.AssetCountText);
        TestAssert.Equal("1", summary.GroupCountText);
        TestAssert.Equal("Reciente", summary.LastAssetText);
        TestAssert.Equal("Mostrando 2 de 3 audios del grupo Seguidores", summary.FooterText);
    }
}

static class LibraryRowFactoryTests
{
    public static void BuildsAudioRows()
    {
        var audio = new AudioAssetConfig
        {
            Id = "audio-1",
            Name = "Follow",
            GroupId = "group-1",
            FilePath = @"C:\audios\follow.mp3",
            DurationMs = 3000
        };
        var rules = new[]
        {
            new EventRule
            {
                Name = "Nuevo seguidor",
                EventKind = TwitchEventKind.Follow,
                PlayAudio = true,
                AudioSourceMode = AudioSourceMode.Single,
                AudioAssetId = "AUDIO-1"
            }
        };

        var row = LibraryRowFactoryService.CreateAudioRow(
            audio,
            rules,
            new Dictionary<string, string> { ["group-1"] = "Seguidores" },
            "Sin grupo",
            "audio-1",
            isAudioPreviewActive: true,
            index: 2);

        TestAssert.Equal("Nuevo seguidor", row.AssignedAlertText);
        TestAssert.Equal("Seguidores", row.GroupName);
        TestAssert.True(row.HasAssignedAlert);
        TestAssert.True(row.IsPreviewing);
        TestAssert.Equal(2, row.Index);
    }

    public static void BuildsMediaRows()
    {
        var video = new MediaAssetConfig
        {
            Id = "video-1",
            Name = "Intro",
            GroupId = "group-1",
            DurationMs = 4500
        };

        var row = LibraryRowFactoryService.CreateMediaRow(
            MediaLibraryKind.Video,
            video,
            new Dictionary<string, string> { ["group-1"] = "Videos" },
            "Sin grupo",
            index: 3,
            canPreview: true,
            previewingMediaKind: MediaLibraryKind.Video,
            previewingMediaId: "VIDEO-1");

        TestAssert.Equal("Videos", row.GroupName);
        TestAssert.Contains("00:04", row.MetadataText);
        TestAssert.True(row.CanPreview);
        TestAssert.True(row.IsPreviewing);
        TestAssert.Equal(3, row.Index);
    }
}

static class LibraryScreenViewModelTests
{
    public static void UpdatesRowsAndSummary()
    {
        var viewModel = new LibraryScreenViewModel<string, string>();

        viewModel.ReplaceAssetRows(["uno", "dos"]);
        viewModel.ReplaceGroupRows(["grupo"]);
        viewModel.UpdateSummary(new LibrarySummaryDisplay("2", "1", "uno", "Mostrando 2"));

        TestAssert.Equal(2, viewModel.AssetRows.Count);
        TestAssert.Equal(1, viewModel.GroupRows.Count);
        TestAssert.Equal("2", viewModel.AssetCountText);
        TestAssert.Equal("1", viewModel.GroupCountText);
        TestAssert.Equal("uno", viewModel.LastAssetText);
        TestAssert.Equal("Mostrando 2", viewModel.FooterText);

        var filterChanges = 0;
        viewModel.FiltersChanged += (_, _) => filterChanges++;
        viewModel.SearchText = "raid";
        viewModel.SelectFilterCommand.Execute("WITH_GROUP");

        TestAssert.Equal("raid", viewModel.SearchText);
        TestAssert.Equal("WITH_GROUP", viewModel.Filter);
        TestAssert.Equal(2, filterChanges);

        viewModel.SetFilters("", "ALL", notify: false);

        TestAssert.Equal("", viewModel.SearchText);
        TestAssert.Equal("ALL", viewModel.Filter);
        TestAssert.Equal(2, filterChanges);

        viewModel.SetGroupFilter("grupo-activo");
        viewModel.SetFilters("", "ALL", notify: false, clearGroupFilter: false);

        TestAssert.Equal("grupo-activo", viewModel.GroupFilterId);

        viewModel.SetFilters("bits", "NO_GROUP");

        TestAssert.Equal("", viewModel.GroupFilterId);
        TestAssert.Equal("bits", viewModel.SearchText);
        TestAssert.Equal("NO_GROUP", viewModel.Filter);
        TestAssert.Equal(3, filterChanges);

        var volumeChanges = new List<int>();
        viewModel.ConfigureVolume(volumeChanges.Add);
        viewModel.SetVolume(42.6, notify: false);

        TestAssert.Equal(43, (int)Math.Round(viewModel.VolumePercent));
        TestAssert.Equal("43%", viewModel.VolumeText);
        TestAssert.Equal(0, volumeChanges.Count);

        viewModel.VolumePercent = 150;

        TestAssert.Equal(100, (int)Math.Round(viewModel.VolumePercent));
        TestAssert.Equal("100%", viewModel.VolumeText);
        TestAssert.Equal("100", string.Join(",", volumeChanges));

        viewModel.SetNewAssetPath("C:/alertas/follow.mp3", "follow");

        TestAssert.Equal("C:/alertas/follow.mp3", viewModel.NewAssetPath);
        TestAssert.Equal("follow", viewModel.NewAssetName);

        viewModel.NewAssetName = "mi follow";
        viewModel.SetNewAssetPath("C:/alertas/otro.mp3", "otro");
        viewModel.NewAssetAlertId = "rule-1";
        viewModel.SelectNewAssetGroup("group-1");

        TestAssert.Equal("C:/alertas/otro.mp3", viewModel.NewAssetPath);
        TestAssert.Equal("mi follow", viewModel.NewAssetName);
        TestAssert.Equal("group-1", viewModel.NewAssetGroupId);
        TestAssert.Equal("", viewModel.NewGroupName);

        var groupChoices = new[] { "grupo-a" };
        var alertChoices = new[] { "alerta-a" };
        viewModel.SetNewAssetChoices(groupChoices, alertChoices);

        TestAssert.Same(groupChoices, viewModel.NewAssetGroupChoices);
        TestAssert.Same(alertChoices, viewModel.NewAssetAlertChoices);

        viewModel.ClearNewAssetForm();

        TestAssert.Equal("", viewModel.NewAssetPath);
        TestAssert.Equal("", viewModel.NewAssetName);
        TestAssert.Equal("", viewModel.NewAssetAlertId);
        TestAssert.Equal("", viewModel.NewAssetGroupId);

        var actions = new List<string>();
        viewModel.ConfigureActions(
            () => actions.Add("browse"),
            () => actions.Add("save"),
            () => actions.Add("add-group"),
            parameter => actions.Add($"view:{parameter}"),
            parameter => actions.Add($"delete-group:{parameter}"),
            parameter => actions.Add($"preview:{parameter}"),
            parameter => actions.Add($"delete:{parameter}"));

        viewModel.BrowseAssetCommand.Execute(null);
        viewModel.SaveAssetCommand.Execute(null);
        viewModel.AddGroupCommand.Execute(null);
        viewModel.ViewGroupCommand.Execute("g1");
        viewModel.DeleteGroupCommand.Execute("g1");
        viewModel.PreviewAssetCommand.Execute("a1");
        viewModel.DeleteAssetCommand.Execute("a1");

        TestAssert.Equal("browse,save,add-group,view:g1,delete-group:g1,preview:a1,delete:a1", string.Join(",", actions));
    }
}

static class SettingsViewModelTests
{
    public static void ExecutesConfiguredActions()
    {
        var viewModel = new SettingsViewModel();
        var actions = new List<string>();

        viewModel.ConfigureActions(
            () => actions.Add("import"),
            () => actions.Add("export"),
            () => actions.Add("backup"),
            () => actions.Add("restore"),
            () => actions.Add("diagnostics"),
            () => actions.Add("save"));
        viewModel.ConfigureEditorActions(parameter => actions.Add($"close:{parameter}"));

        viewModel.ImportSettingsCommand.Execute(null);
        viewModel.ExportSettingsCommand.Execute(null);
        viewModel.CreateBackupCommand.Execute(null);
        viewModel.RestoreBackupCommand.Execute(null);
        viewModel.RunDiagnosticsCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);
        viewModel.SelectCloseBehaviorCommand.Execute("Tray");

        TestAssert.Equal("import,export,backup,restore,diagnostics,save,close:Tray", string.Join(",", actions));

        viewModel.UpdateMetadata("settings.json", "backups", "V2.2.3");
        viewModel.UpdateBackupPathText("backup manual");
        viewModel.UpdateAppState("Estado: aviso", System.Windows.Media.Brushes.Orange, "/icon.png");
        var themeChoices = new[] { "System" };
        viewModel.UpdateThemeModeChoices(themeChoices);
        var config = AppConfig.CreateDefault();
        config.StartHidden = true;
        config.StartWithWindows = true;
        config.ThemeMode = "Dark";
        config.CloseToTray = false;
        config.AutoConnectTwitch = true;
        config.AutoConnectArduino = true;
        config.Obs.AutoReconnect = true;
        config.MaxQueuedSameRuleAlerts = 2;
        config.SameRuleQueueCooldownMs = 1500;
        config.MaxQueuedDifferentRuleAlerts = 5;
        config.DifferentRuleQueueCooldownMs = 2500;
        viewModel.LoadPreferences(config);

        TestAssert.Equal("settings.json", viewModel.SettingsPathText);
        TestAssert.Equal("backup manual", viewModel.BackupPathText);
        TestAssert.Equal("V2.2.3", viewModel.VersionText);
        TestAssert.Equal("Estado: aviso", viewModel.DiagnosticStatusText);
        TestAssert.Equal("/icon.png", viewModel.AppStateIconPath);
        TestAssert.Same(themeChoices, viewModel.ThemeModeChoices);
        TestAssert.True(viewModel.StartHidden);
        TestAssert.True(viewModel.StartWithWindows);
        TestAssert.Equal("Dark", viewModel.ThemeMode);
        TestAssert.False(viewModel.CloseToTray);
        TestAssert.True(viewModel.AutoConnectTwitch);
        TestAssert.True(viewModel.AutoConnectArduino);
        TestAssert.True(viewModel.ObsAutoReconnect);
        TestAssert.Equal("2", viewModel.MaxQueuedSameRuleAlertsText);
        TestAssert.Equal("1500", viewModel.SameRuleQueueCooldownText);
        TestAssert.Equal("5", viewModel.MaxQueuedDifferentRuleAlertsText);
        TestAssert.Equal("2500", viewModel.DifferentRuleQueueCooldownText);
    }
}

static class MediaLibraryKindCatalogTests
{
    public static void MapsMediaMetadata()
    {
        var image = MediaLibraryKindCatalog.Get(MediaLibraryKind.Image);
        var video = MediaLibraryKindCatalog.Get(MediaLibraryKind.Video);

        TestAssert.Equal(UiTextKeys.ImagesTitle, image.TitleKey);
        TestAssert.Equal(UiTextKeys.ImagesFileDialogFilter, image.FileDialogFilterKey);
        TestAssert.Equal(UiTextKeys.ImagesFooterNoun, image.FooterNounKey);
        TestAssert.Equal("#37C7F3", image.AccentColor);
        TestAssert.Contains("media_image.png", image.IconPath);
        TestAssert.Equal(ObsMediaKind.Image, image.ObsKind);

        TestAssert.Equal(UiTextKeys.VideosTitle, video.TitleKey);
        TestAssert.Equal(UiTextKeys.VideosFileDialogFilter, video.FileDialogFilterKey);
        TestAssert.Equal(UiTextKeys.VideosFooterNoun, video.FooterNounKey);
        TestAssert.Equal("#B56CFF", video.AccentColor);
        TestAssert.Contains("media_video.png", video.IconPath);
        TestAssert.Equal(ObsMediaKind.Video, video.ObsKind);
    }
}

static class MediaPreviewPlanTests
{
    public static void BuildsPreviewPlans()
    {
        var video = new MediaAssetConfig { DurationMs = 2500 };
        var videoPlan = MediaPreviewPlanService.Build(MediaLibraryKind.Video, video, " Gameplay ", 42);

        TestAssert.Equal("Gameplay", videoPlan!.SceneName);
        TestAssert.Equal(ObsMediaKind.Video, videoPlan.ObsKind);
        TestAssert.Equal(2500d, videoPlan.Duration.TotalMilliseconds);
        TestAssert.Equal<int?>(42, videoPlan.VolumePercent);

        var imagePlan = MediaPreviewPlanService.Build(MediaLibraryKind.Image, new MediaAssetConfig(), "Escena", 80);

        TestAssert.Equal(ObsMediaKind.Image, imagePlan!.ObsKind);
        TestAssert.Equal(TimeSpan.FromSeconds(5), imagePlan.Duration);
        TestAssert.Equal<int?>(null, imagePlan.VolumePercent);
        TestAssert.Same(null, MediaPreviewPlanService.Build(MediaLibraryKind.Image, new MediaAssetConfig(), "", 80));
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
        var labels = new ConnectionStateLabels("Listo", "Fuera", "Apagado", "Entrando", "Revisar");
        var appLabels = new AppStateLabels("Todo listo", "Hay puntos por revisar", "Hay errores");
        var connected = ConnectionStateService.GetVisual(ConnectionVisualState.Connected, labels);
        var disabled = ConnectionStateService.GetVisual(ConnectionVisualState.Disabled, labels);

        TestAssert.Equal("Listo", connected.Text);
        TestAssert.Equal("#22C55E", connected.Color);
        TestAssert.Contains("status_ok.png", connected.IconPath);
        TestAssert.Equal("Apagado", disabled.Text);
        TestAssert.Contains("status_empty.png", disabled.IconPath);

        var appWarning = ConnectionStateService.GetAppStateVisual(ConnectionVisualState.Warning, appLabels);
        TestAssert.Equal("Hay puntos por revisar", appWarning.Text);
        TestAssert.Contains("appstate_warning.png", appWarning.IconPath);
    }
}

static class ConnectionButtonStateTests
{
    private static readonly ConnectionButtonLabels Labels = new(
        "Autorizando...",
        "Conectando...",
        "Desconectar Twitch",
        "Conectar Twitch",
        "Conectar Arduino",
        "Probando...",
        "Probar Alexa",
        "Desconectar OBS",
        "Conectar OBS",
        "Actualizando...",
        "Actualizar escenas");

    public static void DisablesTwitchWhileBusy()
    {
        var state = ConnectionButtonStateService.ResolveTwitch(
            isAuthorizing: false,
            isConnecting: true,
            isRunning: false,
            Labels);

        TestAssert.False(state.IsEnabled);
        TestAssert.Equal("Conectando...", state.Content);
        TestAssert.Equal("Plug", state.IconKey);
    }

    public static void MapsObsButtons()
    {
        var connected = ConnectionButtonStateService.ResolveObs(
            enabled: true,
            isConnecting: false,
            isSceneActionRunning: false,
            isConnected: true,
            Labels);
        var busyTest = ConnectionButtonStateService.ResolveObsTest(
            enabled: true,
            isConnecting: true,
            isSceneActionRunning: false,
            Labels);

        TestAssert.True(connected.IsEnabled);
        TestAssert.Equal("Desconectar OBS", connected.Content);
        TestAssert.Equal("Plug", connected.IconKey);
        TestAssert.False(busyTest.IsEnabled);
        TestAssert.Equal("Actualizando...", busyTest.Content);
        TestAssert.Equal("Refresh", busyTest.IconKey);
    }
}

static class ObsStatusTextTests
{
    private static readonly ObsStatusTextLabels Labels = new(
        "Desactivado",
        "Conectando",
        "Conectado",
        "Desconectado",
        "Revisar conexion",
        "OBS desactivado. Las acciones OBS no se mostraran ni ejecutaran.",
        "OBS conectado en {0}:{1}.",
        "Conecta OBS Studio para leer escenas y preparar automatizaciones.",
        "Sin escena",
        "127.0.0.1",
        "Sin version",
        "Activado",
        "Desactivado");

    public static void BuildsDisplayValues()
    {
        var disabled = ObsStatusTextService.Build(
            enabled: false,
            isConnecting: false,
            isConnected: false,
            connectionError: "",
            currentScene: "",
            host: "",
            port: 4455,
            version: "",
            sceneCount: -10,
            studioMode: false,
            Labels);

        TestAssert.Equal("Desactivado", disabled.State);
        TestAssert.Contains("OBS desactivado", disabled.StatusText);
        TestAssert.Equal("Sin escena", disabled.CurrentScene);
        TestAssert.Equal("127.0.0.1", disabled.Host);
        TestAssert.Equal("0", disabled.SceneCount);

        var connected = ObsStatusTextService.Build(
            enabled: true,
            isConnecting: false,
            isConnected: true,
            connectionError: "",
            currentScene: " Gameplay ",
            host: " localhost ",
            port: 4455,
            version: "30.2",
            sceneCount: 4,
            studioMode: true,
            Labels);

        TestAssert.Equal("Conectado", connected.State);
        TestAssert.Contains("localhost:4455", connected.StatusText);
        TestAssert.Equal("Gameplay", connected.CurrentScene);
        TestAssert.Equal("Activado", connected.StudioMode);

        var warning = ObsStatusTextService.Build(true, false, false, " fallido ", "", "127.0.0.1", 4455, "", 0, false, Labels);

        TestAssert.Equal("Revisar conexion", warning.State);
        TestAssert.Equal("fallido", warning.StatusText);
    }
}

static class ObsSceneViewTests
{
    public static void BuildsRowsAndChoices()
    {
        var rows = ObsSceneViewService.BuildRows(
        [
            new ObsSceneInfo("Gameplay"),
            new ObsSceneInfo("Una escena con nombre demasiado largo para tarjeta"),
            new ObsSceneInfo("")
        ], "gameplay", shortNameLength: 10);

        TestAssert.Equal(2, rows.Count);
        TestAssert.True(rows[0].IsCurrent);
        TestAssert.Equal("Una escena...", rows[1].ShortName);

        var choices = ObsSceneViewService.BuildChoices(rows, "Mantener escena actual");

        TestAssert.Equal(3, choices.Count);
        TestAssert.Equal("Mantener escena actual", choices[0].Label);
        TestAssert.Equal("Gameplay", ObsSceneViewService.ResolveSelectedSceneName("Gameplay", choices));
        TestAssert.Equal("", ObsSceneViewService.ResolveSelectedSceneName("No existe", choices));
    }
}

static class ObsViewModelTests
{
    public static void UpdatesStatusAndScenes()
    {
        var viewModel = new ObsViewModel();

        viewModel.UpdateStatus(
            new ObsStatusText(
                "Conectado",
                "OBS conectado",
                "Gameplay",
                "127.0.0.1",
                "4455",
                "30.2",
                "2",
                "Desactivado"),
            isScenesEnabled: true);
        viewModel.ReplaceScenes(
        [
            new ObsSceneRow("Gameplay", true, "Gameplay"),
            new ObsSceneRow("BRB", false, "BRB")
        ]);

        TestAssert.Equal("Conectado", viewModel.ConnectionState);
        TestAssert.Equal("Gameplay", viewModel.CurrentScene);
        TestAssert.True(viewModel.IsScenesEnabled);
        TestAssert.Equal(1d, viewModel.ScenesOpacity);
        TestAssert.Equal(2, viewModel.SceneRows.Count);

        var config = AppConfig.CreateDefault();
        config.Obs.OverlayWidth = 1280;
        config.Obs.OverlayHeight = 720;
        config.Obs.OverlayMediaWidth = 320;
        config.Obs.OverlayMediaHeight = 180;
        config.Obs.OverlayPositionMode = "Custom";
        config.Obs.OverlayX = 20;
        config.Obs.OverlayY = 40;
        viewModel.LoadOverlayConfig(config, "http://localhost:1234/overlay");

        TestAssert.Equal("http://localhost:1234/overlay", viewModel.OverlayUrl);
        TestAssert.Equal("1280", viewModel.OverlayWidthText);
        TestAssert.Equal("720", viewModel.OverlayHeightText);
        TestAssert.Equal("320", viewModel.OverlayMediaWidthText);
        TestAssert.Equal("180", viewModel.OverlayMediaHeightText);
        TestAssert.Equal("Custom", viewModel.OverlayPositionMode);
        TestAssert.Equal("20", viewModel.OverlayXText);
        TestAssert.Equal("40", viewModel.OverlayYText);
        TestAssert.True(viewModel.IsCustomOverlayPosition);
        TestAssert.Equal(1d, viewModel.OverlayCoordinateOpacity);

        viewModel.OverlayPositionMode = "Center";
        TestAssert.False(viewModel.IsCustomOverlayPosition);
        TestAssert.Equal(0.58d, viewModel.OverlayCoordinateOpacity);

        viewModel.UpdateStatus(
            new ObsStatusText("Desconectado", "", "Sin escena", "127.0.0.1", "4455", "", "0", "Desactivado"),
            isScenesEnabled: false);

        TestAssert.False(viewModel.IsScenesEnabled);
        TestAssert.Equal(0.58d, viewModel.ScenesOpacity);
    }

    public static void ExecutesConfiguredActions()
    {
        var viewModel = new ObsViewModel();
        var actions = new List<string>();

        viewModel.ConfigureActions(
            () => actions.Add("copy"),
            () => actions.Add("refresh"),
            parameter => actions.Add($"preview:{parameter}"),
            parameter => actions.Add($"change:{parameter}"));

        viewModel.CopyOverlayUrlCommand.Execute(null);
        viewModel.RefreshScenesCommand.Execute(null);
        viewModel.PreviewSceneCommand.Execute("BRB");
        viewModel.ChangeSceneCommand.Execute("Gameplay");

        TestAssert.Equal("copy,refresh,preview:BRB,change:Gameplay", string.Join(",", actions));
    }
}

static class DiagnosticReportServiceTests
{
    public static void BuildsReportWithoutNetwork()
    {
        var config = AppConfig.CreateDefault();
        var service = CreateService();

        var result = service.BuildAsync(new DiagnosticReportContext(
            config,
            @"C:\tmp\missing-settings.json",
            @"C:\tmp\missing-backups",
            EventSubRunning: false,
            StreamStatus: null,
            LightHasOpenPort: false,
            LightCurrentPort: "",
            LightAckStatusText: "",
            RuleHasValidAudio: _ => true)).GetAwaiter().GetResult();

        TestAssert.Contains("Diagnostico Neo Twitch", result.Report);
        TestAssert.Contains("Twitch", result.Report);
        TestAssert.True(result.WarningCount > 0);
    }

    public static void ReportsMissingAudio()
    {
        var config = AppConfig.CreateDefault();
        config.Rules.Clear();
        config.Rules.Add(new EventRule
        {
            Name = "Audio roto",
            IsEnabled = true,
            EventKind = TwitchEventKind.Follow,
            PlayAudio = true
        });
        var service = CreateService();

        var result = service.BuildAsync(new DiagnosticReportContext(
            config,
            @"C:\tmp\missing-settings.json",
            @"C:\tmp\missing-backups",
            EventSubRunning: false,
            StreamStatus: null,
            LightHasOpenPort: false,
            LightCurrentPort: "",
            LightAckStatusText: "",
            RuleHasValidAudio: _ => false)).GetAwaiter().GetResult();

        TestAssert.Contains("Alertas con audio faltante", result.Report);
    }

    private static DiagnosticReportService CreateService()
    {
        return new DiagnosticReportService(_ => Task.FromResult(new VersionCheckResult(
            NeoTwitchProduct.CurrentVersionText,
            NeoTwitchProduct.CurrentVersionText,
            "https://example.test/release",
            IsUpdateAvailable: false)));
    }
}

static class VersionComparisonTests
{
    public static void ComparesNormalizedTags()
    {
        TestAssert.True(VersionComparisonService.IsNewer("V2.2.4", "2.2.3"));
        TestAssert.False(VersionComparisonService.IsNewer("v2.2.3", "V2.2.3"));
        TestAssert.False(VersionComparisonService.IsNewer("nope", "V2.2.3"));

        TestAssert.True(VersionComparisonService.TryParseVersion("V2.2.4", out var parsed));
        TestAssert.Equal(new Version(2, 2, 4), parsed);
    }
}

static class ActivityLogServiceTests
{
    public static void TrimsActivityAndDashboardEntries()
    {
        var activity = new ActivityLogService();

        for (var i = 0; i < ActivityLogService.MaxActivityEntries + 5; i++)
        {
            activity.Add($"Sistema: mensaje {i}", ActivityLogKind.Info);
        }

        TestAssert.Equal(ActivityLogService.MaxActivityEntries, activity.Entries.Count);
        TestAssert.Equal(ActivityLogService.MaxDashboardEntries, activity.DashboardEntries.Count);
        TestAssert.Contains("mensaje 254", activity.Entries[0].Message);
        TestAssert.Contains("mensaje 254", activity.DashboardEntries[0].Message);
    }

    public static void FiltersEntriesAndSearchText()
    {
        var activity = new ActivityLogService();
        var twitch = activity.Add("Twitch: conectado", ActivityLogKind.Twitch);
        var arduino = activity.Add("Arduino: puerto COM3 conectado", ActivityLogKind.Arduino);

        TestAssert.True(activity.Matches(twitch));
        activity.SetFilter("TWITCH", false);
        TestAssert.False(activity.Matches(twitch));
        TestAssert.True(activity.Matches(arduino));

        activity.ResetFilters();
        activity.SetSearchText("COM3");
        TestAssert.False(activity.Matches(twitch));
        TestAssert.True(activity.Matches(arduino));

        activity.Clear();
        TestAssert.Equal(0, activity.Entries.Count);
        TestAssert.Equal(0, activity.DashboardEntries.Count);
    }
}

static class ActivityLogPresentationTests
{
    public static void ClassifiesDisplayMetadata()
    {
        var twitch = ActivityLogPresentationService.Build("Twitch: nuevo seguidor juan", ActivityLogKind.Event);
        TestAssert.Equal("EVENTO", twitch.SourceKey);
        TestAssert.Equal("SEGUIDOR", twitch.Category);
        TestAssert.Equal("Nuevo seguidor", twitch.Title);
        TestAssert.Equal("OK", twitch.StatusText);
        TestAssert.Equal("Assets/Icons/action_follower.png", twitch.ActivityIconPath);

        var arduinoError = ActivityLogPresentationService.Build("Arduino: no se pudo abrir COM3", ActivityLogKind.Arduino);
        TestAssert.Equal("ARDUINO", arduinoError.SourceKey);
        TestAssert.Equal("Error", arduinoError.StatusText);
        TestAssert.True(arduinoError.IsImportant);
        TestAssert.Equal("Assets/Icons/status_error.png", arduinoError.StatusIconPath);
    }
}

static class ActivityLogClassifierTests
{
    public static void ResolvesSourcesAndCategories()
    {
        TestAssert.Equal("TWITCH", ActivityLogClassifier.ResolveSourceKey("Chat: mensaje enviado", ActivityLogKind.Info));
        TestAssert.Equal("ARDUINO", ActivityLogClassifier.ResolveSourceKey("Fondo aplicado", ActivityLogKind.Info));
        TestAssert.Equal("OBS", ActivityLogClassifier.ResolveSourceKey("OBS: escena cambiada", ActivityLogKind.Info));
        TestAssert.Equal("AUDIO", ActivityLogClassifier.ResolveSourceKey("Sonido reproducido", ActivityLogKind.Info));
        TestAssert.Equal("SISTEMA", ActivityLogClassifier.ResolveSourceKey("Configuracion guardada", ActivityLogKind.Info));
        TestAssert.Equal(ActivityLogKind.Twitch, ActivityLogClassifier.Classify("Twitch: conectado"));
        TestAssert.Equal(ActivityLogKind.Arduino, ActivityLogClassifier.Classify("Puertos COM actualizados"));
        TestAssert.Equal(ActivityLogKind.Event, ActivityLogClassifier.Classify("Juan envio 100 bits"));
        TestAssert.Equal(ActivityLogKind.Important, ActivityLogClassifier.Classify("No se pudo leer configuracion"));

        TestAssert.Equal("BITS", ActivityLogClassifier.ResolveCategory("100 bits enviados", ActivityLogKind.Event));
        TestAssert.Equal("SUB", ActivityLogClassifier.ResolveCategory("Nueva suscripcion prime", ActivityLogKind.Event));
        TestAssert.Equal("CHAT", ActivityLogClassifier.ResolveCategory("Comando de chat !rave", ActivityLogKind.Event));
        TestAssert.Equal("IMPORTANTE", ActivityLogClassifier.ResolveCategory("Aviso general", ActivityLogKind.Important));
    }
}

static class ActivityViewModelTests
{
    public static void MapsFilterProperties()
    {
        var activity = new ActivityLogService();
        var viewModel = new ActivityViewModel(activity);
        var twitch = activity.Add("Twitch: conectado", ActivityLogKind.Twitch);

        TestAssert.True(activity.Matches(twitch));
        viewModel.TwitchFilterEnabled = false;

        TestAssert.False(activity.Matches(twitch));
        TestAssert.False(viewModel.IsFilterEnabled("TWITCH"));

        viewModel.ClearFilters();

        TestAssert.True(viewModel.TwitchFilterEnabled);
        TestAssert.True(activity.Matches(twitch));
    }

    public static void FiltersEntriesView()
    {
        TestThread.RunSta(() =>
        {
            var activity = new ActivityLogService();
            var viewModel = new ActivityViewModel(activity);

            activity.Add("Twitch: conectado", ActivityLogKind.Twitch);
            activity.Add("Arduino: puerto COM3 conectado", ActivityLogKind.Arduino);
            viewModel.Refresh();

            TestAssert.Equal(2, viewModel.EntriesView.Cast<ActivityLogEntry>().Count());

            viewModel.SetFilter("TWITCH", enabled: false);
            var filtered = viewModel.EntriesView.Cast<ActivityLogEntry>().ToArray();
            TestAssert.Equal(1, filtered.Length);
            TestAssert.Contains("Arduino", filtered[0].Message);

            viewModel.SearchText = "COM3";
            TestAssert.Equal(1, viewModel.EntriesView.Cast<ActivityLogEntry>().Count());

            viewModel.ClearFilters();
            TestAssert.Equal("", viewModel.SearchText);
            TestAssert.True(viewModel.IsFilterEnabled("TWITCH"));
            TestAssert.Equal(2, viewModel.EntriesView.Cast<ActivityLogEntry>().Count());

            viewModel.ClearHistory();
            TestAssert.Equal(0, activity.Entries.Count);
            TestAssert.Equal(0, viewModel.DashboardEntries.Count);
        });
    }
}

static class DashboardConnectionStateTests
{
    public static void ResolvesAllServices()
    {
        var states = DashboardConnectionStateService.Resolve(new DashboardConnectionStateInput(
            TwitchAuthorizing: false,
            TwitchConnecting: true,
            TwitchHasConnectionError: false,
            TwitchHasToken: false,
            ArduinoEnabled: true,
            ArduinoConnecting: false,
            ArduinoHasConfirmedAck: true,
            ArduinoCompatibleWithoutAck: false,
            ArduinoHasOpenPort: false,
            AlexaEnabled: true,
            AlexaConnecting: false,
            AlexaIsConfigured: true,
            AlexaRelayConnected: false,
            ObsEnabled: false,
            ObsConnecting: false,
            ObsIsConnected: false,
            ObsHasConnectionError: false));

        TestAssert.Equal(ConnectionVisualState.Connecting, states.Twitch);
        TestAssert.Equal(ConnectionVisualState.Connected, states.Arduino);
        TestAssert.Equal(ConnectionVisualState.Warning, states.Alexa);
        TestAssert.Equal(ConnectionVisualState.Disabled, states.Obs);
    }
}

static class DashboardSummaryTests
{
    public static void CountsTwitchEvents()
    {
        var summary = new DashboardSummaryService();

        summary.RegisterTwitchEvent(new TwitchEvent { Kind = TwitchEventKind.Follow });
        summary.RegisterTwitchEvent(new TwitchEvent { Kind = TwitchEventKind.Subscription });
        summary.RegisterTwitchEvent(new TwitchEvent { Kind = TwitchEventKind.Cheer, Bits = 100 });
        summary.RegisterTwitchEvent(new TwitchEvent { Kind = TwitchEventKind.Cheer, Bits = -10 });
        summary.RegisterTwitchEvent(new TwitchEvent { Kind = TwitchEventKind.ChatCommand });
        var snapshot = summary.Snapshot;

        TestAssert.Equal(1, snapshot.Followers);
        TestAssert.Equal(1, snapshot.Subscriptions);
        TestAssert.Equal(100, snapshot.Bits);
        TestAssert.Equal(1, snapshot.ChatMessages);
    }

    public static void CountsMatchedRulesSafely()
    {
        var summary = new DashboardSummaryService();

        summary.RegisterMatchedRules(3);
        summary.RegisterMatchedRules(-10);

        TestAssert.Equal(3, summary.Snapshot.Events);
    }
}

static class DashboardSummaryDisplayTests
{
    public static void FormatsSummaryMetrics()
    {
        var display = DashboardSummaryDisplayService.Build(new DashboardSummarySnapshot(
            Followers: 1,
            Subscriptions: 2,
            Bits: 300,
            ChatMessages: 4,
            Events: 5));

        TestAssert.Equal("+1", display.Followers.Text);
        TestAssert.Equal("#14B8A6", display.Followers.Color);
        TestAssert.Equal("+2", display.Subscriptions.Text);
        TestAssert.Equal("+300", display.Bits.Text);
        TestAssert.Equal("4", display.ChatMessages.Text);
        TestAssert.Equal("5", display.Events.Text);
    }
}

static class DashboardViewModelTests
{
    public static void UpdatesSummaryMetrics()
    {
        var recentEntries = new[] { "uno" };
        var viewModel = new DashboardViewModel(() => { }, recentEntries);
        var display = DashboardSummaryDisplayService.Build(new DashboardSummarySnapshot(
            Followers: 7,
            Subscriptions: 2,
            Bits: 900,
            ChatMessages: 3,
            Events: 4));

        viewModel.UpdateSummary(display);

        TestAssert.Equal("+7", viewModel.Followers.Text);
        TestAssert.Equal("+900", viewModel.Bits.Text);
        TestAssert.Equal("4", viewModel.Events.Text);
        TestAssert.Same(recentEntries, viewModel.RecentActivityEntries);
    }

    public static void UpdatesConnectionStates()
    {
        var viewModel = new DashboardViewModel(() => { });

        viewModel.UpdateConnectionStates(
            new ConnectionStateVisual("Twitch listo", "#22C55E", "Assets/Icons/status_ok.png"),
            new ConnectionStateVisual("Arduino apagado", "#94A3B8", "Assets/Icons/status_empty.png"),
            new ConnectionStateVisual("Alexa revisar", "#FFB020", "Assets/Icons/status_warning.png"),
            new ConnectionStateVisual("OBS error", "#F43F5E", "Assets/Icons/status_error.png"));

        TestAssert.Equal("Twitch listo", viewModel.TwitchState.Text);
        TestAssert.Equal("Arduino apagado", viewModel.ArduinoState.ToolTip);
        TestAssert.Equal("Alexa revisar", viewModel.AlexaState.Text);
        TestAssert.Equal("OBS error", viewModel.ObsState.Text);
        TestAssert.Equal((byte)0x22, viewModel.TwitchState.Brush.Color.R);
    }
}

static class ConnectionsViewModelTests
{
    public static void MapsBadgesAndHelperText()
    {
        var viewModel = new ConnectionsViewModel();

        viewModel.UpdateBadges(
            new ConnectionStateVisual("Twitch listo", "#22C55E", "Assets/Icons/status_ok.png"),
            new ConnectionStateVisual("Arduino off", "#94A3B8", "Assets/Icons/status_empty.png"),
            new ConnectionStateVisual("Alexa revisar", "#FFB020", "Assets/Icons/status_warning.png"),
            new ConnectionStateVisual("OBS error", "#F43F5E", "Assets/Icons/status_error.png"));
        viewModel.UpdateAlexaStatusText("Alexa configurada");
        viewModel.UpdateObsConnectionHelpText("OBS desconectado");
        var config = AppConfig.CreateDefault();
        config.TwitchClientId = "client-id";
        config.TwitchClientSecret = "secret";
        config.ArduinoEnabled = true;
        config.SerialPort = "COM7";
        config.BaudRate = 57600;
        config.Alexa.Enabled = true;
        config.Alexa.RelayUrl = "https://relay.example";
        config.Alexa.AuthToken = "alexa-token";
        config.Obs.Enabled = true;
        config.Obs.Host = "192.168.0.20";
        config.Obs.Port = 4456;
        config.Obs.Password = "obs-password";
        viewModel.LoadTwitchConfig(config);
        viewModel.LoadArduinoConfig(config);
        viewModel.LoadAlexaConfig(config);
        viewModel.LoadObsConnectionConfig(config);
        var portChoices = new[] { "COM3" };
        viewModel.UpdatePortChoices(portChoices);

        TestAssert.Equal("Twitch listo", viewModel.TwitchBadge.Text);
        TestAssert.Equal("Arduino off", viewModel.ArduinoBadge.Text);
        TestAssert.Equal("client-id", viewModel.TwitchClientId);
        TestAssert.Equal("secret", viewModel.TwitchClientSecret);
        TestAssert.True(viewModel.ArduinoEnabled);
        TestAssert.Equal("COM7", viewModel.SerialPort);
        TestAssert.Equal("57600", viewModel.BaudRateText);
        TestAssert.True(viewModel.AlexaEnabled);
        TestAssert.Equal("https://relay.example", viewModel.AlexaRelayUrl);
        TestAssert.Equal("alexa-token", viewModel.AlexaAuthToken);
        TestAssert.True(viewModel.ObsEnabled);
        TestAssert.Equal("192.168.0.20", viewModel.ObsHost);
        TestAssert.Equal("4456", viewModel.ObsPortText);
        TestAssert.Equal("obs-password", viewModel.ObsPassword);
        TestAssert.Equal("Alexa configurada", viewModel.AlexaStatusText);
        TestAssert.Equal("OBS desconectado", viewModel.ObsConnectionHelpText);
        TestAssert.Same(portChoices, viewModel.PortChoices);
        TestAssert.Equal((byte)0x22, viewModel.TwitchBadge.ForegroundBrush.Color.R);
        TestAssert.Equal((byte)0x22, viewModel.TwitchBadge.BackgroundBrush.Color.A);
    }

    public static void MapsButtonStates()
    {
        var viewModel = new ConnectionsViewModel();

        viewModel.UpdateButtonStates(
            new ConnectionButtonState(false, "Conectando...", "Plug"),
            new ConnectionButtonState(true, "Conectar Arduino", "Plug"),
            new ConnectionButtonState(false, "Probando Alexa", "Play"),
            new ConnectionButtonState(true, "Desconectar OBS", "Plug"),
            new ConnectionButtonState(true, "Actualizar escenas", "Refresh"));

        TestAssert.False(viewModel.TwitchButton.IsEnabled);
        TestAssert.Equal("Conectando...", viewModel.TwitchButton.Text);
        TestAssert.True(viewModel.ArduinoButton.IsEnabled);
        TestAssert.False(viewModel.AlexaTestButton.IsEnabled);
        TestAssert.Equal("Desconectar OBS", viewModel.ObsButton.Text);
        TestAssert.Equal("Actualizar escenas", viewModel.ObsTestButton.Text);
    }

    public static void ExecutesConfiguredActions()
    {
        var viewModel = new ConnectionsViewModel();
        var actions = new List<string>();

        viewModel.ConfigureActions(
            () => actions.Add("save"),
            () => actions.Add("twitch"),
            () => actions.Add("open-twitch"),
            () => actions.Add("client-id"),
            () => actions.Add("client-secret"),
            () => actions.Add("ports"),
            () => actions.Add("arduino"),
            () => actions.Add("open-alexa"),
            () => actions.Add("test-alexa"),
            () => actions.Add("alexa-url"),
            () => actions.Add("alexa-token"),
            () => actions.Add("open-obs"),
            () => actions.Add("connect-obs"),
            () => actions.Add("test-obs"),
            () => actions.Add("obs-password"));

        viewModel.SaveCommand.Execute(null);
        viewModel.ToggleTwitchCommand.Execute(null);
        viewModel.OpenTwitchConsoleCommand.Execute(null);
        viewModel.ToggleClientIdVisibilityCommand.Execute(null);
        viewModel.ToggleClientSecretVisibilityCommand.Execute(null);
        viewModel.DetectPortsCommand.Execute(null);
        viewModel.ConnectArduinoCommand.Execute(null);
        viewModel.OpenAlexaConsoleCommand.Execute(null);
        viewModel.TestAlexaCommand.Execute(null);
        viewModel.ToggleAlexaRelayUrlVisibilityCommand.Execute(null);
        viewModel.ToggleAlexaAuthTokenVisibilityCommand.Execute(null);
        viewModel.OpenObsGuideCommand.Execute(null);
        viewModel.ConnectObsCommand.Execute(null);
        viewModel.TestObsCommand.Execute(null);
        viewModel.ToggleObsPasswordVisibilityCommand.Execute(null);

        TestAssert.Equal(
            "save,twitch,open-twitch,client-id,client-secret,ports,arduino,open-alexa,test-alexa,alexa-url,alexa-token,open-obs,connect-obs,test-obs,obs-password",
            string.Join(",", actions));
    }
}

static class AlexaViewModelTests
{
    public static void ExecutesConfiguredActions()
    {
        var viewModel = new AlexaViewModel();
        var actions = new List<string>();
        var config = AppConfig.CreateDefault();
        config.BackgroundAlexaEnabled = true;
        config.BackgroundAlexaTurnOffAfterEvent = true;
        config.BackgroundAlexaOnEventName = "fondo_on";
        config.BackgroundAlexaOffEventName = "fondo_off";

        viewModel.LoadBackgroundConfig(config);

        TestAssert.True(viewModel.BackgroundEnabled);
        TestAssert.True(viewModel.BackgroundTurnOffAfterEvent);
        TestAssert.Equal("fondo_on", viewModel.BackgroundOnEventName);
        TestAssert.Equal("fondo_off", viewModel.BackgroundOffEventName);

        viewModel.ConfigureActions(
            () => actions.Add("apply"),
            () => actions.Add("stop"));

        viewModel.ApplyBackgroundCommand.Execute(null);
        viewModel.StopBackgroundCommand.Execute(null);

        TestAssert.Equal("apply,stop", string.Join(",", actions));
    }
}

static class LightsViewModelTests
{
    public static void ExecutesConfiguredActions()
    {
        var viewModel = new LightsViewModel();
        var actions = new List<string>();

        viewModel.ConfigureActions(
            () => actions.Add("add"),
            () => actions.Add("duplicate"),
            () => actions.Add("remove"),
            () => actions.Add("apply"),
            () => actions.Add("stop"),
            () => actions.Add("sketch"),
            () => actions.Add("guide"));
        viewModel.ConfigureEditorActions(
            parameter => actions.Add($"pattern:{parameter}"),
            parameter => actions.Add($"adjust:{parameter}"),
            parameter => actions.Add($"preset:{parameter}"),
            parameter => actions.Add($"color:{parameter}"));

        viewModel.AddStripCommand.Execute(null);
        viewModel.DuplicateStripCommand.Execute(null);
        viewModel.RemoveStripCommand.Execute(null);
        viewModel.ApplyBackgroundCommand.Execute(null);
        viewModel.StopBackgroundCommand.Execute(null);
        viewModel.OpenSketchCommand.Execute(null);
        viewModel.OpenGuideCommand.Execute(null);
        viewModel.SelectBackgroundPatternCommand.Execute(LightPattern.Pulse);
        viewModel.AdjustBackgroundLightValueCommand.Execute("Step:10");
        viewModel.SelectBackgroundLightPresetCommand.Execute("Medium");
        viewModel.PickBackgroundLightColorCommand.Execute("Secondary");
        viewModel.UpdateArduinoStatus(new LightsArduinoStatusText("Arduino Uno", "COM3", "300", "Pin 6"));
        var patternChoices = new[] { "Fijo" };
        viewModel.UpdateBackgroundPatternChoices(patternChoices);
        var config = AppConfig.CreateDefault();
        config.BackgroundEnabled = true;
        config.BackgroundTargetPins = "6, 7";
        config.BackgroundPattern = LightPattern.Rave;
        config.BackgroundPrimaryColor = "#112233";
        config.BackgroundSecondaryColor = "#445566";
        config.BackgroundTertiaryColor = "#778899";
        config.BackgroundBrightness = 211;
        config.BackgroundCycleMs = 120;
        config.BackgroundStepMs = 450;
        viewModel.LoadBackground(config);
        var strips = new[] { new LedStripConfig { Name = "Principal", Pin = 6, LedCount = 30 } };
        viewModel.SetLedStripsSource(strips);
        viewModel.RefreshLedStrips();
        viewModel.LoadSelectedStrip(strips[0]);

        TestAssert.Equal("add,duplicate,remove,apply,stop,sketch,guide,pattern:Pulse,adjust:Step:10,preset:Medium,color:Secondary", string.Join(",", actions));
        TestAssert.Equal("Arduino Uno", viewModel.ArduinoDeviceText);
        TestAssert.Equal("COM3", viewModel.ArduinoPortText);
        TestAssert.Equal("300", viewModel.ArduinoLedCountText);
        TestAssert.Equal("Pin 6", viewModel.ArduinoPinsText);
        TestAssert.Same(patternChoices, viewModel.BackgroundPatternChoices);
        TestAssert.True(viewModel.BackgroundEnabled);
        TestAssert.Equal("6, 7", viewModel.BackgroundTargetPins);
        TestAssert.Equal(LightPattern.Rave, viewModel.BackgroundPattern);
        TestAssert.Equal("#112233", viewModel.BackgroundPrimaryColor);
        TestAssert.Equal("#445566", viewModel.BackgroundSecondaryColor);
        TestAssert.Equal("#778899", viewModel.BackgroundTertiaryColor);
        TestAssert.Equal(211d, viewModel.BackgroundBrightness);
        TestAssert.Equal(120d, viewModel.BackgroundCycleMs);
        TestAssert.Equal(450d, viewModel.BackgroundStepMs);
        TestAssert.Same(strips, viewModel.LedStripsView.SourceCollection);
        TestAssert.True(viewModel.IsStripEditorEnabled);
        TestAssert.Equal("Principal", viewModel.SelectedStripName);
        TestAssert.Equal("6", viewModel.SelectedStripPinText);
        TestAssert.Equal("30", viewModel.SelectedStripLedCountText);
        viewModel.LoadSelectedStrip(null);
        TestAssert.False(viewModel.IsStripEditorEnabled);
        TestAssert.Equal("", viewModel.SelectedStripName);
        TestAssert.Equal(0, viewModel.BackgroundLedPreviewDots.Count);
    }
}

static class RuleEditorViewModelTests
{
    public static void MapsBasicFields()
    {
        var viewModel = new RuleEditorViewModel();
        var rule = new EventRule
        {
            IsEnabled = false,
            Name = "Comando rave",
            EventKind = TwitchEventKind.ChatCommand,
            CustomRewardTitle = "Canje raro",
            ChatCommand = "!rave",
            MinimumBits = 250,
            SendChatMessage = true,
            ChatMessageTemplate = "Rave @{user}",
            SendAlexaEvent = true,
            UseLights = true,
            PlayAudio = true,
            SendObsScene = true,
            ObsSceneName = "Gameplay",
            ObsSceneDelayMs = 500,
            ObsReturnToPreviousScene = false,
            ObsReturnDelayMs = 7000,
            SendObsMedia = true,
            ObsMediaKind = ObsMediaKind.Video,
            ObsMediaSourceMode = MediaSourceMode.Group,
            ObsMediaAssetId = "video-1",
            ObsMediaGroupId = "videos",
            ObsMediaDurationMs = 9000,
            AudioSourceMode = AudioSourceMode.Group,
            AudioAssetId = "audio-1",
            AudioGroupId = "audios",
            Pattern = LightPattern.Rave,
            TargetPins = "6, 7",
            PrimaryColor = "#112233",
            SecondaryColor = "#445566",
            TertiaryColor = "#778899",
            Brightness = 211,
            DurationMs = 3000,
            CycleMs = 120,
            StepMs = 450
        };

        viewModel.LoadBasicFields(rule);

        TestAssert.False(viewModel.IsEnabled);
        TestAssert.Equal("Comando rave", viewModel.RuleNameText);
        TestAssert.Equal(TwitchEventKind.ChatCommand, viewModel.EventKind);
        TestAssert.Equal("Canje raro", viewModel.CustomRewardTitle);
        TestAssert.Equal("!rave", viewModel.ChatCommand);
        TestAssert.Equal("250", viewModel.MinimumBitsText);
        TestAssert.True(viewModel.SendChatMessage);
        TestAssert.Equal("Rave @{user}", viewModel.ChatMessageTemplate);
        TestAssert.True(viewModel.SendAlexaEvent);
        TestAssert.True(viewModel.UseLights);
        TestAssert.True(viewModel.PlayAudio);
        TestAssert.True(viewModel.SendObsScene);
        TestAssert.Equal("Gameplay", viewModel.ObsSceneName);
        TestAssert.Equal("500", viewModel.ObsSceneDelayText);
        TestAssert.False(viewModel.ObsReturnToPreviousScene);
        TestAssert.Equal("7000", viewModel.ObsReturnDelayText);
        TestAssert.True(viewModel.SendObsMedia);
        TestAssert.Equal(ObsMediaKind.Video, viewModel.ObsMediaKind);
        TestAssert.Equal(MediaSourceMode.Group, viewModel.ObsMediaSourceMode);
        TestAssert.Equal("video-1", viewModel.ObsMediaAssetId);
        TestAssert.Equal("videos", viewModel.ObsMediaGroupId);
        TestAssert.Equal("9000", viewModel.ObsMediaDurationText);
        TestAssert.Equal(AudioSourceMode.Group, viewModel.AudioSourceMode);
        TestAssert.Equal("audio-1", viewModel.AudioAssetId);
        TestAssert.Equal("audios", viewModel.AudioGroupId);
        TestAssert.Equal(LightPattern.Rave, viewModel.Pattern);
        TestAssert.Equal("6, 7", viewModel.TargetPins);
        TestAssert.Equal("#112233", viewModel.PrimaryColor);
        TestAssert.Equal("#445566", viewModel.SecondaryColor);
        TestAssert.Equal("#778899", viewModel.TertiaryColor);
        TestAssert.Equal(211d, viewModel.Brightness);
        TestAssert.Equal(3000d, viewModel.DurationMs);
        TestAssert.Equal(120d, viewModel.CycleMs);
        TestAssert.Equal(450d, viewModel.StepMs);

        viewModel.Clear();

        TestAssert.True(viewModel.IsEnabled);
        TestAssert.Equal("", viewModel.RuleNameText);
        TestAssert.Equal(TwitchEventKind.Follow, viewModel.EventKind);
        TestAssert.Equal("", viewModel.CustomRewardTitle);
        TestAssert.Equal("", viewModel.ChatCommand);
        TestAssert.Equal("1", viewModel.MinimumBitsText);
        TestAssert.False(viewModel.SendChatMessage);
        TestAssert.Equal("", viewModel.ChatMessageTemplate);
        TestAssert.False(viewModel.SendAlexaEvent);
        TestAssert.False(viewModel.UseLights);
        TestAssert.False(viewModel.PlayAudio);
        TestAssert.False(viewModel.SendObsScene);
        TestAssert.Equal("", viewModel.ObsSceneName);
        TestAssert.Equal("0", viewModel.ObsSceneDelayText);
        TestAssert.True(viewModel.ObsReturnToPreviousScene);
        TestAssert.Equal("15000", viewModel.ObsReturnDelayText);
        TestAssert.False(viewModel.SendObsMedia);
        TestAssert.Equal(ObsMediaKind.Image, viewModel.ObsMediaKind);
        TestAssert.Equal(MediaSourceMode.Single, viewModel.ObsMediaSourceMode);
        TestAssert.Equal("", viewModel.ObsMediaAssetId);
        TestAssert.Equal("", viewModel.ObsMediaGroupId);
        TestAssert.Equal("5000", viewModel.ObsMediaDurationText);
        TestAssert.Equal(AudioSourceMode.Single, viewModel.AudioSourceMode);
        TestAssert.Equal("", viewModel.AudioAssetId);
        TestAssert.Equal("", viewModel.AudioGroupId);
        TestAssert.Equal(LightPattern.Pulse, viewModel.Pattern);
        TestAssert.Equal("", viewModel.TargetPins);
        TestAssert.Equal("#14B8A6", viewModel.PrimaryColor);
        TestAssert.Equal("#B56CFF", viewModel.SecondaryColor);
        TestAssert.Equal("#FFFFFF", viewModel.TertiaryColor);
        TestAssert.Equal(180d, viewModel.Brightness);
        TestAssert.Equal(5000d, viewModel.DurationMs);
        TestAssert.Equal(80d, viewModel.CycleMs);
        TestAssert.Equal(120d, viewModel.StepMs);
    }
}

static class DashboardStatusTextLabelFactoryTests
{
    public static void BuildsLabels()
    {
        var labels = DashboardStatusTextLabelFactory.Build(UiTextService.CreateDefault());

        TestAssert.Equal("Sin Twitch", labels.NoTwitch);
        TestAssert.Equal("Conectado", labels.ConnectionConnected);
        TestAssert.Contains("{0}", labels.ArduinoConnectedFormat);
        TestAssert.Contains("{0}", labels.AlexaSidebarFormat);
    }
}

static class DashboardStatusTextTests
{
    private static readonly DashboardStatusTextLabels Labels = new(
        "Sin Twitch",
        "Sin login",
        "Canal Twitch",
        "Autorizando",
        "Conectando",
        "Revisar conexion",
        "Eventos conectados",
        "Sesion autorizada",
        "Sin conectar",
        "Esperando autorizacion de Twitch.",
        "Conectando EventSub y chat de Twitch.",
        "En directo en {0}. {1} espectadores.",
        "En directo. {0} espectadores.",
        "Canal sin directo activo.",
        "Escuchando eventos. Directo sin consultar.",
        "Listo para conectar eventos.",
        "Desactivado",
        "Conectando",
        "Conectado en {0}",
        "COM",
        "Verificando Arduino",
        "Sin conectar",
        "Las luces Arduino no se mostraran ni ejecutaran.",
        "Intentando conectar con {0}.",
        "el puerto configurado",
        "{0} de fondo",
        "Fondo apagado",
        "{0} baudios. {1} tiras, {2} LEDs. {3}.",
        "{0} baudios. Modo compatible sin ACK; las luces pueden funcionar, pero el sketch no confirmo comandos.",
        "El puerto esta abierto; esperando confirmacion del sketch.",
        "Puerto: {0}. {1} tiras, {2} LEDs.",
        "sin COM",
        "Sin pines",
        "Pin {0}",
        "Verificando",
        "Conectado",
        "Desconectado",
        "Conectando",
        "Alexa lista. Las reglas pueden enviar eventos a la Skill/relay.",
        "Alexa activa, falta configurar una URL valida de Skill/relay.",
        "Alexa desactivada. Las reglas no mostraran acciones de Alexa.",
        "Relay conectado",
        "Relay configurado",
        "Configuracion incompleta",
        "Fondo: {0}",
        "Fondo sin mantener",
        "Al finalizar: {0}",
        "Al finalizar: conserva estado",
        "{0}. {1}.");

    public static void FormatsLiveTwitchStatus()
    {
        var text = DashboardStatusTextService.BuildTwitchStatusText(
            isAuthorizing: false,
            isConnecting: false,
            new TwitchStreamStatus(true, 23, "Stream", "Just Chatting"),
            eventSubRunning: true,
            Labels);

        TestAssert.Equal("En directo en Just Chatting. 23 espectadores.", text);
    }

    public static void FormatsConnectionLabels()
    {
        var channel = DashboardStatusTextService.BuildChannelDisplayText(
            channelReady: true,
            displayName: "",
            login: "neo_streamer",
            Labels);

        TestAssert.Equal("neo_streamer", channel.Name);
        TestAssert.Equal("@neo_streamer", channel.Login);
        TestAssert.Equal(
            "Revisar conexion",
            DashboardStatusTextService.BuildTwitchConnectionText(
                isAuthorizing: false,
                isConnecting: false,
                hasConnectionError: true,
                eventSubRunning: true,
                hasToken: true,
                Labels));
        TestAssert.Equal(
            "Relay configurado",
            DashboardStatusTextService.BuildAlexaConnectionText(
                enabled: true,
                isConfigured: true,
                isConnecting: false,
                relayConnected: false,
                Labels));
    }

    public static void FormatsArduinoStatus()
    {
        TestAssert.Equal(
            "Conectado en COM3",
            DashboardStatusTextService.BuildArduinoConnectionText(
                arduinoEnabled: true,
                isConnecting: false,
                hasConfirmedAck: true,
                compatibleWithoutAck: false,
                hasOpenPort: true,
                currentPort: "COM3",
                Labels));
        TestAssert.Equal(
            "115200 baudios. 1 tiras, 30 LEDs. Color fijo de fondo.",
            DashboardStatusTextService.BuildArduinoStatusText(
                arduinoEnabled: true,
                isConnecting: false,
                hasConfirmedAck: true,
                compatibleWithoutAck: false,
                hasOpenPort: true,
                serialPort: "COM3",
                baudRate: 115200,
                stripCount: 1,
                totalLeds: 30,
                backgroundEnabled: true,
                backgroundPattern: LightPattern.Solid,
                Labels));

        var lights = DashboardStatusTextService.BuildLightsArduinoStatusText(
            arduinoEnabled: true,
            hasConfirmedAck: false,
            compatibleWithoutAck: false,
            hasOpenPort: true,
            currentPort: "",
            configuredPort: "COM4",
            [new LedStripConfig { Pin = 6, LedCount = 30 }],
            Labels);

        TestAssert.Equal("Verificando", lights.Device);
        TestAssert.Equal("COM4", lights.Port);
        TestAssert.Equal("30", lights.LedCount);
        TestAssert.Equal("Pin 6", lights.Pins);
    }

    public static void FormatsAlexaBackgroundStatus()
    {
        var text = DashboardStatusTextService.BuildAlexaSidebarStatusText(
            backgroundEnabled: true,
            backgroundOnEventName: "luz_encendida",
            turnOffAfterEvent: false,
            backgroundOffEventName: "luz_apagada",
            Labels);

        TestAssert.Equal("Fondo: luz_encendida. Al finalizar: conserva estado.", text);
    }
}

static class UiTextCatalogTests
{
    public static void ContainsAllTextKeys()
    {
        var catalog = SpanishUiTextCatalog.Create();
        var keyFields = typeof(UiTextKeys)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string));

        foreach (var field in keyFields)
        {
            var key = (string)field.GetRawConstantValue()!;
            TestAssert.True(catalog.ContainsKey(key), $"Falta texto para la key '{key}'.");
        }
    }
}

static class UiTextFormatterTests
{
    public static void FormatsFallbackText()
    {
        TestAssert.Equal("Canal", UiTextFormatter.FirstNonEmpty("", " ", "Canal", "Otro"));
        TestAssert.Equal("fallback", UiTextFormatter.NormalizeEventName(" ", "fallback"));
        TestAssert.Equal("evento", UiTextFormatter.NormalizeEventName("  evento  ", "fallback"));
        TestAssert.Equal("uno, dos y 2 mas", UiTextFormatter.FormatNameList(["uno", "dos", "tres", "cuatro"], maxVisible: 2));
        TestAssert.Equal("sin nombre", UiTextFormatter.FormatNameList(["", " "]));
    }

    public static void BuildsBoundedSecretMasks()
    {
        TestAssert.Equal(8, UiTextFormatter.BuildSecretMask("abc").Length);
        TestAssert.Equal(20, UiTextFormatter.BuildSecretMask(new string('x', 80)).Length);
        TestAssert.Equal("********", UiTextFormatter.BuildSecretMask(""));
    }
}

static class CircularProgressGeometryTests
{
    public static void CalculatesPercentages()
    {
        TestAssert.Equal(0, CircularProgressGeometryService.ToPercent(50, 0));
        TestAssert.Equal(0, CircularProgressGeometryService.ToPercent(-10, 100));
        TestAssert.Equal(50, CircularProgressGeometryService.ToPercent(50, 100));
        TestAssert.Equal(100, CircularProgressGeometryService.ToPercent(300, 100));
    }

    public static void BuildsArcGeometry()
    {
        TestAssert.Same(System.Windows.Media.Geometry.Empty, CircularProgressGeometryService.BuildArcGeometry(0));

        var half = CircularProgressGeometryService.BuildArcGeometry(0.5);
        var large = CircularProgressGeometryService.BuildArcGeometry(0.75);

        TestAssert.True(half is System.Windows.Media.PathGeometry);
        TestAssert.False(((System.Windows.Media.ArcSegment)((System.Windows.Media.PathGeometry)half).Figures[0].Segments[0]).IsLargeArc);
        TestAssert.True(((System.Windows.Media.ArcSegment)((System.Windows.Media.PathGeometry)large).Figures[0].Segments[0]).IsLargeArc);
    }
}

static class IconPathCatalogTests
{
    public static void ReturnsKnownIconsAndFallback()
    {
        TestAssert.True(IconPathCatalog.Get("Play").Contains("L19,12", StringComparison.Ordinal));
        TestAssert.Equal("M12,5 L12,19 M5,12 L19,12", IconPathCatalog.Get("Algo que no existe"));
    }
}

static class ButtonIconCatalogTests
{
    public static void MapsButtonLabels()
    {
        TestAssert.True(ButtonIconCatalog.TryGetIconKey("Guardar cambios", out var saveIcon));
        TestAssert.Equal("Save", saveIcon);

        TestAssert.True(ButtonIconCatalog.TryGetIconKey("  Probar alerta  ", out var playIcon));
        TestAssert.Equal("Play", playIcon);

        TestAssert.False(ButtonIconCatalog.TryGetIconKey("Texto sin icono", out _));
    }
}

static class ButtonIconContentTests
{
    public static void BuildsIconButtonContent()
    {
        TestThread.RunSta(() =>
        {
            var button = new System.Windows.Controls.Button
            {
                Content = "Guardar cambios"
            };

            TestAssert.True(ButtonIconContentService.TrySetButtonIcon(button, "Guardar cambios"));
            TestAssert.True(button.Content is System.Windows.Controls.StackPanel);
            var panel = (System.Windows.Controls.StackPanel)button.Content;
            TestAssert.Equal(2, panel.Children.Count);
            TestAssert.True(panel.Children[0] is System.Windows.Shapes.Path);
            TestAssert.True(panel.Children[1] is System.Windows.Controls.TextBlock);
            TestAssert.Equal("Guardar cambios", ((System.Windows.Controls.TextBlock)panel.Children[1]).Text);
        });
    }
}

static class VisualTreeTraversalTests
{
    public static void FindsDescendants()
    {
        TestThread.RunSta(() =>
        {
            var root = new System.Windows.Controls.StackPanel();
            var border = new System.Windows.Controls.Border();
            var nested = new System.Windows.Controls.Button();
            border.Child = nested;
            root.Children.Add(border);

            var buttons = VisualTreeTraversalService.FindChildren<System.Windows.Controls.Button>(root).ToList();

            TestAssert.Equal(1, buttons.Count);
            TestAssert.True(ReferenceEquals(nested, buttons[0]));
        });
    }
}

static class FilterButtonThemeTests
{
    public static void AppliesActiveAndInactiveColors()
    {
        TestThread.RunSta(() =>
        {
            var palette = ThemePalette.Dark;
            var button = new System.Windows.Controls.Button();

            FilterButtonThemeService.Apply(button, active: true, "#14B8A6", palette);
            TestAssert.Equal(
                ((System.Windows.Media.SolidColorBrush)UiBrushFactory.FrozenBrushFrom("#14B8A6")).Color,
                ((System.Windows.Media.SolidColorBrush)button.Foreground).Color);

            FilterButtonThemeService.Apply(button, active: false, "#14B8A6", palette);
            TestAssert.Same(palette.Input, button.Background);
            TestAssert.Same(palette.Text, button.Foreground);
            TestAssert.Same(palette.Border, button.BorderBrush);
        });
    }
}

static class NavigationButtonThemeTests
{
    public static void AppliesSelectedColors()
    {
        TestThread.RunSta(() =>
        {
            var palette = ThemePalette.Dark;
            var button = new System.Windows.Controls.Button();

            NavigationButtonThemeService.Apply(button, palette, selected: true);
            TestAssert.Same(palette.NavSelected, button.Background);
            TestAssert.Same(System.Windows.Media.Brushes.White, button.Foreground);

            NavigationButtonThemeService.Apply(button, palette, selected: false);
            TestAssert.Same(System.Windows.Media.Brushes.Transparent, button.Background);
            TestAssert.Same(palette.SidebarMutedText, button.Foreground);
        });
    }
}

static class ThemeElementApplicationTests
{
    public static void AppliesCommonControls()
    {
        TestThread.RunSta(() =>
        {
            var palette = ThemePalette.Dark;

            var textBox = new System.Windows.Controls.TextBox();
            var handled = ThemeElementApplicationService.TryApply(textBox, palette, out var skipChildren);
            TestAssert.True(handled);
            TestAssert.False(skipChildren);
            TestAssert.Same(palette.Input, textBox.Background);
            TestAssert.Same(palette.Text, textBox.Foreground);
            TestAssert.Same(palette.Border, textBox.BorderBrush);

            var checkBox = new System.Windows.Controls.CheckBox();
            handled = ThemeElementApplicationService.TryApply(checkBox, palette, out skipChildren);
            TestAssert.True(handled);
            TestAssert.True(skipChildren);
            TestAssert.Same(palette.Input, checkBox.Background);
            TestAssert.Same(palette.MutedText, checkBox.BorderBrush);

            var accentText = new System.Windows.Controls.TextBlock { Tag = "Accent" };
            handled = ThemeElementApplicationService.TryApply(accentText, palette, out skipChildren);
            TestAssert.True(handled);
            TestAssert.False(skipChildren);
            TestAssert.Same(palette.Accent, accentText.Foreground);

            var staticBorder = new System.Windows.Controls.Border
            {
                Tag = "StaticBrush",
                Background = System.Windows.Media.Brushes.Red
            };
            handled = ThemeElementApplicationService.TryApply(staticBorder, palette, out _);
            TestAssert.True(handled);
            TestAssert.Same(System.Windows.Media.Brushes.Red, staticBorder.Background);
        });
    }
}

static class ColorConversionTests
{
    public static void ConvertsHexAndHsvValues()
    {
        var fallback = System.Windows.Media.Color.FromRgb(1, 2, 3);
        var parsed = ColorConversionService.ParseColor("14B8A6", fallback);

        TestAssert.Equal((byte)0x14, parsed.R);
        TestAssert.Equal((byte)0xB8, parsed.G);
        TestAssert.Equal((byte)0xA6, parsed.B);
        TestAssert.Equal("#14B8A6", ColorConversionService.ToHex(parsed));
        TestAssert.Equal(fallback, ColorConversionService.ParseColor("nope", fallback));

        var red = ColorConversionService.FromHsv(0, 1, 1);
        TestAssert.Equal("#FF0000", ColorConversionService.ToHex(red));

        var hsv = ColorConversionService.ToHsv(System.Windows.Media.Color.FromRgb(0, 255, 255));
        TestAssert.Equal(180d, Math.Round(hsv.Hue));
        TestAssert.Equal(1d, Math.Round(hsv.Saturation, 2));
        TestAssert.Equal(1d, Math.Round(hsv.Value, 2));
    }
}

static class UiVisibilityTests
{
    public static void TogglesMultipleElements()
    {
        TestThread.RunSta(() =>
        {
            var first = new System.Windows.Controls.TextBlock();
            var second = new System.Windows.Controls.Border();

            UiVisibilityService.SetVisible(false, first, second);
            TestAssert.Equal(System.Windows.Visibility.Collapsed, first.Visibility);
            TestAssert.Equal(System.Windows.Visibility.Collapsed, second.Visibility);

            UiVisibilityService.SetVisible(true, first, second);
            TestAssert.Equal(System.Windows.Visibility.Visible, first.Visibility);
            TestAssert.Equal(System.Windows.Visibility.Visible, second.Visibility);
        });
    }
}

static class OptionVisibilityTests
{
    public static void ResolvesRulePanels()
    {
        var visibility = OptionVisibilityService.ResolveRule(new RuleOptionVisibilityInput(
            TwitchEventKind.Cheer,
            ArduinoAvailable: true,
            UseLights: true,
            PlayAudio: true,
            AudioSourceMode.Single,
            HasAudioAssets: false,
            HasAudioGroups: false,
            SendChatMessage: true,
            AlexaAvailable: true,
            SendAlexaEvent: true,
            ObsAvailable: true,
            SendObsScene: true,
            SelectedObsSceneName: "Recortes",
            ReturnObsScene: true,
            HasObsScenes: true,
            SendObsMedia: true,
            ObsMediaKind.Image,
            MediaSourceMode.Single,
            HasObsMediaAssets: true,
            HasObsMediaGroups: false,
            LightPattern.Pulse));

        TestAssert.True(visibility.ShowMinimumBits);
        TestAssert.False(visibility.ShowRewardTitle);
        TestAssert.True(visibility.ShowAudioDetails);
        TestAssert.True(visibility.ShowAudioEmptyHint);
        TestAssert.True(visibility.ShowChatDetails);
        TestAssert.True(visibility.ShowAlexaDetails);
        TestAssert.True(visibility.ShowObsSceneTiming);
        TestAssert.False(visibility.ShowObsReturnDelay);
        TestAssert.True(visibility.ShowObsMediaDuration);
        TestAssert.True(visibility.ShowLightConfiguration);
        TestAssert.True(visibility.ShowSecondaryColor);
        TestAssert.True(visibility.ShowBrightness);
        TestAssert.False(visibility.ShowDuration);

        var videoVisibility = OptionVisibilityService.ResolveRule(new RuleOptionVisibilityInput(
            TwitchEventKind.Follow,
            ArduinoAvailable: true,
            UseLights: false,
            PlayAudio: false,
            AudioSourceMode.Single,
            HasAudioAssets: true,
            HasAudioGroups: false,
            SendChatMessage: false,
            AlexaAvailable: false,
            SendAlexaEvent: false,
            ObsAvailable: true,
            SendObsScene: false,
            SelectedObsSceneName: "",
            ReturnObsScene: false,
            HasObsScenes: true,
            SendObsMedia: true,
            ObsMediaKind.Video,
            MediaSourceMode.Group,
            HasObsMediaAssets: false,
            HasObsMediaGroups: true,
            LightPattern.Solid));

        TestAssert.False(videoVisibility.ShowObsMediaDuration);
    }

    public static void ResolvesBackgroundPanels()
    {
        var visible = OptionVisibilityService.ResolveBackground(new BackgroundOptionVisibilityInput(
            ArduinoAvailable: true,
            BackgroundEnabled: true,
            AlexaAvailable: true,
            AlexaEnabled: false,
            AlexaTurnOffAfterEvent: true,
            LightPattern.Rave));

        TestAssert.True(visible.ShowAlexaControls);
        TestAssert.False(visible.ShowAlexaUnavailable);
        TestAssert.True(visible.ShowAlexaEvents);
        TestAssert.True(visible.ShowArduinoBackground);
        TestAssert.True(visible.ShowColorOptions);
        TestAssert.True(visible.ShowBrightness);

        var unavailable = OptionVisibilityService.ResolveBackground(new BackgroundOptionVisibilityInput(
            ArduinoAvailable: false,
            BackgroundEnabled: true,
            AlexaAvailable: false,
            AlexaEnabled: false,
            AlexaTurnOffAfterEvent: false,
            LightPattern.Solid));

        TestAssert.False(unavailable.ShowArduinoBackground);
        TestAssert.True(unavailable.ShowAlexaUnavailable);
    }
}

static class UiAccentCatalogTests
{
    public static void MapsEventAndPatternColors()
    {
        TestAssert.Equal("#14B8A6", UiAccentCatalog.ForEventKind(TwitchEventKind.Follow));
        TestAssert.Equal("#FB923C", UiAccentCatalog.ForEventKind(TwitchEventKind.ChannelPointRedemption));
        TestAssert.Equal("#EC4899", UiAccentCatalog.ForLightPattern(LightPattern.Rave));
        TestAssert.Equal("#14B8A6", UiAccentCatalog.AudioSingle);
        TestAssert.Equal("#37C7F3", UiAccentCatalog.ObsImage);
    }
}

static class UiBrushFactoryTests
{
    public static void CreatesFrozenBrushes()
    {
        var brush = UiBrushFactory.FrozenBrushFrom("#14B8A6");
        var translucent = UiBrushFactory.TranslucentBrushFrom("#14B8A6");

        TestAssert.True(brush.IsFrozen);
        TestAssert.Equal((byte)0x14, brush.Color.R);
        TestAssert.True(translucent.IsFrozen);
        TestAssert.Equal((byte)0x22, translucent.Color.A);
        TestAssert.Equal((byte)0x14, translucent.Color.R);
    }
}

static class ThemeResourceTests
{
    public static void AppliesPaletteResources()
    {
        var resources = new System.Windows.ResourceDictionary();
        var palette = ThemePalette.Dark;

        ThemeResourceService.Apply(resources, palette);

        TestAssert.Same(palette.Window, resources["ThemeWindowBrush"]);
        TestAssert.Same(palette.Accent, resources["ThemeSelectionBrush"]);
        TestAssert.Same(palette.Accent, resources[System.Windows.SystemColors.HighlightBrushKey]);
    }
}

static class TwitchConnectionRecoveryTests
{
    public static void DetectsRecoverableRefreshErrors()
    {
        TestAssert.True(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("No pude refrescar Twitch: missing client secret")));
        TestAssert.True(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("No pude refrescar Twitch: invalid client")));
        TestAssert.True(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("No pude refrescar Twitch: invalid refresh token")));
        TestAssert.True(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("invalid refresh token")));
        TestAssert.False(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("Twitch no inicio el login")));
        TestAssert.False(TwitchConnectionRecoveryService.IsRecoverableRefreshError(new InvalidOperationException("No pude refrescar Twitch: timeout")));
    }
}

static class ServiceNavigationVisibilityTests
{
    public static void HidesOptionalServiceTabs()
    {
        var config = AppConfig.CreateDefault();

        var initial = ServiceNavigationVisibilityService.Resolve(config);
        TestAssert.False(initial.Lights);
        TestAssert.False(initial.Alexa);
        TestAssert.False(initial.Obs);
        TestAssert.False(initial.Images);
        TestAssert.False(initial.Videos);

        config.ArduinoEnabled = true;
        config.Alexa.Enabled = true;
        config.Obs.Enabled = true;

        var enabled = ServiceNavigationVisibilityService.Resolve(config);
        TestAssert.True(enabled.Lights);
        TestAssert.True(enabled.Alexa);
        TestAssert.True(enabled.Obs);
        TestAssert.True(enabled.Images);
        TestAssert.True(enabled.Videos);
    }
}

static class ShellViewModelTests
{
    public static void MapsNavigationVisibility()
    {
        var navigatedTo = -1;
        var shell = new ShellViewModel(UiTextService.CreateDefault(), tab =>
        {
            navigatedTo = tab;
            return true;
        });
        var config = AppConfig.CreateDefault();

        shell.ApplyServiceVisibility(config);

        TestAssert.False(shell.FindByIndex(ShellViewModel.LightsTabIndex)!.IsVisible);
        TestAssert.False(shell.FindByIndex(ShellViewModel.ObsTabIndex)!.IsVisible);

        config.ArduinoEnabled = true;
        config.Obs.Enabled = true;
        shell.ApplyServiceVisibility(config);
        shell.NavigateTo(ShellViewModel.ObsTabIndex);

        TestAssert.True(shell.FindByIndex(ShellViewModel.LightsTabIndex)!.IsVisible);
        TestAssert.True(shell.FindByIndex(ShellViewModel.ObsTabIndex)!.IsVisible);
        TestAssert.Equal(ShellViewModel.ObsTabIndex, navigatedTo);
        TestAssert.True(shell.FindByIndex(ShellViewModel.ObsTabIndex)!.IsSelected);
    }

    public static void MapsProfileAndLiveState()
    {
        var shell = new ShellViewModel(UiTextService.CreateDefault(), _ => true);

        shell.UpdateChannel("Dafovii", "@dafovii");
        shell.UpdateLiveIndicator(true, ThemePalette.Dark, "En directo", "Offline", "Perfil");

        TestAssert.Equal("Dafovii", shell.ChannelName);
        TestAssert.Equal("@dafovii", shell.ChannelLogin);
        TestAssert.Equal("En directo", shell.LiveStateText);
        TestAssert.Equal("Perfil", shell.TopProfileText);
        TestAssert.Equal((byte)0xFF, shell.LiveDotFill.Color.R);

        shell.UpdateLiveIndicator(false, ThemePalette.Light, "En directo", "Offline", "Perfil");

        TestAssert.Equal("Offline", shell.LiveStateText);
        TestAssert.Equal((byte)0x00, shell.LiveDotFill.Color.A);

        shell.UpdateServiceStatusText(
            twitchConnection: "Twitch conectado",
            twitchStatus: "En vivo",
            arduinoConnection: "Arduino conectado",
            arduinoStatus: "300 LEDs",
            alexaConnection: "Alexa lista",
            alexaSidebarStatus: "Fondo activo");

        TestAssert.Equal("Twitch conectado", shell.TwitchConnectionText);
        TestAssert.Equal("En vivo", shell.TwitchStatusText);
        TestAssert.Equal("Arduino conectado", shell.ArduinoConnectionText);
        TestAssert.Equal("300 LEDs", shell.ArduinoStatusText);
        TestAssert.Equal("Alexa lista", shell.AlexaConnectionText);
        TestAssert.Equal("Fondo activo", shell.AlexaSidebarStatusText);
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

static class TestThread
{
    public static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }
}
