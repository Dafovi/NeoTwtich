using System.ComponentModel;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Alerts;

public sealed class EventRuleRowViewModel : ObservableObject, IDisposable
{
    private readonly IUiTextService _text;

    public EventRuleRowViewModel(EventRule rule, IUiTextService text)
    {
        Rule = rule;
        _text = text;
        Rule.PropertyChanged += Rule_PropertyChanged;
    }

    public EventRule Rule { get; }

    public string Name => Rule.Name;

    public string DisplayLabel => EventRulePresentationService.BuildDisplayLabel(Rule, _text);

    public string StatusText => EventRulePresentationService.BuildStatusText(Rule, _text);

    public string StatusColor => EventRulePresentationService.BuildStatusColor(Rule);

    public string EventIconPath => EventRulePresentationService.BuildEventIconPath(Rule);

    public string EventAccentColor => EventRulePresentationService.BuildEventAccentColor(Rule);

    public string ActionsSummary => EventRulePresentationService.BuildActionsSummary(Rule, _text);

    public Visibility LightsActionVisibility => Rule.UseLights ? Visibility.Visible : Visibility.Collapsed;

    public double LightsActionOpacity => Rule.LightsActionAvailable ? 1d : 0.32d;

    public string LightsActionToolTip => EventRulePresentationService.BuildLightsToolTip(Rule, _text);

    public Visibility AudioActionVisibility => Rule.PlayAudio ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ChatActionVisibility => Rule.SendChatMessage ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AlexaActionVisibility => Rule.SendAlexaEvent ? Visibility.Visible : Visibility.Collapsed;

    public double AlexaActionOpacity => Rule.AlexaActionAvailable ? 1d : 0.32d;

    public string AlexaActionToolTip => EventRulePresentationService.BuildAlexaToolTip(Rule, _text);

    public Visibility ObsActionVisibility => Rule.SendObsScene || Rule.SendObsMedia ? Visibility.Visible : Visibility.Collapsed;

    public double ObsActionOpacity => Rule.ObsActionAvailable ? 1d : 0.32d;

    public string ObsActionToolTip => EventRulePresentationService.BuildObsToolTip(Rule, _text);

    public void Dispose()
    {
        Rule.PropertyChanged -= Rule_PropertyChanged;
    }

    private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaisePresentationChanged();
    }

    private void RaisePresentationChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(DisplayLabel));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(EventIconPath));
        OnPropertyChanged(nameof(EventAccentColor));
        OnPropertyChanged(nameof(ActionsSummary));
        OnPropertyChanged(nameof(LightsActionVisibility));
        OnPropertyChanged(nameof(LightsActionOpacity));
        OnPropertyChanged(nameof(LightsActionToolTip));
        OnPropertyChanged(nameof(AudioActionVisibility));
        OnPropertyChanged(nameof(ChatActionVisibility));
        OnPropertyChanged(nameof(AlexaActionVisibility));
        OnPropertyChanged(nameof(AlexaActionOpacity));
        OnPropertyChanged(nameof(AlexaActionToolTip));
        OnPropertyChanged(nameof(ObsActionVisibility));
        OnPropertyChanged(nameof(ObsActionOpacity));
        OnPropertyChanged(nameof(ObsActionToolTip));
    }
}
