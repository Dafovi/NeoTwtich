using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoTwitch.Shared;
using Forms = System.Windows.Forms;

namespace NeoTwitch.Installer;

public partial class MainWindow : Window
{
    private readonly InstallerOptions _options;
    private readonly InstallerService _installerService = new();
    private CancellationTokenSource _installCts = new();
    private int _step;
    private bool _isInstalling;
    private InstallResult? _installResult;

    public MainWindow()
    {
        _options = InstallerOptions.FromArgs(Environment.GetCommandLineArgs().Skip(1).ToArray());
        InitializeComponent();
        InstallPathBox.Text = _options.InstallPath;
        DesktopShortcutCheck.IsChecked = _options.CreateDesktopShortcut;
        StartMenuShortcutCheck.IsChecked = _options.CreateStartMenuShortcut;
        StartWithWindowsCheck.IsChecked = _options.StartWithWindows;
        LaunchAfterInstallCheck.IsChecked = _options.LaunchAfterInstall;
        OpenAfterCompleteCheck.IsChecked = _options.LaunchAfterInstall;

        if (_options.IsUpdate)
        {
            WelcomeTitleText.Text = "Actualización de Neo Twitch";
            InstallingTitleText.Text = "Actualizando Neo Twitch";
            CompleteTitleText.Text = "¡Actualización completada!";
            CompleteDescriptionText.Text = "Neo Twitch quedó actualizado y listo para abrirse de nuevo.";
            WelcomeInstallButton.Content = "Actualizar →";
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        VersionText.Text = string.IsNullOrWhiteSpace(_options.RequestedVersion)
            ? $"Versión {InstallerVersion.CurrentVersionText}"
            : $"Versión {_options.RequestedVersion}";

        if (_options.IsUpdate)
        {
            _step = 2;
            ShowStep();
            await BeginInstallAsync();
            return;
        }

        ShowStep();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling)
        {
            return;
        }

        Close();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Selecciona la carpeta donde se instalará Neo Twitch",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(InstallPathBox.Text) ? InstallPathBox.Text : InstallerOptions.DefaultInstallPath
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            InstallPathBox.Text = dialog.SelectedPath;
        }
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_step == 3)
        {
            Finish();
            return;
        }

        if (_step == 1)
        {
            await BeginInstallAsync();
            return;
        }

        _step++;
        if (_step == 1)
        {
            ReadOptionsFromUi();
        }

        ShowStep();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_step <= 0 || _isInstalling)
        {
            return;
        }

        _step--;
        ShowStep();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling)
        {
            _installCts.Cancel();
            StatusText.Text = "Cancelando...";
            return;
        }

        Close();
    }

    private async Task BeginInstallAsync()
    {
        ReadOptionsFromUi();
        _step = 2;
        _isInstalling = true;
        _installCts.Dispose();
        _installCts = new CancellationTokenSource();
        ShowStep();
        SetProgressSteps("Preparando instalación", "Copiando archivos", "Creando accesos directos", "Configurando componentes", "Finalizando instalación");

        try
        {
            var progress = new Progress<InstallProgress>(UpdateProgress);
            _installResult = await _installerService.InstallAsync(_options, progress, _installCts.Token);
            _step = 3;
            _isInstalling = false;
            OpenAfterCompleteCheck.IsChecked = _options.LaunchAfterInstall;
            UpdateReleaseNotes(_installResult.ReleaseNotes);
            ShowStep();
        }
        catch (OperationCanceledException)
        {
            _isInstalling = false;
            StatusText.Text = "Instalación cancelada.";
            _step = 1;
            ShowStep();
        }
        catch (Exception ex)
        {
            _isInstalling = false;
            StatusText.Text = ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Instalador Neo Twitch", MessageBoxButton.OK, MessageBoxImage.Warning);
            _step = _options.IsUpdate ? 0 : 1;
            ShowStep();
        }
    }

    private void Finish()
    {
        if (OpenAfterCompleteCheck.IsChecked == true && _installResult is not null && File.Exists(_installResult.AppExePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _installResult.AppExePath,
                WorkingDirectory = Path.GetDirectoryName(_installResult.AppExePath),
                UseShellExecute = true
            });
        }

        Close();
    }

    private void NotesButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.LatestReleaseUrl,
            UseShellExecute = true
        });
    }

    private void UpdateReleaseNotes(string? releaseNotes)
    {
        if (!_options.IsUpdate || string.IsNullOrWhiteSpace(releaseNotes))
        {
            ReleaseNotesPanel.Visibility = Visibility.Collapsed;
            ReleaseNotesText.Text = "";
            return;
        }

        var notes = SimplifyReleaseNotes(releaseNotes);
        ReleaseNotesText.Text = notes.Length > 900 ? $"{notes[..900]}..." : notes;
        ReleaseNotesPanel.Visibility = Visibility.Visible;
    }

    private static string SimplifyReleaseNotes(string releaseNotes)
    {
        var lines = releaseNotes
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line
                .TrimStart('#')
                .Trim()
                .Replace("`", "", StringComparison.Ordinal)
                .Replace("**", "", StringComparison.Ordinal));

        return string.Join(Environment.NewLine, lines);
    }

    private void ReadOptionsFromUi()
    {
        _options.InstallPath = string.IsNullOrWhiteSpace(InstallPathBox.Text)
            ? InstallerOptions.DefaultInstallPath
            : InstallPathBox.Text.Trim();
        _options.CreateDesktopShortcut = DesktopShortcutCheck.IsChecked == true;
        _options.CreateStartMenuShortcut = StartMenuShortcutCheck.IsChecked == true;
        _options.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _options.LaunchAfterInstall = LaunchAfterInstallCheck.IsChecked == true || OpenAfterCompleteCheck.IsChecked == true;
    }

    private void UpdateProgress(InstallProgress progress)
    {
        InstallProgressBar.Value = progress.Percent;
        ProgressPercentText.Text = $"{progress.Percent}%";
        ProgressMessageText.Text = progress.Message;
        StatusText.Text = progress.Message;
        SetProgressStepsStatus(progress.Percent);
    }

    private void SetProgressSteps(params string[] steps)
    {
        ProgressStepsList.ItemsSource = steps.Select(step => new TextBlock
        {
            Text = $"○ {step}",
            Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
            Margin = new Thickness(0, 3, 0, 3)
        });
    }

    private void SetProgressStepsStatus(int percent)
    {
        var completed = percent switch
        {
            >= 95 => 5,
            >= 80 => 4,
            >= 65 => 3,
            >= 35 => 2,
            >= 10 => 1,
            _ => 0
        };

        for (var i = 0; i < ProgressStepsList.Items.Count; i++)
        {
            if (ProgressStepsList.Items[i] is TextBlock textBlock)
            {
                var cleanText = textBlock.Text.Length > 2 ? textBlock.Text[2..] : textBlock.Text;
                textBlock.Text = $"{(i < completed ? "✓" : "○")} {cleanText}";
                textBlock.Foreground = i < completed
                    ? (System.Windows.Media.Brush)FindResource("AccentBrightBrush")
                    : (System.Windows.Media.Brush)FindResource("MutedBrush");
            }
        }
    }

    private void ShowStep()
    {
        WelcomePanel.Visibility = _step == 0 ? Visibility.Visible : Visibility.Collapsed;
        OptionsPanel.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        InstallingPanel.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        CompletePanel.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;

        FooterPanel.Visibility = _step == 0 || _step == 2 ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = _step == 3 ? Visibility.Collapsed : Visibility.Visible;
        NotesButton.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.IsEnabled = !_isInstalling;
        NextButton.Content = _step switch
        {
            1 => _options.IsUpdate ? "Actualizar ahora →" : "Instalar ahora →",
            3 => "Finalizar →",
            _ => "Instalar ahora →"
        };

        if (_step == 3)
        {
            StatusText.Text = _options.IsUpdate ? "Actualización completada." : "Instalación completada.";
        }
    }
}
