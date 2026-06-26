using System.Windows.Input;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Dashboard;

public sealed class DashboardViewModel
{
    public DashboardViewModel(Action goToActivity)
    {
        GoToActivityCommand = new RelayCommand(goToActivity);
    }

    public ICommand GoToActivityCommand { get; }
}
