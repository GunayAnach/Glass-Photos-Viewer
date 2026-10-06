using GlassPhotoViewer.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.VisualBasic.FileIO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT;
using WinRT.Interop;

namespace GlassPhotoViewer.WinUI;

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
    private readonly string _windowPlacementPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Glass Photo Viewer",
        "window-placement.json");
    private readonly string _legacyWindowPlacementPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Glass Photos",
        "window-placement.json");
    private WindowPlacement? _lastWindowPlacement;
    private PhotoCollection? _photos;
    private long _loadGeneration;
    private bool _isRenaming;
    private bool _isFullScreen;
    private bool _isFitToWindow = true;
    private bool _isDeleteConfirmationVisible;
    private bool _isCropping;
    private bool _isSavingCrop;
    private NormalizedCropRect _cropRect = new(0.1, 0.1, 0.8, 0.8);
    private NormalizedCropRect _cropDragStart;
    private Windows.Foundation.Point _cropPointerStart;
    private string? _cropDragMode;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        _initialPath = initialPath;
        _windowHandle = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_windowHandle));
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "GlassPhotoViewer.ico");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }
        RestoreWindowPlacement();
        _lastWindowPlacement = CaptureWindowPlacement();
        _appWindow.Changed += AppWindow_Changed;
        _appWindow.Closing += AppWindow_Closing;
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

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            DispatcherQueue.TryEnqueue(UpdateWindowTitle);
        }
        if (!_isFullScreen && IsWindowRestored() && (args.DidSizeChange || args.DidPositionChange))
        {
            _lastWindowPlacement = CaptureWindowPlacement();
        }
    }

    private bool IsWindowRestored() =>
        _appWindow.Presenter is OverlappedPresenter presenter &&
        presenter.State == OverlappedPresenterState.Restored;

    private WindowPlacement CaptureWindowPlacement() => new(
        _appWindow.Position.X,
        _appWindow.Position.Y,
        _appWindow.Size.Width,
        _appWindow.Size.Height);

    private void RestoreWindowPlacement()
    {
        var placement = WindowPlacementStore.LoadFirstAvailable(
            _windowPlacementPath,
            _legacyWindowPlacementPath);
        if (placement is null) return;

        var display = DisplayArea.GetFromPoint(
            new PointInt32(placement.X, placement.Y),
            DisplayAreaFallback.Nearest);
        if (display is null) return;

        var workArea = display.WorkArea;
        var width = Math.Min(placement.Width, workArea.Width);
        var height = Math.Min(placement.Height, workArea.Height);
        var x = Math.Clamp(placement.X, workArea.X, workArea.X + workArea.Width - width);
        var y = Math.Clamp(placement.Y, workArea.Y, workArea.Y + workArea.Height - height);
        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            WindowPlacementStore.Save(
                _windowPlacementPath,
                _lastWindowPlacement ?? CaptureWindowPlacement());
        }
        catch
        {
            // Window shutdown must continue even if local settings cannot be written.
        }
    }

    private void UpdateWindowTitle()
    {
        if (_photos is null)
        {
            Title = "Glass Photo Viewer";
            return;
        }

        const double averageTitleCharacterWidth = 7.0;
        const int systemCaptionWidth = 190;
        var availableWidth = Math.Max(140, _appWindow.Size.Width - systemCaptionWidth);
        var maximumCharacters = Math.Max(20, (int)(availableWidth / averageTitleCharacterWidth));
        Title = WindowTitleFormatter.Format(_photos.CurrentPath, maximumCharacters);
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e) => await OpenFolderAsync();

    private async Task OpenFolderAsync()
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
        CropButton.IsEnabled = true;
        RotateLeftButton.IsEnabled = true;
        RotateRightButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
        InfoButton.IsEnabled = true;
        ShareButton.IsEnabled = true;
        FullScreenButton.IsEnabled = true;
        UpdateWindowTitle();
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

        _isDeleteConfirmationVisible = true;
        ContentDialogResult result;
        try
        {
            result = await dialog.ShowAsync();
        }
        finally
        {
            _isDeleteConfirmationVisible = false;
        }

        if (result != ContentDialogResult.Primary) return;

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

    private async void Crop_Click(object sender, RoutedEventArgs e)
    {
        if (_isCropping)
        {
            await CommitCropAsync();
        }
        else
        {
            BeginCrop();
        }
    }

    private void BeginCrop()
    {
        if (_photos is null || _isSavingCrop) return;
        CancelRename();
        _isFitToWindow = true;
        PhotoImage.Stretch = Stretch.Uniform;
        InfoSidebar.Visibility = Visibility.Collapsed;
        _cropRect = new NormalizedCropRect(0.1, 0.1, 0.8, 0.8);
        _isCropping = true;
        CropOverlay.Visibility = Visibility.Visible;
        CropIconText.Text = "✓";
        CropIconText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
        ToolTipService.SetToolTip(CropButton, "Apply crop (Enter)");
        SetEditingControlsEnabled(false);
        CropButton.IsEnabled = true;
        UpdateCropOverlay();
    }

    private void CancelCrop()
    {
        if (!_isCropping || _isSavingCrop) return;
        _isCropping = false;
        _cropDragMode = null;
        CropOverlay.Visibility = Visibility.Collapsed;
        CropIconText.Text = "";
        CropIconText.ClearValue(TextBlock.ForegroundProperty);
        ToolTipService.SetToolTip(CropButton, "Crop Photo (C)");
        SetEditingControlsEnabled(_photos is not null);
    }

    private void SetEditingControlsEnabled(bool enabled)
    {
        RenameButton.IsEnabled = enabled;
        RotateLeftButton.IsEnabled = enabled;
        RotateRightButton.IsEnabled = enabled;
        InfoButton.IsEnabled = enabled;
        ShareButton.IsEnabled = enabled;
        FullScreenButton.IsEnabled = enabled;
        DeleteButton.IsEnabled = enabled;
    }

    private async Task CommitCropAsync()
    {
        if (!_isCropping || _isSavingCrop || _photos is null) return;
        var path = _photos.CurrentPath;
        StorageFile? temporaryFile = null;
        _isSavingCrop = true;
        CropButton.IsEnabled = false;

        try
        {
            var sourceFile = await StorageFile.GetFileFromPathAsync(path);
            var folder = await sourceFile.GetParentAsync();
            var extension = sourceFile.FileType.ToLowerInvariant();
            temporaryFile = await folder.CreateFileAsync(
                $".glassphotoviewer-{Guid.NewGuid():N}{extension}",
                CreationCollisionOption.FailIfExists);

            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(sourceStream);
                var provider = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);
                var sourcePixels = provider.DetachPixelData();
                var sourceWidth = checked((int)decoder.OrientedPixelWidth);
                var sourceHeight = checked((int)decoder.OrientedPixelHeight);
                var crop = _cropRect.ToPixels(sourceWidth, sourceHeight);
                var croppedPixels = new byte[checked(crop.Width * crop.Height * 4)];
                var sourceStride = checked(sourceWidth * 4);
                var targetStride = checked(crop.Width * 4);
                for (var row = 0; row < crop.Height; row++)
                {
                    Buffer.BlockCopy(
                        sourcePixels,
                        checked((crop.Y + row) * sourceStride + crop.X * 4),
                        croppedPixels,
                        row * targetStride,
                        targetStride);
                }

                using var outputStream = await temporaryFile.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateAsync(EncoderIdForExtension(extension), outputStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    checked((uint)crop.Width),
                    checked((uint)crop.Height),
                    decoder.DpiX,
                    decoder.DpiY,
                    croppedPixels);
                await encoder.FlushAsync();
            }

            await temporaryFile.MoveAndReplaceAsync(sourceFile);
            temporaryFile = null;
            _imageCache.Remove(path);
            _isSavingCrop = false;
            CancelCrop();
            await DisplayCurrentPhotoAsync();
        }
        catch (Exception exception)
        {
            if (temporaryFile is not null)
            {
                try { await temporaryFile.DeleteAsync(StorageDeleteOption.PermanentDelete); }
                catch { }
            }
            _isSavingCrop = false;
            CropButton.IsEnabled = true;
            await ShowErrorAsync("Could not crop photo", exception.Message);
        }
    }

    private void PhotoViewport_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCropOverlay();

    private Windows.Foundation.Rect GetDisplayedImageBounds()
    {
        if (PhotoImage.Source is not BitmapImage bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            return new Windows.Foundation.Rect();

        var viewportWidth = PhotoViewport.ActualWidth;
        var viewportHeight = PhotoViewport.ActualHeight;
        var scale = Math.Min(viewportWidth / bitmap.PixelWidth, viewportHeight / bitmap.PixelHeight);
        var width = bitmap.PixelWidth * scale;
        var height = bitmap.PixelHeight * scale;
        return new Windows.Foundation.Rect(
            (viewportWidth - width) / 2,
            (viewportHeight - height) / 2,
            width,
            height);
    }

    private void UpdateCropOverlay()
    {
        if (!_isCropping) return;
        CropOverlay.Width = PhotoViewport.ActualWidth;
        CropOverlay.Height = PhotoViewport.ActualHeight;
        var image = GetDisplayedImageBounds();
        if (image.Width <= 0 || image.Height <= 0) return;
        var selection = new Windows.Foundation.Rect(
            image.X + _cropRect.X * image.Width,
            image.Y + _cropRect.Y * image.Height,
            _cropRect.Width * image.Width,
            _cropRect.Height * image.Height);

        SetCanvasRect(CropSelection, selection.X, selection.Y, selection.Width, selection.Height);
        SetCanvasRect(CropDimTop, image.X, image.Y, image.Width, selection.Y - image.Y);
        SetCanvasRect(CropDimBottom, image.X, selection.Bottom, image.Width, image.Bottom - selection.Bottom);
        SetCanvasRect(CropDimLeft, image.X, selection.Y, selection.X - image.X, selection.Height);
        SetCanvasRect(CropDimRight, selection.Right, selection.Y, image.Right - selection.Right, selection.Height);
    }

    private static void SetCanvasRect(FrameworkElement element, double x, double y, double width, double height)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
    }

    private void CropSelection_PointerPressed(object sender, PointerRoutedEventArgs e) =>
        BeginCropDrag("Move", e);

    private void CropHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        BeginCropDrag((sender as FrameworkElement)?.Tag?.ToString() ?? "Move", e);
        e.Handled = true;
    }

    private void BeginCropDrag(string mode, PointerRoutedEventArgs e)
    {
        if (!_isCropping || _isSavingCrop) return;
        _cropDragMode = mode;
        _cropDragStart = _cropRect;
        _cropPointerStart = e.GetCurrentPoint(CropOverlay).Position;
        CropOverlay.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void CropOverlay_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_cropDragMode is null) return;
        var image = GetDisplayedImageBounds();
        if (image.Width <= 0 || image.Height <= 0) return;
        var point = e.GetCurrentPoint(CropOverlay).Position;
        var dx = (point.X - _cropPointerStart.X) / image.Width;
        var dy = (point.Y - _cropPointerStart.Y) / image.Height;
        const double minimum = 0.05;
        var left = _cropDragStart.X;
        var top = _cropDragStart.Y;
        var right = _cropDragStart.X + _cropDragStart.Width;
        var bottom = _cropDragStart.Y + _cropDragStart.Height;

        switch (_cropDragMode)
        {
            case "Move":
                left = Math.Clamp(_cropDragStart.X + dx, 0, 1 - _cropDragStart.Width);
                top = Math.Clamp(_cropDragStart.Y + dy, 0, 1 - _cropDragStart.Height);
                right = left + _cropDragStart.Width;
                bottom = top + _cropDragStart.Height;
                break;
            case "TopLeft":
                left = Math.Clamp(_cropDragStart.X + dx, 0, right - minimum);
                top = Math.Clamp(_cropDragStart.Y + dy, 0, bottom - minimum);
                break;
            case "TopRight":
                right = Math.Clamp(right + dx, left + minimum, 1);
                top = Math.Clamp(_cropDragStart.Y + dy, 0, bottom - minimum);
                break;
            case "BottomLeft":
                left = Math.Clamp(_cropDragStart.X + dx, 0, right - minimum);
                bottom = Math.Clamp(bottom + dy, top + minimum, 1);
                break;
            case "BottomRight":
                right = Math.Clamp(right + dx, left + minimum, 1);
                bottom = Math.Clamp(bottom + dy, top + minimum, 1);
                break;
        }

        _cropRect = new NormalizedCropRect(left, top, right - left, bottom - top).Clamp(minimum);
        UpdateCropOverlay();
    }

    private void CropOverlay_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        CropOverlay.ReleasePointerCapture(e.Pointer);
        _cropDragMode = null;
    }

    private void CropOverlay_PointerCaptureLost(object sender, PointerRoutedEventArgs e) =>
        _cropDragMode = null;

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
                $".glassphotoviewer-{Guid.NewGuid():N}{extension}",
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
            args.Request.Data.Properties.Description = "Shared from Glass Photo Viewer";
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

    private void Info_Click(object sender, RoutedEventArgs e) => ToggleInfoSidebar();

    private void ToggleInfoSidebar()
    {
        if (_photos is null) return;
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

            rows.Add(("File Size", MetadataFormatter.FormatFileSize(info.Length)));
            if (imageProperties.Width > 0 && imageProperties.Height > 0)
            {
                rows.Add(("Dimensions", $"{imageProperties.Width} × {imageProperties.Height}"));
            }

            var metadataKeys = new[]
            {
                "System.Image.ColorSpace",
                "System.Photo.ExposureTime",
                "System.Photo.FNumber",
                "System.Photo.ISOSpeed",
                "System.Photo.FocalLength",
                "System.Photo.LensModel"
            };
            var metadata = await file.Properties.RetrievePropertiesAsync(metadataKeys);

            double? Number(string key)
            {
                if (!metadata.TryGetValue(key, out var value) || value is null) return null;
                return MetadataFormatter.TryReadDouble(value, out var number) ? number : null;
            }

            string? Text(string key)
            {
                return metadata.TryGetValue(key, out var value)
                    ? MetadataFormatter.FormatPropertyValue(value)
                    : null;
            }

            if (metadata.TryGetValue("System.Image.ColorSpace", out var colorSpace) && colorSpace is not null)
            {
                rows.Add(("Color Space", MetadataFormatter.FormatColorSpace(colorSpace)));
            }
            if (Number("System.Photo.ExposureTime") is double exposureTime && exposureTime > 0)
            {
                rows.Add(("Exposure Time", MetadataFormatter.FormatExposureTime(exposureTime)));
            }
            if (Number("System.Photo.FNumber") is double fNumber && fNumber > 0)
            {
                rows.Add(("F-Number", $"f/{fNumber:0.0}"));
            }
            if (Number("System.Photo.ISOSpeed") is double iso && iso > 0)
            {
                rows.Add(("ISO", $"{iso:0}"));
            }
            if (Number("System.Photo.FocalLength") is double focalLength && focalLength > 0)
            {
                rows.Add(("Focal Length", $"{focalLength:0}mm"));
            }
            if (Text("System.Photo.LensModel") is { } lensModel)
            {
                rows.Add(("Lens", lensModel));
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
            if (!string.IsNullOrWhiteSpace(imageProperties.CameraManufacturer))
            {
                rows.Add(("Make", imageProperties.CameraManufacturer));
            }
            if (!string.IsNullOrWhiteSpace(imageProperties.CameraModel))
            {
                rows.Add(("Model", imageProperties.CameraModel));
            }
        }
        catch
        {
            if (!rows.Any(row => row.Title == "File Size"))
            {
                rows.Add(("File Size", MetadataFormatter.FormatFileSize(info.Length)));
            }
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
        CancelCrop();
        CancelRename();
        PhotoImage.Source = null;
        WelcomePanel.Visibility = Visibility.Visible;
        TopHeader.Visibility = Visibility.Collapsed;
        InfoSidebar.Visibility = Visibility.Collapsed;
        Title = "Glass Photo Viewer";
        _photos = null;
    }

    private bool KeyboardCommandIsBlocked => _isRenaming || _isDeleteConfirmationVisible || _isCropping;

    private async void Previous_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos?.MovePrevious() != true) return;
        args.Handled = true;
        await DisplayCurrentPhotoAsync();
    }

    private async void Next_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos?.MoveNext() != true) return;
        args.Handled = true;
        await DisplayCurrentPhotoAsync();
    }

    private async void RotateLeft_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        await RotateCurrentAsync(BitmapRotation.Clockwise270Degrees);
    }

    private async void RotateRight_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        await RotateCurrentAsync(BitmapRotation.Clockwise90Degrees);
    }

    private void ToggleFit_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        _isFitToWindow = !_isFitToWindow;
        PhotoImage.Stretch = _isFitToWindow ? Stretch.Uniform : Stretch.None;
    }

    private async void Rename_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_isCropping && !_isSavingCrop)
        {
            args.Handled = true;
            await CommitCropAsync();
            return;
        }
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        BeginRename();
    }

    private void RenameOnly_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        BeginRename();
    }

    private async void Delete_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        await DeleteCurrentAsync();
    }

    private void Crop_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        BeginCrop();
    }

    private void Info_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked || _photos is null) return;
        args.Handled = true;
        ToggleInfoSidebar();
    }

    private void FullScreen_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked) return;
        args.Handled = true;
        ToggleFullScreen();
    }

    private void Escape_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private async void OpenFolder_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (KeyboardCommandIsBlocked) return;
        args.Handled = true;
        await OpenFolderAsync();
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
