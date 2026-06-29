using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void InitializeRulesBinding()
    {
        _rulesViewSource.Source = _config.Rules;
        _rulesViewSource.Filter += RulesViewSource_Filter;
        RulesList.ItemsSource = _rulesViewSource.View;
    }

    private void InitializeRuleOptionSources()
    {
        EventKindBox.ItemsSource = _eventOptions;
        EventKindBox.DisplayMemberPath = nameof(UiOption<TwitchEventKind>.Label);
        EventKindBox.SelectedValuePath = nameof(UiOption<TwitchEventKind>.Value);

        RuleAudioAssetBox.ItemsSource = _config.AudioLibrary;
        RuleAudioAssetBox.DisplayMemberPath = nameof(AudioAssetConfig.DisplayName);
        RuleAudioAssetBox.SelectedValuePath = nameof(AudioAssetConfig.Id);

        RuleAudioGroupBox.ItemsSource = _config.AudioGroups;
        RuleAudioGroupBox.DisplayMemberPath = nameof(AudioGroupConfig.Name);
        RuleAudioGroupBox.SelectedValuePath = nameof(AudioGroupConfig.Id);

        RuleObsSceneBox.ItemsSource = _obsSceneChoices;
        RuleObsSceneBox.DisplayMemberPath = nameof(ObsSceneChoice.Label);
        RuleObsSceneBox.SelectedValuePath = nameof(ObsSceneChoice.Name);

        RuleObsMediaKindBox.ItemsSource = _obsMediaKindOptions;
        RuleObsMediaKindBox.DisplayMemberPath = nameof(UiOption<ObsMediaKind>.Label);
        RuleObsMediaKindBox.SelectedValuePath = nameof(UiOption<ObsMediaKind>.Value);

        RuleObsMediaSourceModeBox.ItemsSource = _mediaSourceModeOptions;
        RuleObsMediaSourceModeBox.DisplayMemberPath = nameof(UiOption<MediaSourceMode>.Label);
        RuleObsMediaSourceModeBox.SelectedValuePath = nameof(UiOption<MediaSourceMode>.Value);

        RuleObsMediaAssetBox.DisplayMemberPath = nameof(MediaAssetConfig.DisplayName);
        RuleObsMediaAssetBox.SelectedValuePath = nameof(MediaAssetConfig.Id);
        RuleObsMediaGroupBox.DisplayMemberPath = nameof(MediaGroupConfig.Name);
        RuleObsMediaGroupBox.SelectedValuePath = nameof(MediaGroupConfig.Id);
    }

    private void InitializeLibraryOptionSources()
    {
        NewAudioAlertBox.ItemsSource = AudioAlertChoices;
        NewAudioAlertBox.DisplayMemberPath = nameof(AudioAlertChoice.Name);
        NewAudioAlertBox.SelectedValuePath = nameof(AudioAlertChoice.Id);

        NewAudioGroupBox.ItemsSource = AudioGroupChoices;
        NewAudioGroupBox.DisplayMemberPath = nameof(AudioGroupChoice.Name);
        NewAudioGroupBox.SelectedValuePath = nameof(AudioGroupChoice.Id);

        NewImageGroupBox.ItemsSource = ImageGroupChoices;
        NewImageGroupBox.DisplayMemberPath = nameof(MediaGroupChoice.Name);
        NewImageGroupBox.SelectedValuePath = nameof(MediaGroupChoice.Id);

        NewVideoGroupBox.ItemsSource = VideoGroupChoices;
        NewVideoGroupBox.DisplayMemberPath = nameof(MediaGroupChoice.Name);
        NewVideoGroupBox.SelectedValuePath = nameof(MediaGroupChoice.Id);
    }

    private void InitializeBackgroundOptionSources()
    {
        PatternBox.ItemsSource = _patternOptions;
        PatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
        PatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);

        BackgroundPatternBox.ItemsSource = _patternOptions;
        BackgroundPatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
        BackgroundPatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);
    }

    private void InitializeConnectionOptionSources()
    {
        ThemeModeBox.ItemsSource = _themeModeOptions;
        ThemeModeBox.DisplayMemberPath = nameof(UiOption<string>.Label);
        ThemeModeBox.SelectedValuePath = nameof(UiOption<string>.Value);

        StripsList.ItemsSource = _config.LedStrips;
        PortComboBox.DisplayMemberPath = nameof(SerialPortInfo.DisplayName);
        PortComboBox.SelectedValuePath = nameof(SerialPortInfo.PortName);
    }
}
