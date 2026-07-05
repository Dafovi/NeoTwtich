using System.Windows;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Shell;

public sealed class NavigationItemViewModel : ObservableObject
{
    private bool _isSelected;
    private bool _isVisible = true;

    public NavigationItemViewModel(
        int tabIndex,
        string key,
        string label,
        string iconPath,
        string tooltip)
    {
        TabIndex = tabIndex;
        Key = key;
        Label = label;
        IconPath = iconPath;
        Tooltip = tooltip;
    }

    public int TabIndex { get; }

    public string Key { get; }

    public string Label { get; }

    public string IconPath { get; }

    public string Tooltip { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
            {
                OnPropertyChanged(nameof(Visibility));
            }
        }
    }

    public Visibility Visibility => IsVisible ? Visibility.Visible : Visibility.Collapsed;
}
