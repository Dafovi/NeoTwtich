using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using static NeoTwitch.Services.Ui.UiBrushFactory;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void ImageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _imageSearchText = textBox.Text.Trim();
        RefreshMediaLibraryView(MediaLibraryKind.Image);
    }

    internal void VideoSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _videoSearchText = textBox.Text.Trim();
        RefreshMediaLibraryView(MediaLibraryKind.Video);
    }

    internal void VideoVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingUi)
        {
            return;
        }

        _config.VideoVolumePercent = (int)Math.Round(VideoVolumeSlider.Value);
        UpdateVideoVolumeText();
        SaveConfig();
    }

    internal void ImageFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SetMediaFilter(MediaLibraryKind.Image, sender);
    }

    internal void VideoFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SetMediaFilter(MediaLibraryKind.Video, sender);
    }

    private void SetMediaFilter(MediaLibraryKind kind, object sender)
    {
        if (sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageFilter = button.Tag?.ToString() ?? "ALL";
            _imageGroupFilterId = "";
        }
        else
        {
            _videoFilter = button.Tag?.ToString() ?? "ALL";
            _videoGroupFilterId = "";
        }

        UpdateMediaFilterButtons(kind);
        RefreshMediaLibraryView(kind);
    }

    internal void ImageLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        UpdateMediaAssetGroup(MediaLibraryKind.Image, sender);
    }

    internal void VideoLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        UpdateMediaAssetGroup(MediaLibraryKind.Video, sender);
    }

    private void UpdateMediaAssetGroup(MediaLibraryKind kind, object sender)
    {
        if ((kind == MediaLibraryKind.Image && _refreshingImageLibrary)
            || (kind == MediaLibraryKind.Video && _refreshingVideoLibrary)
            || _loadingUi
            || sender is not System.Windows.Controls.ComboBox comboBox
            || comboBox.Tag is not string assetId)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var selectedGroupId = comboBox.SelectedValue as string ?? "";
        if (string.Equals(asset.GroupId, selectedGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        asset.GroupId = selectedGroupId;
        SaveConfig();
        _ = Dispatcher.InvokeAsync(() => RefreshMediaLibraryView(kind), DispatcherPriority.Background);
    }

    internal void BrowseNewImageButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseNewMedia(MediaLibraryKind.Image);
    }

    internal void BrowseNewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseNewMedia(MediaLibraryKind.Video);
    }

    private void BrowseNewMedia(MediaLibraryKind kind)
    {
        var dialog = new WpfOpenFileDialog
        {
            Filter = MediaLibraryKindCatalog.Get(kind).FileDialogFilter,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _newImagePath = dialog.FileName;
            NewImagePathBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NewImageNameBox.Text))
            {
                NewImageNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
        else
        {
            _newVideoPath = dialog.FileName;
            NewVideoPathBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NewVideoNameBox.Text))
            {
                NewVideoNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    internal void SaveNewImageButton_Click(object sender, RoutedEventArgs e)
    {
        SaveNewMedia(MediaLibraryKind.Image);
    }

    internal void SaveNewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        SaveNewMedia(MediaLibraryKind.Video);
    }

    private void SaveNewMedia(MediaLibraryKind kind)
    {
        var path = kind == MediaLibraryKind.Image ? _newImagePath : _newVideoPath;
        var title = MediaLibraryTitle(kind);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            WpfMessageBox.Show(this, _text.Format(UiTextKeys.MediaPickValidFile, title.ToLowerInvariant()), title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var library = GetMediaLibrary(kind);
        var existing = library.FirstOrDefault(asset => string.Equals(asset.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var asset = existing ?? new MediaAssetConfig { FilePath = path };
        asset.Name = kind == MediaLibraryKind.Image
            ? string.IsNullOrWhiteSpace(NewImageNameBox.Text) ? Path.GetFileNameWithoutExtension(path) : NewImageNameBox.Text.Trim()
            : string.IsNullOrWhiteSpace(NewVideoNameBox.Text) ? Path.GetFileNameWithoutExtension(path) : NewVideoNameBox.Text.Trim();
        asset.GroupId = kind == MediaLibraryKind.Image
            ? NewImageGroupBox.SelectedValue as string ?? ""
            : NewVideoGroupBox.SelectedValue as string ?? "";

        if (kind == MediaLibraryKind.Image)
        {
            var size = MediaMetadataService.ProbeImageSize(path);
            asset.Width = size.Width;
            asset.Height = size.Height;
        }
        else
        {
            asset.DurationMs = MediaMetadataService.ProbeVideoDurationMs(path);
        }

        if (existing is null)
        {
            library.Add(asset);
        }

        if (kind == MediaLibraryKind.Image)
        {
            _newImagePath = "";
            NewImagePathBox.Text = "";
            NewImageNameBox.Text = "";
            NewImageGroupBox.SelectedValue = "";
        }
        else
        {
            _newVideoPath = "";
            NewVideoPathBox.Text = "";
            NewVideoNameBox.Text = "";
            NewVideoGroupBox.SelectedValue = "";
        }

        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, title, asset.DisplayName), ActivityLogKind.Info);
    }

    internal void AddImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        AddMediaGroup(MediaLibraryKind.Image);
    }

    internal void AddVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        AddMediaGroup(MediaLibraryKind.Video);
    }

    private void AddMediaGroup(MediaLibraryKind kind)
    {
        var nameBox = kind == MediaLibraryKind.Image ? NewImageGroupNameBox : NewVideoGroupNameBox;
        var title = MediaLibraryTitle(kind);
        var name = nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.LibraryWriteGroupName), title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var groups = GetMediaGroups(kind);
        var mutation = LibraryGroupService.GetOrCreate<MediaGroupConfig>(groups, name);
        if (!mutation.IsValid || mutation.Group is null)
        {
            return;
        }

        if (!mutation.Created)
        {
            if (kind == MediaLibraryKind.Image)
            {
                NewImageGroupBox.SelectedValue = mutation.Group.Id;
            }
            else
            {
                NewVideoGroupBox.SelectedValue = mutation.Group.Id;
            }

            nameBox.Text = "";
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            NewImageGroupBox.SelectedValue = mutation.Group.Id;
        }
        else
        {
            NewVideoGroupBox.SelectedValue = mutation.Group.Id;
        }

        nameBox.Text = "";
        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibraryGroupCreatedLog, title, mutation.Group.Name), ActivityLogKind.Info);
    }

    internal void ViewImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        ViewMediaGroup(MediaLibraryKind.Image, sender);
    }

    internal void ViewVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        ViewMediaGroup(MediaLibraryKind.Video, sender);
    }

    private void ViewMediaGroup(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var group = GetMediaGroups(kind).FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageGroupFilterId = group.Id;
            _imageFilter = "ALL";
            ImageSearchBox.Text = "";
            _imageSearchText = "";
        }
        else
        {
            _videoGroupFilterId = group.Id;
            _videoFilter = "ALL";
            VideoSearchBox.Text = "";
            _videoSearchText = "";
        }

        UpdateMediaFilterButtons(kind);
        RefreshMediaLibraryView(kind);
    }

    internal void DeleteImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaGroup(MediaLibraryKind.Image, sender);
    }

    internal void DeleteVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaGroup(MediaLibraryKind.Video, sender);
    }

    private void DeleteMediaGroup(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var groups = GetMediaGroups(kind);
        var group = groups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var count = LibraryGroupService.CountAssetsInGroup(library, group.Id);
        var title = MediaLibraryTitle(kind);
        if (WpfMessageBox.Show(
                this,
                _text.Format(UiTextKeys.LibraryDeleteGroupPrompt, group.Name, count),
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        LibraryGroupService.ClearGroupFromAssets(library, group.Id);

        groups.Remove(group);
        if (kind == MediaLibraryKind.Image && string.Equals(_imageGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _imageGroupFilterId = "";
        }
        else if (kind == MediaLibraryKind.Video && string.Equals(_videoGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _videoGroupFilterId = "";
        }

        SaveConfig();
        RefreshMediaLibraryView(kind);
    }

    internal void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaAsset(MediaLibraryKind.Image, sender);
    }

    internal void DeleteVideoButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaAsset(MediaLibraryKind.Video, sender);
    }

    internal async void PreviewImageButton_Click(object sender, RoutedEventArgs e)
    {
        await PreviewMediaAssetAsync(MediaLibraryKind.Image, sender);
    }

    internal async void PreviewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        await PreviewMediaAssetAsync(MediaLibraryKind.Video, sender);
    }

    private async Task PreviewMediaAssetAsync(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string assetId)
        {
            return;
        }

        if (_previewingMediaKind == kind
            && string.Equals(_previewingMediaId, assetId, StringComparison.OrdinalIgnoreCase))
        {
            await StopMediaPreviewAsync();
            return;
        }

        if (!_config.Obs.IsConfigured || !_obsService.IsConnected)
        {
            AddLog(_text.Get(UiTextKeys.MediaObsConnectRequiredLog), ActivityLogKind.Important);
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.MediaObsConnectRequiredPrompt), "OBS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null || !File.Exists(asset.FilePath))
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingFileLog), ActivityLogKind.Important);
            return;
        }

        var sceneName = _obsService.CurrentScene;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingSceneLog), ActivityLogKind.Important);
            return;
        }

        var info = MediaLibraryKindCatalog.Get(kind);
        var obsKind = info.ObsKind;
        var sourceName = info.PreviewSourceName;
        var duration = obsKind == ObsMediaKind.Video
            ? TimeSpan.FromMilliseconds(asset.DurationMs > 0 ? asset.DurationMs : 5000)
            : TimeSpan.FromSeconds(5);

        await StopMediaPreviewAsync();
        var previewCts = new CancellationTokenSource();
        _mediaPreviewCts = previewCts;
        _previewingMediaKind = kind;
        _previewingMediaId = asset.Id;
        RefreshMediaLibraryView(kind);

        try
        {
            var result = await _obsService.ShowMediaSourceAsync(
                sceneName,
                sourceName,
                asset.FilePath,
                obsKind,
                _config.Obs,
                obsKind == ObsMediaKind.Video ? _config.VideoVolumePercent : null,
                previewCts.Token);
            ApplyObsResult(result);
            _mediaPreviewHideRequest = new ObsMediaHideRequest(sceneName, sourceName, duration, DateTimeOffset.UtcNow);
            WriteObsOverlayState(asset, obsKind, duration);
            MarkObsMediaAssetUsed(obsKind, asset);
            AddLog(_text.Format(UiTextKeys.MediaObsPreviewLog, MediaLibraryTitle(kind).ToLowerInvariant(), asset.DisplayName), ActivityLogKind.Obs);

            await Task.Delay(duration, previewCts.Token);
            await StopMediaPreviewAsync();
        }
        catch (OperationCanceledException)
        {
            // The user stopped the preview.
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo probar medio OBS '{asset.DisplayName}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
            await StopMediaPreviewAsync();
        }
    }

    private async Task StopMediaPreviewAsync()
    {
        var cts = _mediaPreviewCts;
        var request = _mediaPreviewHideRequest;
        var kind = _previewingMediaKind;

        _mediaPreviewCts = null;
        _mediaPreviewHideRequest = null;
        _previewingMediaKind = null;
        _previewingMediaId = "";

        try
        {
            cts?.Cancel();
            if (request is not null && _obsService.IsConnected)
            {
                await HideRuleObsMediaAsync(request, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo detener la prueba de medio OBS.");
            AddLog($"OBS prueba: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            cts?.Dispose();
            if (kind is MediaLibraryKind mediaKind)
            {
                RefreshMediaLibraryView(mediaKind);
            }
        }
    }

    private void DeleteMediaAsset(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string assetId)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var title = MediaLibraryTitle(kind);
        if (WpfMessageBox.Show(this, _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, asset.DisplayName), title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        library.Remove(asset);
        SaveConfig();
        RefreshMediaLibraryView(kind);
    }

    private void RefreshMediaLibraryView(MediaLibraryKind kind)
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RefreshMediaLibraryView(kind));
            return;
        }

        SetMediaRefreshing(kind, true);
        try
        {
            var library = GetMediaLibrary(kind);
            var groups = GetMediaGroups(kind);
            var groupFilterId = GetMediaGroupFilterId(kind);
            var groupsById = groups.ToDictionary(group => group.Id, group => group.Name, StringComparer.OrdinalIgnoreCase);

            RefreshMediaGroupChoicesIfNeeded(kind);

            var rows = library
                .Select((asset, index) => CreateMediaLibraryRow(kind, asset, groupsById, index))
                .Where(row => MediaRowMatchesFilters(kind, row))
                .ToArray();

            var rowTarget = GetMediaRows(kind);
            rowTarget.Clear();
            foreach (var row in rows)
            {
                rowTarget.Add(row);
            }

            var groupRows = GetMediaGroupRows(kind);
            groupRows.Clear();
            var groupIndex = 0;
            foreach (var group in groups)
            {
                var count = library.Count(asset => string.Equals(asset.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                groupRows.Add(new MediaGroupRow(
                    group.Id,
                    group.Name,
                    _text.Format(UiTextKeys.LibraryFileCount, count, count == 1 ? "" : "s"),
                    FrozenBrushFrom((groupIndex++ % 4) switch
                    {
                        0 => "#14B8A6",
                        1 => "#B56CFF",
                        2 => "#37C7F3",
                        _ => "#22C55E"
                    })));
            }

            var lastAsset = library
                .Where(asset => asset.LastUsedAt is not null)
                .OrderByDescending(asset => asset.LastUsedAt)
                .FirstOrDefault();

            var groupFilterText = string.IsNullOrWhiteSpace(groupFilterId)
                ? ""
                : $" del grupo {groupsById.GetValueOrDefault(groupFilterId, _text.Get(UiTextKeys.LibrarySelectedGroup))}";

            if (kind == MediaLibraryKind.Image)
            {
                ImageSavedCountText.Text = library.Count.ToString();
                ImageGroupCountText.Text = groups.Count.ToString();
                LastImageText.Text = lastAsset?.DisplayName ?? _text.Get(UiTextKeys.LibraryLastUnused);
                ImageLibraryFooterText.Text = $"Mostrando {rows.Length} de {library.Count} {MediaLibraryKindCatalog.Get(kind).FooterNoun}{groupFilterText}";
                NewImageGroupBox.Items.Refresh();
            }
            else
            {
                VideoSavedCountText.Text = library.Count.ToString();
                VideoGroupCountText.Text = groups.Count.ToString();
                LastVideoText.Text = lastAsset?.DisplayName ?? _text.Get(UiTextKeys.LibraryLastUnused);
                VideoLibraryFooterText.Text = $"Mostrando {rows.Length} de {library.Count} {MediaLibraryKindCatalog.Get(kind).FooterNoun}{groupFilterText}";
                NewVideoGroupBox.Items.Refresh();
            }

            UpdateMediaFilterButtons(kind);
            RefreshRuleObsMediaChoices();
        }
        finally
        {
            SetMediaRefreshing(kind, false);
        }
    }

    private void RefreshMediaGroupChoicesIfNeeded(MediaLibraryKind kind)
    {
        var groups = GetMediaGroups(kind);
        var signature = string.Join("|", groups.Select(group => $"{group.Id}:{group.Name}"));
        var currentSignature = kind == MediaLibraryKind.Image
            ? _imageGroupChoicesSignature
            : _videoGroupChoicesSignature;
        if (string.Equals(signature, currentSignature, StringComparison.Ordinal))
        {
            return;
        }

        var choices = kind == MediaLibraryKind.Image ? ImageGroupChoices : VideoGroupChoices;
        choices.Clear();
        choices.Add(new MediaGroupChoice("", _text.Get(UiTextKeys.LibraryNoGroupAssigned)));
        foreach (var group in groups)
        {
            choices.Add(new MediaGroupChoice(group.Id, group.Name));
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageGroupChoicesSignature = signature;
        }
        else
        {
            _videoGroupChoicesSignature = signature;
        }
    }

    private MediaLibraryRow CreateMediaLibraryRow(
        MediaLibraryKind kind,
        MediaAssetConfig asset,
        IReadOnlyDictionary<string, string> groupsById,
        int index)
    {
        var info = MediaLibraryKindCatalog.Get(kind);
        var accentColor = info.AccentColor;
        var metadata = kind == MediaLibraryKind.Image
            ? asset.ResolutionText
            : MediaMetadataService.BuildVideoMetadata(asset);

        return new MediaLibraryRow(
            asset.Id,
            asset.DisplayName,
            asset.FilePath,
            asset.GroupId,
            groupsById.TryGetValue(asset.GroupId, out var groupName) ? groupName : _text.Get(UiTextKeys.LibraryNoGroup),
            metadata,
            info.IconPath,
            FrozenBrushFrom(accentColor),
            TranslucentBrushFrom(accentColor),
            index,
            _config.Obs.IsConfigured && _obsService.IsConnected && !_isObsConnecting,
            _previewingMediaKind == kind && string.Equals(_previewingMediaId, asset.Id, StringComparison.OrdinalIgnoreCase));
    }

    private bool MediaRowMatchesFilters(MediaLibraryKind kind, MediaLibraryRow row)
    {
        return LibraryRowFilterService.MatchesMedia(
            row,
            GetMediaGroupFilterId(kind),
            GetMediaFilter(kind),
            GetMediaSearchText(kind));
    }

    private void UpdateMediaFilterButtons(MediaLibraryKind kind)
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        var accentColor = MediaLibraryKindCatalog.Get(kind).AccentColor;
        var filter = GetMediaFilter(kind);
        var buttons = kind == MediaLibraryKind.Image
            ? new[] { ImageFilterAllButton, ImageFilterWithGroupButton, ImageFilterNoGroupButton }
            : new[] { VideoFilterAllButton, VideoFilterWithGroupButton, VideoFilterNoGroupButton };

        foreach (var button in buttons)
        {
            var active = string.Equals(button.Tag?.ToString(), filter, StringComparison.OrdinalIgnoreCase);
            button.Background = active ? TranslucentBrushFrom(accentColor) : palette.Input;
            button.Foreground = active ? FrozenBrushFrom(accentColor) : palette.Text;
            button.BorderBrush = active ? FrozenBrushFrom(accentColor) : palette.Border;
        }
    }

    private ObservableCollection<MediaAssetConfig> GetMediaLibrary(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageLibrary : _config.VideoLibrary;
    }

    private ObservableCollection<MediaGroupConfig> GetMediaGroups(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageGroups : _config.VideoGroups;
    }

    private ObservableCollection<MediaLibraryRow> GetMediaRows(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageLibraryRows : _videoLibraryRows;
    }

    private ObservableCollection<MediaGroupRow> GetMediaGroupRows(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageGroupRows : _videoGroupRows;
    }

    private string GetMediaSearchText(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageSearchText : _videoSearchText;
    }

    private string GetMediaFilter(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageFilter : _videoFilter;
    }

    private string GetMediaGroupFilterId(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageGroupFilterId : _videoGroupFilterId;
    }

    private void SetMediaRefreshing(MediaLibraryKind kind, bool refreshing)
    {
        if (kind == MediaLibraryKind.Image)
        {
            _refreshingImageLibrary = refreshing;
        }
        else
        {
            _refreshingVideoLibrary = refreshing;
        }
    }

    private string MediaLibraryTitle(MediaLibraryKind kind)
    {
        return _text.Get(MediaLibraryKindCatalog.Get(kind).TitleKey);
    }

    private void UpdateVideoVolumeText()
    {
        VideoVolumeValueText.Text = $"{_config.VideoVolumePercent}%";
    }
}
