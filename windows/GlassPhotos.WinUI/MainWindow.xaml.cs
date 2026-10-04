using GlassPhotos.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.VisualBasic.FileIO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace GlassPhotos.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly string? _initialPath;
    private readonly AsyncLruCache<string, BitmapImage> _imageCache =
        new(capacity: 7, StringComparer.OrdinalIgnoreCase);
    private readonly AppWindow _appWindow;
    private PhotoCollection? _photos;
    private long _loadGeneration;
    private bool _isRenaming;
    private bool _isFullScreen;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        _initialPath = initialPath;
        var windowHandle = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(windowHandle));
        Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated;
        if (_initialPath is not null)
        {
            await OpenPhotoAsync(_initialPath);
        }
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;

        try
        {
            _photos = PhotoCollection.OpenDirectory(folder.Path);
            await DisplayCurrentPhotoAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Could not open folder", exception.Message);
        }
    }

    private async Task OpenPhotoAsync(string path)
    {
        try
        {
            _photos = PhotoCollection.Open(path);
            await DisplayCurrentPhotoAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Could not open photo", exception.Message);
        }
    }

    private async Task DisplayCurrentPhotoAsync()
    {
        if (_photos is null) return;

        CancelRename();
        var generation = Interlocked.Increment(ref _loadGeneration);
        var path = _photos.CurrentPath;
        var bitmap = await _imageCache.GetAsync(path, LoadBitmapAsync);

        if (generation != _loadGeneration) return;

        PhotoImage.Source = bitmap;
        WelcomePanel.Visibility = Visibility.Collapsed;
        TopHeader.Visibility = Visibility.Visible;
        FileNameText.Text = Path.GetFileName(path);
        ToolTipService.SetToolTip(FileNameText, path);
        PositionText.Text = $"({_photos.CurrentIndex + 1}/{_photos.Files.Count})";
        CounterPill.Visibility = _photos.Files.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        RenameButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
        InfoButton.IsEnabled = true;
        FullScreenButton.IsEnabled = true;
        Title = $"{Path.GetFileName(path)} — Glass Photos";
        UpdateImageInfo(path);
        _ = PrefetchNeighboursAsync();
    }

    private static async Task<BitmapImage> LoadBitmapAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        await using var stream = await file.OpenStreamForReadAsync();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
        return bitmap;
    }

    private async Task PrefetchNeighboursAsync()
    {
        if (_photos is null) return;

        var paths = new[] { _photos.CurrentIndex - 1, _photos.CurrentIndex + 1 }
            .Where(index => index >= 0 && index < _photos.Files.Count)
            .Select(index => _photos.Files[index])
            .ToArray();

        try
        {
            await Task.WhenAll(paths.Select(path => _imageCache.GetAsync(path, LoadBitmapAsync)));
        }
        catch
        {
            // A neighbor may disappear while prefetching. The visible image remains usable.
        }
    }

    private void FileName_Tapped(object sender, TappedRoutedEventArgs e) => BeginRename();

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (_isRenaming)
        {
            await CommitRenameAsync();
        }
        else
        {
            BeginRename();
        }
    }

    private void BeginRename()
    {
        if (_photos is null || _isRenaming) return;

        _isRenaming = true;
        RenameTextBox.Text = Path.GetFileNameWithoutExtension(_photos.CurrentPath);
        ExtensionText.Text = Path.GetExtension(_photos.CurrentPath);
        FileNameText.Visibility = Visibility.Collapsed;
        RenamePanel.Visibility = Visibility.Visible;
        RenameButton.Content = "Done";
        RenameTextBox.Focus(FocusState.Programmatic);
        RenameTextBox.SelectAll();
    }

    private async Task CommitRenameAsync()
    {
        if (_photos is null || !_isRenaming) return;

        try
        {
            var previousPath = _photos.CurrentPath;
            _photos.RenameCurrent(RenameTextBox.Text);
            _imageCache.Remove(previousPath);
            await DisplayCurrentPhotoAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Could not rename photo", exception.Message);
            RenameTextBox.Focus(FocusState.Programmatic);
        }
    }

    private void CancelRename()
    {
        _isRenaming = false;
        FileNameText.Visibility = Visibility.Visible;
        RenamePanel.Visibility = Visibility.Collapsed;
        RenameButton.Content = "Rename";
    }

    private async void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await CommitRenameAsync();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            CancelRename();
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e) => await DeleteCurrentAsync();

    private async Task DeleteCurrentAsync()
    {
        if (_photos is null) return;

        var path = _photos.CurrentPath;
        var dialog = new ContentDialog
        {
            Title = $"Move “{Path.GetFileName(path)}” to the Recycle Bin?",
            Content = "You can recover it from the Recycle Bin until it is emptied.",
            PrimaryButtonText = "Move to Recycle Bin",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            _imageCache.Remove(path);
            FileSystem.DeleteFile(
                path,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing);

            if (_photos.RemoveCurrentAfterDeletion() is null)
            {
                ClearPhoto();
            }
            else
            {
                await DisplayCurrentPhotoAsync();
            }
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Could not recycle photo", exception.Message);
        }
    }

    private void Info_Click(object sender, RoutedEventArgs e)
    {
        InfoSidebar.Visibility = InfoSidebar.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void CloseInfo_Click(object sender, RoutedEventArgs e) =>
        InfoSidebar.Visibility = Visibility.Collapsed;

    private void UpdateImageInfo(string path)
    {
        var info = new FileInfo(path);
        InfoFileName.Text = info.Name;
        InfoFilePath.Text = info.FullName;
        InfoFileSize.Text = FormatFileSize(info.Length);
        InfoModified.Text = info.LastWriteTime.ToString("g");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.#} {units[unit]}";
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();

    private void ToggleFullScreen()
    {
        _isFullScreen = !_isFullScreen;
        _appWindow.SetPresenter(_isFullScreen
            ? AppWindowPresenterKind.FullScreen
            : AppWindowPresenterKind.Overlapped);
        TopHeader.Visibility = _isFullScreen ? Visibility.Collapsed : Visibility.Visible;
        InfoSidebar.Visibility = _isFullScreen ? Visibility.Collapsed : InfoSidebar.Visibility;
    }

    private void ClearPhoto()
    {
        CancelRename();
        PhotoImage.Source = null;
        WelcomePanel.Visibility = Visibility.Visible;
        TopHeader.Visibility = Visibility.Collapsed;
        InfoSidebar.Visibility = Visibility.Collapsed;
        Title = "Glass Photos";
        _photos = null;
    }

    private async void Window_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_isRenaming) return;

        if (e.Key == VirtualKey.Left && _photos?.MovePrevious() == true)
        {
            e.Handled = true;
            await DisplayCurrentPhotoAsync();
        }
        else if (e.Key == VirtualKey.Right && _photos?.MoveNext() == true)
        {
            e.Handled = true;
            await DisplayCurrentPhotoAsync();
        }
        else if (e.Key == VirtualKey.Enter && _photos is not null)
        {
            e.Handled = true;
            BeginRename();
        }
        else if (e.Key == VirtualKey.Delete && _photos is not null)
        {
            e.Handled = true;
            await DeleteCurrentAsync();
        }
        else if (e.Key == VirtualKey.F && _photos is not null)
        {
            e.Handled = true;
            ToggleFullScreen();
        }
        else if (e.Key == VirtualKey.Escape && _isFullScreen)
        {
            e.Handled = true;
            ToggleFullScreen();
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
