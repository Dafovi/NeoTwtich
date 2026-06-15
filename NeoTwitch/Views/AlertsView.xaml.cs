using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class AlertsView : NeoTwitchView
{
    public AlertsView()
    {
        InitializeComponent();
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e) => Host?.AddRuleButton_Click(sender, e);

    private void RuleSearchBox_TextChanged(object sender, TextChangedEventArgs e) => Host?.RuleSearchBox_TextChanged(sender, e);

    private void RuleStatusFilterButton_Click(object sender, RoutedEventArgs e) => Host?.RuleStatusFilterButton_Click(sender, e);

    private void RuleCategoryFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.RuleCategoryFilterBox_SelectionChanged(sender, e);

    private void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.RulesList_SelectionChanged(sender, e);

    private void DuplicateRuleButton_Click(object sender, RoutedEventArgs e) => Host?.DuplicateRuleButton_Click(sender, e);

    private void RuleTestButton_Click(object sender, RoutedEventArgs e) => Host?.RuleTestButton_Click(sender, e);

    private void SaveRuleButton_Click(object sender, RoutedEventArgs e) => Host?.SaveRuleButton_Click(sender, e);

    private void RuleFieldChanged(object sender, RoutedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, TextChangedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, SelectionChangedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Host?.RuleFieldChanged(sender, e);

    private void EventKindTile_Click(object sender, RoutedEventArgs e) => Host?.EventKindTile_Click(sender, e);

    private void PatternTile_Click(object sender, RoutedEventArgs e) => Host?.PatternTile_Click(sender, e);

    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.PrimaryColorButton_Click(sender, e);

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.SecondaryColorButton_Click(sender, e);

    private void TertiaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.TertiaryColorButton_Click(sender, e);

    private void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => Host?.RuleLedPreviewPanel_IsVisibleChanged(sender, e);

    private void RuleAudioModeButton_Click(object sender, RoutedEventArgs e) => Host?.RuleAudioModeButton_Click(sender, e);

    private void RuleObsMediaKindButton_Click(object sender, RoutedEventArgs e) => Host?.RuleObsMediaKindButton_Click(sender, e);

    private void RuleObsMediaSourceModeButton_Click(object sender, RoutedEventArgs e) => Host?.RuleObsMediaSourceModeButton_Click(sender, e);

    private void RemoveRuleButton_Click(object sender, RoutedEventArgs e) => Host?.RemoveRuleButton_Click(sender, e);
}
