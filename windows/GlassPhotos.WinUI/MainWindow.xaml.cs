using GlassPhotos.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.VisualBasic.FileIO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT;
using WinRT.Interop;

namespace GlassPhotos.WinUI;

public sealed partial class MainWindow : Window
{
    [ComImport]
    [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDataTransferManagerInterop
    {
        nint GetForWindow([In] nint appWindow, [In] ref Guid riid);
        void ShowShareUIForWindow(nint appWindow);
    }

    private static readonly Guid DataTransferManagerIid =
        new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

    private readonly string? _initialPath;
    private readonly AsyncLruCache<string, BitmapImage> _imageCache =
        new(capacity: 7, StringComparer.OrdinalIgnoreCase);
    private readonly AppWindow _appWindow;
    private readonly nint _windowHandle;
    private readonly IDataTransferManagerInterop _shareInterop;
    private readonly DataTransferManager _shareManager;
    private PhotoCollection? _photos;
    private long _loadGeneration;
    private bool _isRenaming;
    private bool _isFullScreen;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        _initialPath = initialPath;
        _windowHandle = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_windowHandle));
        _shareInterop = DataTransferManager.As<IDataTransferManagerInterop>();
        var shareManagerIid = DataTransferManagerIid;
        var shareManagerPointer = _shareInterop.GetForWindow(_windowHandle, ref shareManagerIid);
        _shareManager = MarshalInterface<DataTransferManager>.FromAbi(shareManagerPointer);
        _shareManager.DataRequested += ShareManager_DataRequested;
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
        InitializeWithWindow.Initialize(picker, _windowHandle);

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
        RotateLeftButton.IsEnabled = true;
        RotateRightButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
        InfoButton.IsEnabled = true;
        ShareButton.IsEnabled = true;
        FullScreenButton.IsEnabled = true;
        Title = $"{Path.GetFileName(path)} — Glass Photos";
        _ = UpdateImageInfoAsync(path, generation);
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

    private async void RotateLeft_Click(object sender, RoutedEventArgs e) =>
        await RotateCurrentAsync(BitmapRotation.Clockwise270Degrees);

    private async void RotateRight_Click(object sender, RoutedEventArgs e) =>
        await RotateCurrentAsync(BitmapRotation.Clockwise90Degrees);

    private async Task RotateCurrentAsync(BitmapRotation rotation)
    {
        if (_photos is null) return;

        var path = _photos.CurrentPath;
        StorageFile? temporaryFile = null;
        try
        {
            RotateLeftButton.IsEnabled = false;
            RotateRightButton.IsEnabled = false;

            var sourceFile = await StorageFile.GetFileFromPathAsync(path);
            var folder = await sourceFile.GetParentAsync();
            var extension = sourceFile.FileType.ToLowerInvariant();
            var encoderId = EncoderIdForExtension(extension);
            temporaryFile = await folder.CreateFileAsync(
                $".glassphotos-{Guid.NewGuid():N}{extension}",
                CreationCollisionOption.FailIfExists);

            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(sourceStream);
                var transform = new BitmapTransform { Rotation = rotation };
                var pixels = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);

                using var outputStream = await temporaryFile.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateAsync(encoderId, outputStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    decoder.PixelHeight,
                    decoder.PixelWidth,
                    decoder.DpiX,
                    decoder.DpiY,
                    pixels.DetachPixelData());
                await encoder.FlushAsync();
            }

            await temporaryFile.MoveAndReplaceAsync(sourceFile);
            temporaryFile = null;
            _imageCache.Remove(path);
            await DisplayCurrentPhotoAsync();
        }
        catch (Exception exception)
        {
            if (temporaryFile is not null)
            {
                try { await temporaryFile.DeleteAsync(StorageDeleteOption.PermanentDelete); }
                catch { }
            }
            await ShowErrorAsync("Could not rotate photo", exception.Message);
        }
        finally
        {
            RotateLeftButton.IsEnabled = _photos is not null;
            RotateRightButton.IsEnabled = _photos is not null;
        }
    }

    private static Guid EncoderIdForExtension(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => BitmapEncoder.JpegEncoderId,
        ".png" => BitmapEncoder.PngEncoderId,
        ".tif" or ".tiff" => BitmapEncoder.TiffEncoderId,
        ".bmp" => BitmapEncoder.BmpEncoderId,
        ".gif" => BitmapEncoder.GifEncoderId,
        _ => throw new NotSupportedException(
            $"Persistent rotation is not supported for {extension} images on Windows.")
    };

    private void Share_Click(object sender, RoutedEventArgs e)
    {
        if (_photos is not null)
        {
            _shareInterop.ShowShareUIForWindow(_windowHandle);
        }
    }

    private async void ShareManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        var deferral = args.Request.GetDeferral();
        try
        {
            if (_photos is null)
            {
                args.Request.FailWithDisplayText("No photo is open.");
                return;
            }

            var file = await StorageFile.GetFileFromPathAsync(_photos.CurrentPath);
            args.Request.Data.Properties.Title = file.Name;
            args.Request.Data.Properties.Description = "Shared from Glass Photos";
            args.Request.Data.RequestedOperation = DataPackageOperation.Copy;
            args.Request.Data.SetStorageItems([file]);
        }
        catch (Exception exception)
        {
            args.Request.FailWithDisplayText(exception.Message);
        }
        finally
        {
            deferral.Complete();
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

    private async Task UpdateImageInfoAsync(string path, long generation)
    {
        var info = new FileInfo(path);
        var rows = new List<(string Title, string Value)>
        {
            ("File Name", info.Name),
            ("File Path", info.FullName)
        };

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var imageProperties = await file.Properties.GetImagePropertiesAsync();
            if (imageProperties.DateTaken.Year > 1601)
            {
                rows.Add(("Date Taken", imageProperties.DateTaken.ToString("g")));
            }

            rows.Add(("File Size", FormatFileSize(info.Length)));
            if (imageProperties.Width > 0 && imageProperties.Height > 0)
            {
                rows.Add(("Dimensions", $"{imageProperties.Width} × {imageProperties.Height}"));
            }
            if (!string.IsNullOrWhiteSpace(imageProperties.CameraManufacturer))
            {
                rows.Add(("Camera Make", imageProperties.CameraManufacturer));
            }
            if (!string.IsNullOrWhiteSpace(imageProperties.CameraModel))
            {
                rows.Add(("Camera Model", imageProperties.CameraModel));
            }
            if (imageProperties.Latitude is double latitude &&
                imageProperties.Longitude is double longitude)
            {
                rows.Add(("GPS Coordinates", $"{latitude}, {longitude}"));
            }
        }
        catch
        {
            rows.Add(("File Size", FormatFileSize(info.Length)));
        }

        rows.Add(("Modified", info.LastWriteTime.ToString("g")));
        rows.Add(("Created", info.CreationTime.ToString("g")));

        if (generation != _loadGeneration) return;
        InfoRows.Children.Clear();
        foreach (var row in rows)
        {
            AddInfoRow(row.Title, row.Value);
        }
    }

    private void AddInfoRow(string title, string value)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = title.ToUpperInvariant(),
            Style = (Style)RootGrid.Resources["InfoLabel"]
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            Style = (Style)RootGrid.Resources["InfoValue"]
        });
        InfoRows.Children.Add(panel);
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
