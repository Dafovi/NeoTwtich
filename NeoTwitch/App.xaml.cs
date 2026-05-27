using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NeoTwitch.Services;

namespace NeoTwitch;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private bool _exceptionLoggingRegistered;

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterExceptionLogging();

        try
        {
            _singleInstanceMutex = new Mutex(true, "NeoTwitch.SingleInstance", out var createdNew);
            _ownsMutex = createdNew;
            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Neo Twitch ya esta abierta. Revisa el icono en la bandeja del sistema.",
                    "Neo Twitch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            var logPath = CrashReporter.Log(ex, "Fallo al iniciar la aplicacion.");
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
        var logPath = CrashReporter.Log(e.Exception, "Fallo no controlado en la interfaz.");
        ShowFatalError(logPath, e.Exception.Message);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            CrashReporter.Log(exception, "Fallo no controlado del proceso.");
        }
        else
        {
            CrashReporter.LogMessage($"Fallo no controlado del proceso: {e.ExceptionObject}");
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashReporter.Log(e.Exception, "Fallo no observado en una tarea en segundo plano.");
        e.SetObserved();
    }

    private static void ShowFatalError(string logPath, string detail)
    {
        System.Windows.MessageBox.Show(
            $"La app no pudo iniciar correctamente.\n\nDetalle: {detail}\n\nLog: {logPath}",
            "Neo Twitch",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
