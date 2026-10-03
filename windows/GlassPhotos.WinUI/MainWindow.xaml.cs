using GlassPhotos.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace GlassPhotos.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly string? _initialPath;
    private PhotoCollection? _photos;
    private long _loadGeneration;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        _initialPath = initialPath;
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

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".heic");
        picker.FileTypeFilter.Add(".heif");
        picker.FileTypeFilter.Add(".tif");
        picker.FileTypeFilter.Add(".tiff");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".dng");
        picker.FileTypeFilter.Add(".nef");
        picker.FileTypeFilter.Add(".cr2");
        picker.FileTypeFilter.Add(".arw");
        picker.FileTypeFilter.Add(".raf");

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await OpenPhotoAsync(file.Path);
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

        var generation = Interlocked.Increment(ref _loadGeneration);
        var path = _photos.CurrentPath;
        var file = await StorageFile.GetFileFromPathAsync(path);
        await using var stream = await file.OpenStreamForReadAsync();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

        if (generation != _loadGeneration) return;

        PhotoImage.Source = bitmap;
        WelcomePanel.Visibility = Visibility.Collapsed;
        FileNameText.Text = Path.GetFileName(path);
        ToolTipService.SetToolTip(FileNameText, path);
        PositionText.Text = $"{_photos.CurrentIndex + 1} / {_photos.Files.Count}";
        PreviousButton.IsEnabled = _photos.CurrentIndex > 0;
        NextButton.IsEnabled = _photos.CurrentIndex < _photos.Files.Count - 1;
        Title = $"{Path.GetFileName(path)} — Glass Photos";
    }

    private async void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (_photos?.MovePrevious() == true)
        {
            await DisplayCurrentPhotoAsync();
        }
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_photos?.MoveNext() == true)
        {
            await DisplayCurrentPhotoAsync();
        }
    }

    private async void Window_KeyDown(object sender, KeyRoutedEventArgs e)
    {
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
