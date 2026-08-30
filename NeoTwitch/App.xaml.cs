using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;

namespace NeoTwitch;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static readonly IUiTextService Text = UiTextService.CreateDefault();
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private bool _exceptionLoggingRegistered;

    protected override async void OnStartup(StartupEventArgs e)
    {
        RegisterExceptionLogging();
        AppServices? services = null;

        try
        {
            _singleInstanceMutex = new Mutex(true, NeoTwitchProduct.SingleInstanceMutexName, out var createdNew);
            _ownsMutex = createdNew;
            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    Text.Format(UiTextKeys.AppAlreadyOpenMessage, NeoTwitchProduct.DisplayName),
                    NeoTwitchProduct.DisplayName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            var startupOptions = AppStartupOptions.Parse(e.Args);
            services = AppServices.CreateDefault();
            var window = new MainWindow(startupOptions, services);
            window.Show();
        }
        catch (Exception ex)
        {
            if (services is not null)
            {
                await services.DisposeAsync();
            }

            var logPath = CrashReporter.Log(ex, Text.Get(UiTextKeys.AppStartupFailureLog));
            ShowFatalError(logPath, ex.Message);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void RegisterExceptionLogging()
    {
        if (_exceptionLoggingRegistered)
        {
            return;
        }

        _exceptionLoggingRegistered = true;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = CrashReporter.Log(e.Exception, Text.Get(UiTextKeys.AppUiUnhandledLog));
        ShowFatalError(logPath, e.Exception.Message);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            CrashReporter.Log(exception, Text.Get(UiTextKeys.AppProcessUnhandledLog));
        }
        else
        {
            CrashReporter.LogMessage(Text.Format(UiTextKeys.AppProcessUnhandledObjectLog, e.ExceptionObject ?? ""));
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashReporter.Log(e.Exception, Text.Get(UiTextKeys.AppBackgroundTaskUnhandledLog));
        e.SetObserved();
    }

    private static void ShowFatalError(string logPath, string detail)
    {
        System.Windows.MessageBox.Show(
            Text.Format(UiTextKeys.AppFatalErrorMessage, detail, logPath),
            NeoTwitchProduct.DisplayName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
