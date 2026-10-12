using GlassPhotoViewer.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using WinRT.Interop;

namespace GlassPhotoViewer.WinUI;

internal sealed class PhotoPrintPipeline
{
    private readonly nint _windowHandle;
    private readonly string _imagePath;
    private PrintManager? _printManager;
    private PrintDocument? _document;
    private IPrintDocumentSource? _documentSource;
    private BitmapImage? _image;

    public PhotoPrintPipeline(nint windowHandle, string imagePath)
    {
        _windowHandle = windowHandle;
        _imagePath = imagePath;
    }

    public async Task ShowAsync()
    {
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_imagePath);
        using (var stream = await file.OpenReadAsync())
        {
            _image = new BitmapImage();
            await _image.SetSourceAsync(stream);
        }

        _printManager = PrintManagerInterop.GetForWindow(_windowHandle);
        _document = new PrintDocument();
        _documentSource = _document.DocumentSource;
        _document.Paginate += Paginate;
        _document.GetPreviewPage += GetPreviewPage;
        _document.AddPages += AddPages;
        _printManager.PrintTaskRequested += PrintTaskRequested;
        try
        {
            await PrintManagerInterop.ShowPrintUIForWindowAsync(_windowHandle);
        }
        finally
        {
            _printManager.PrintTaskRequested -= PrintTaskRequested;
        }
    }

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        var task = args.Request.CreatePrintTask("Print Photo", request => request.SetSource(_documentSource));
        task.Options.Orientation = PhotoPrintLayout.Orientation(_image!.PixelWidth, _image.PixelHeight)
            == PrintOrientation.Landscape ? PrintOrientation.Landscape : PrintOrientation.Portrait;
    }

    private void Paginate(object? sender, PaginateEventArgs e)
    {
        var description = e.PrintTaskOptions.GetPageDescription(0);
        _document!.SetPreviewPage(0, new Image
        {
            Source = _image,
            Stretch = Stretch.Uniform,
            Width = description.PageSize.Width,
            Height = description.PageSize.Height
        });
        _document.SetPreviewPageCount(1, PreviewPageCountType.Final);
    }

    private void GetPreviewPage(object? sender, GetPreviewPageEventArgs e) { }

    private void AddPages(object? sender, AddPagesEventArgs e)
    {
        _document!.AddPage(new Image
        {
            Source = _image,
            Stretch = Stretch.Uniform,
            Width = e.PrintTaskOptions.GetPageDescription(0).PageSize.Width,
            Height = e.PrintTaskOptions.GetPageDescription(0).PageSize.Height
        });
        _document.AddPagesComplete();
    }
}
