using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateCloseBehaviorCards()
    {
        if (_initializingComponent)
        {
            return;
        }

        var closeToTray = CloseToTrayCheck.IsChecked == true;
        if (CloseToTrayRadio.IsChecked != closeToTray)
        {
            CloseToTrayRadio.IsChecked = closeToTray;
        }

        if (CloseAppRadio.IsChecked != !closeToTray)
        {
            CloseAppRadio.IsChecked = !closeToTray;
        }

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        ApplyCloseBehaviorCardTheme(CloseToTrayCard, closeToTray, palette);
        ApplyCloseBehaviorCardTheme(CloseAppCard, !closeToTray, palette);
    }

    private static void ApplyCloseBehaviorCardTheme(Border card, bool selected, ThemePalette palette)
    {
        card.Background = selected
            ? TranslucentBrushFrom("#14B8A6")
            : palette.Input;
        card.BorderBrush = selected
            ? palette.Accent
            : palette.Border;
    }

    private void ApplyTheme()
    {
        _config.DarkMode = ResolveDarkMode(_config.ThemeMode);
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        Background = palette.Window;
        Resources["ThemeWindowBrush"] = palette.Window;
        Resources["ThemeSidebarBrush"] = palette.Sidebar;
        Resources["ThemeSurfaceBrush"] = palette.Surface;
        Resources["ThemeButtonBrush"] = palette.Button;
        Resources["ThemeTextBrush"] = palette.Text;
        Resources["ThemeMutedTextBrush"] = palette.MutedText;
        Resources["ThemeSidebarTextBrush"] = palette.SidebarText;
        Resources["ThemeSidebarMutedTextBrush"] = palette.SidebarMutedText;
        Resources["ThemeInputBrush"] = palette.Input;
        Resources["ThemeBorderBrush"] = palette.Border;
        Resources["ThemeSelectionBrush"] = palette.Accent;
        Resources["ThemeConsoleBrush"] = palette.Console;
        Resources["ThemeScrollThumbBrush"] = palette.Accent;
        Resources["ThemeScrollTrackBrush"] = palette.ScrollTrack;
        Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        ApplyWindowChromeColor();
        UpdateNavigationButtons();
        ApplyBackgroundOutputMode();
        ApplyThemeToElement(this, palette);
        ApplyBackgroundOutputMode();
        UpdateTwitchLiveIndicator();
        UpdateDashboardSummary();
        UpdateColorButtons();
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateBackgroundPatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        UpdateAudioFilterButtons();
        UpdateMediaFilterButtons(MediaLibraryKind.Image);
        UpdateMediaFilterButtons(MediaLibraryKind.Video);
        UpdateCloseBehaviorCards();
    }

    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        var skipChildren = false;

        switch (element)
        {
            case Border border when border.TemplatedParent is not null:
                break;
            case Border border when string.Equals(border.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case Border border when border.DataContext is ActivityLogEntry:
                break;
            case Border border:
                border.BorderBrush = palette.Border;
                if (IsSidebarBorder(border))
                {
                    border.Background = palette.Sidebar;
                    break;
                }

                if (IsTitleBarBorder(border))
                {
                    border.Background = palette.Window;
                    border.BorderBrush = palette.Border;
                    break;
                }

                if (IsConsoleBorder(border))
                {
                    border.Background = palette.Console;
                    break;
                }

                if (IsInsideNamedElement(border, "SidebarChrome"))
                {
                    border.Background = palette.SidebarCard;
                    border.BorderBrush = palette.SidebarCardBorder;
                    break;
                }

                border.Background = palette.Surface;
                break;
            case TextBlock textBlock when textBlock.DataContext is ActivityLogEntry:
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Accent", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = palette.Accent;
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Success", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = FrozenBrushFrom("#22C55E");
                break;
            case TextBlock textBlock:
                if (IsInsideNamedElement(textBlock, "SidebarChrome"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                        ? palette.SidebarMutedText
                        : palette.SidebarText;
                    break;
                }

                if (IsInsideNamedElement(textBlock, "MiniConsolePanel"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12
                        ? palette.ConsoleMutedText
                        : System.Windows.Media.Brushes.White;
                    break;
                }

                textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                    ? palette.MutedText
                    : palette.Text;
                break;
            case System.Windows.Controls.TextBox textBox:
                textBox.Background = palette.Input;
                textBox.Foreground = palette.Text;
                textBox.BorderBrush = palette.Border;
                textBox.CaretBrush = palette.Text;
                break;
            case System.Windows.Controls.ComboBox comboBox:
                comboBox.Background = palette.Input;
                comboBox.Foreground = palette.Text;
                comboBox.BorderBrush = palette.Border;
                comboBox.Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
                comboBox.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
                break;
            case System.Windows.Controls.ListBox listBox:
                if (IsInsideNamedElement(listBox, "MiniConsolePanel"))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = System.Windows.Media.Brushes.White;
                    listBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    break;
                }

                if (IsActivityFeedListBox(listBox))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = palette.Text;
                    listBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    break;
                }

                listBox.Background = palette.Input;
                listBox.Foreground = palette.Text;
                listBox.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.TabControl tabControl:
                tabControl.Background = System.Windows.Media.Brushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                break;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.CheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                break;
            case Slider slider:
                slider.Foreground = palette.Accent;
                break;
            case System.Windows.Controls.Button button when IsColorButton(button):
                button.BorderBrush = palette.Border;
                skipChildren = true;
                break;
            case ToggleButton toggleButton when IsRuleStatusFilterButton(toggleButton):
                ApplyRuleStatusFilterButtonTheme(toggleButton, palette);
                skipChildren = true;
                break;
            case ToggleButton toggleButton:
                ApplyActivityFilterButtonTheme(toggleButton, palette);
                skipChildren = true;
                break;
            case System.Windows.Controls.Button button:
                ApplyButtonTheme(button, palette);
                skipChildren = true;
                break;
        }

        if (skipChildren)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            ApplyThemeToElement(VisualTreeHelper.GetChild(element, i), palette);
        }
    }

    private void ApplyButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        if (TryFindResource("NavButton") is Style navButtonStyle &&
            ReferenceEquals(button.Style, navButtonStyle))
        {
            ApplyNavigationButtonTheme(button, palette);
            return;
        }

        if (IsWindowControlButton(button))
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
            button.Foreground = palette.MutedText;
            button.BorderBrush = System.Windows.Media.Brushes.Transparent;
            return;
        }

        if (TryFindResource("PrimaryButton") is Style primaryButtonStyle &&
            ReferenceEquals(button.Style, primaryButtonStyle))
        {
            button.Background = palette.Accent;
            button.Foreground = System.Windows.Media.Brushes.White;
            button.BorderBrush = palette.Accent;
            return;
        }

        if (TryFindResource("DangerButton") is Style dangerButtonStyle &&
            ReferenceEquals(button.Style, dangerButtonStyle))
        {
            button.Background = palette.DangerSurface;
            button.Foreground = palette.DangerText;
            button.BorderBrush = palette.DangerBorder;
            return;
        }

        button.Background = palette.Button;
        button.Foreground = palette.Text;
        button.BorderBrush = palette.Border;
    }

    private void ApplyRuleStatusFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var active = button.IsChecked == true;
        var accentColor = button.Tag?.ToString() switch
        {
            "ACTIVE" => "#22C55E",
            "INACTIVE" => "#94A3B8",
            _ => "#14B8A6"
        };
        var accent = FrozenBrushFrom(accentColor);

        button.Background = active
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = active
            ? accent
            : palette.MutedText;
        button.BorderBrush = active
            ? accent
            : palette.Border;
    }

    private static bool IsRuleStatusFilterButton(ToggleButton button)
    {
        return button.Name.StartsWith("RuleFilter", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ToggleButton> RuleStatusFilterButtons()
    {
        return
        [
            RuleFilterAllButton,
            RuleFilterActiveButton,
            RuleFilterInactiveButton
        ];
    }

    private void UpdateEventKindTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedKind = EventKindBox.SelectedValue is TwitchEventKind kind
            ? kind
            : TwitchEventKind.Follow;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in EventKindTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<TwitchEventKind>(value, out var tileKind))
            {
                continue;
            }

            var selected = tileKind == selectedKind;
            var accentColor = EventKindAccent(tileKind);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = selected
                ? TranslucentBrushFrom(accentColor)
                : palette.Input;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : palette.Text;
        }
    }

    private IEnumerable<System.Windows.Controls.Button> EventKindTileButtons()
    {
        return
        [
            EventFollowTileButton,
            EventSubscriptionTileButton,
            EventRaidTileButton,
            EventCheerTileButton,
            EventChatCommandTileButton,
            EventRedemptionTileButton
        ];
    }

    private static string EventKindAccent(TwitchEventKind kind)
    {
        return kind switch
        {
            TwitchEventKind.Follow => "#14B8A6",
            TwitchEventKind.Subscription => "#B56CFF",
            TwitchEventKind.Raid => "#F43F5E",
            TwitchEventKind.Cheer => "#37C7F3",
            TwitchEventKind.ChatCommand => "#22C55E",
            TwitchEventKind.ChannelPointRedemption => "#FB923C",
            _ => "#94A3B8"
        };
    }

    private void UpdatePatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = PatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Pulse;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        var tileBackground = _config.DarkMode
            ? palette.Input
            : FrozenBrushFrom("#10202A");
        var tileForeground = _config.DarkMode
            ? palette.Text
            : FrozenBrushFrom("#F8FAFC");

        foreach (var button in PatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            var accentColor = PatternAccent(tilePattern);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = tileBackground;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : tileForeground;
        }
    }

    private void UpdateRuleAudioModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        ApplyRuleAudioModeButtonTheme(RuleSingleAudioModeButton, _ruleAudioMode == AudioSourceMode.Single, "#14B8A6", palette);
        ApplyRuleAudioModeButtonTheme(RuleGroupAudioModeButton, _ruleAudioMode == AudioSourceMode.Group, "#B56CFF", palette);
    }

    private static void ApplyRuleAudioModeButtonTheme(System.Windows.Controls.Button button, bool active, string accentColor, ThemePalette palette)
    {
        button.Background = active ? TranslucentBrushFrom(accentColor) : palette.Input;
        button.Foreground = active ? FrozenBrushFrom(accentColor) : palette.Text;
        button.BorderBrush = active ? FrozenBrushFrom(accentColor) : palette.Border;
    }

    private void UpdateRuleObsMediaModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        var mediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind kind
            ? kind
            : ObsMediaKind.Image;
        var sourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode mode
            ? mode
            : MediaSourceMode.Single;

        ApplyRuleAudioModeButtonTheme(RuleObsImageModeButton, mediaKind == ObsMediaKind.Image, "#37C7F3", palette);
        ApplyRuleAudioModeButtonTheme(RuleObsVideoModeButton, mediaKind == ObsMediaKind.Video, "#B56CFF", palette);
        ApplyRuleAudioModeButtonTheme(RuleObsSingleMediaModeButton, sourceMode == MediaSourceMode.Single, "#14B8A6", palette);
        ApplyRuleAudioModeButtonTheme(RuleObsGroupMediaModeButton, sourceMode == MediaSourceMode.Group, "#22C55E", palette);
    }

    private IEnumerable<System.Windows.Controls.Button> PatternTileButtons()
    {
        return
        [
            PatternSolidTileButton,
            PatternPulseTileButton,
            PatternRainbowTileButton,
            PatternChaseTileButton,
            PatternTheaterTileButton,
            PatternSparkleTileButton,
            PatternRaveTileButton
        ];
    }

    private static string PatternAccent(LightPattern pattern)
    {
        return pattern switch
        {
            LightPattern.Solid => "#14B8A6",
            LightPattern.Pulse => "#B56CFF",
            LightPattern.Rainbow => "#37C7F3",
            LightPattern.Chase => "#22C55E",
            LightPattern.Theater => "#F59E0B",
            LightPattern.Sparkle => "#FACC15",
            LightPattern.Rave => "#EC4899",
            _ => "#94A3B8"
        };
    }

    private void UpdateBackgroundPatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Solid;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        var tileBackground = _config.DarkMode
            ? palette.Input
            : FrozenBrushFrom("#10202A");
        var tileForeground = _config.DarkMode
            ? palette.Text
            : FrozenBrushFrom("#F8FAFC");

        foreach (var button in BackgroundPatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            var accentColor = PatternAccent(tilePattern);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = tileBackground;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : tileForeground;
        }
    }

    private IEnumerable<System.Windows.Controls.Button> BackgroundPatternTileButtons()
    {
        return
        [
            BackgroundPatternSolidTileButton,
            BackgroundPatternPulseTileButton,
            BackgroundPatternRainbowTileButton,
            BackgroundPatternChaseTileButton,
            BackgroundPatternTheaterTileButton,
            BackgroundPatternSparkleTileButton,
            BackgroundPatternRaveTileButton
        ];
    }

    private void UpdateNavigationButtons()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateServiceNavigationVisibility();

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in new[] { NavSettingsButton, NavConnectionsButton, NavRulesButton, NavStripsButton, NavAlexaButton, NavAudioButton, NavImagesButton, NavVideosButton, NavObsButton, NavPreferencesButton, NavActivityButton })
        {
            ApplyNavigationButtonTheme(button, palette);
        }
    }

    private void UpdateServiceNavigationVisibility()
    {
        if (_initializingComponent)
        {
            return;
        }

        SetNavigationTargetVisible(NavStripsButton, LightsTab, _config.ArduinoEnabled);
        SetNavigationTargetVisible(NavAlexaButton, AlexaTab, _config.Alexa.Enabled);
        SetNavigationTargetVisible(NavObsButton, ObsTab, _config.Obs.Enabled);
        SetNavigationTargetVisible(NavImagesButton, ImagesTab, _config.Obs.Enabled);
        SetNavigationTargetVisible(NavVideosButton, VideosTab, _config.Obs.Enabled);

        if (MainTabs.SelectedItem is TabItem { Visibility: not Visibility.Visible })
        {
            MainTabs.SelectedItem = ConnectionsTab;
        }
    }

    private static void SetNavigationTargetVisible(
        System.Windows.Controls.Button button,
        TabItem tab,
        bool isVisible)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        button.Visibility = visibility;
        tab.Visibility = visibility;
    }

    private void ApplyNavigationButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        var isSelected = int.TryParse(button.Tag?.ToString(), out var index)
            && index == MainTabs.SelectedIndex;

        button.Background = isSelected
            ? palette.NavSelected
            : System.Windows.Media.Brushes.Transparent;
        button.Foreground = isSelected
            ? System.Windows.Media.Brushes.White
            : palette.SidebarMutedText;
        button.BorderBrush = System.Windows.Media.Brushes.Transparent;
    }

    private static bool IsColorButton(System.Windows.Controls.Button button)
    {
        return !string.IsNullOrWhiteSpace(button.Name)
            && button.Name.EndsWith("ColorButton", StringComparison.OrdinalIgnoreCase);
    }

    private static SolidColorBrush TranslucentBrushFrom(string accentColor)
    {
        return accentColor.StartsWith('#') && accentColor.Length == 7
            ? FrozenBrushFrom($"#22{accentColor[1..]}")
            : FrozenBrushFrom("#2200C7B7");
    }

    private static bool IsActivityFeedListBox(System.Windows.Controls.ListBox listBox)
    {
        return string.Equals(listBox.Name, "ActivityList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(listBox.Name, "DashboardActivityList", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWindowControlButton(System.Windows.Controls.Button button)
    {
        return string.Equals(button.Name, "MinimizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "MaximizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "CloseWindowButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidebarBorder(Border border)
    {
        return string.Equals(border.Name, "SidebarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTitleBarBorder(Border border)
    {
        return string.Equals(border.Name, "TitleBarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsoleBorder(Border border)
    {
        return string.Equals(border.Name, "MiniConsolePanel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideNamedElement(DependencyObject element, string name)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement frameworkElement
                && string.Equals(frameworkElement.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
