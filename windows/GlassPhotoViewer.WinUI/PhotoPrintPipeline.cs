using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using GlassPhotoViewer.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace GlassPhotoViewer.WinUI;

internal sealed class PhotoPrintPipeline
{
    private const uint DmOrientation = 0x00000001;
    private const uint DmInBuffer = 0x00000008;
    private const uint DmOutBuffer = 0x00000002;
    private const uint DmInPrompt = 0x00000004;
    private const int IdOk = 1;
    private const int IdCancel = 2;
    private const int HorzRes = 8;
    private const int VertRes = 10;

    private readonly nint _windowHandle;
    private readonly string _imagePath;

    public PhotoPrintPipeline(nint windowHandle, string imagePath)
    {
        _windowHandle = windowHandle;
        _imagePath = imagePath;
    }

    public async Task ShowAsync()
    {
        var photo = await LoadOrientedPngAsync(_imagePath);
        var printer = await RunOnStaThreadAsync(
            () => ShowDefaultPrinterDriverDialog(photo.IsLandscape));
        if (printer is null) return;

        await Task.Run(() => PrintToDevice(printer, photo.PngBytes, Path.GetFileName(_imagePath)));
    }

    private static Task<T> RunOnStaThreadAsync<T>(Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "Glass Photo Viewer printer settings"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static async Task<DecodedPhoto> LoadOrientedPngAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        using var input = await file.OpenReadAsync();
        using var decoder = await BitmapDecoder.CreateAsync(input);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.ColorManageToSRgb);

        using var output = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();

        output.Seek(0);
        using var outputReader = output.AsStreamForRead();
        using var png = new MemoryStream();
        await outputReader.CopyToAsync(png);

        return new DecodedPhoto(
            png.ToArray(),
            decoder.OrientedPixelWidth > decoder.OrientedPixelHeight);
    }

    private PrinterJobSettings? ShowDefaultPrinterDriverDialog(bool landscape)
    {
        var printerName = GetDefaultPrinterName();
        if (!OpenPrinter(printerName, out var printerHandle, nint.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the default printer.");
        }

        try
        {
            var size = DocumentProperties(_windowHandle, printerHandle, printerName, nint.Zero, nint.Zero, 0);
            if (size <= 0)
            {
                throw new InvalidOperationException("The default printer did not provide print settings.");
            }

            var defaultMode = Marshal.AllocHGlobal(size);
            var selectedMode = Marshal.AllocHGlobal(size);
            try
            {
                var result = DocumentProperties(
                    _windowHandle, printerHandle, printerName, defaultMode, nint.Zero, DmOutBuffer);
                if (result != IdOk)
                {
                    throw new InvalidOperationException("Could not read the default printer settings.");
                }

                var mode = Marshal.PtrToStructure<DevModeHeader>(defaultMode);
                mode.Fields |= DmOrientation;
                mode.Orientation = (short)(landscape ? 2 : 1);
                Marshal.StructureToPtr(mode, defaultMode, fDeleteOld: false);

                result = DocumentProperties(
                    _windowHandle,
                    printerHandle,
                    printerName,
                    selectedMode,
                    defaultMode,
                    DmInBuffer | DmOutBuffer | DmInPrompt);

                if (result == IdCancel) return null;
                if (result != IdOk)
                {
                    throw new InvalidOperationException("The printer settings dialog could not be opened.");
                }

                var devModeBytes = new byte[size];
                Marshal.Copy(selectedMode, devModeBytes, 0, size);
                return new PrinterJobSettings(printerName, devModeBytes);
            }
            finally
            {
                Marshal.FreeHGlobal(selectedMode);
                Marshal.FreeHGlobal(defaultMode);
            }
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    private static string GetDefaultPrinterName()
    {
        uint characterCount = 0;
        _ = GetDefaultPrinter(null, ref characterCount);
        if (characterCount == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No default printer is configured.");
        }

        var name = new StringBuilder((int)characterCount);
        if (!GetDefaultPrinter(name, ref characterCount))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the default printer name.");
        }

        return name.ToString();
    }

    private static void PrintToDevice(PrinterJobSettings printer, byte[] pngBytes, string documentName)
    {
        var devMode = Marshal.AllocHGlobal(printer.DevMode.Length);
        Marshal.Copy(printer.DevMode, 0, devMode, printer.DevMode.Length);
        try
        {
            var deviceContext = CreateDC("WINSPOOL", printer.Name, null, devMode);
            if (deviceContext == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not prepare the selected printer.");
            }

            try
            {
                PrintPage(deviceContext, pngBytes, documentName);
            }
            finally
            {
                DeleteDC(deviceContext);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(devMode);
        }
    }

    private static void PrintPage(nint deviceContext, byte[] pngBytes, string documentName)
    {
        using var stream = new MemoryStream(pngBytes, writable: false);
        using var source = Image.FromStream(stream);
        using var image = new Bitmap(source);

        var width = GetDeviceCaps(deviceContext, HorzRes);
        var height = GetDeviceCaps(deviceContext, VertRes);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The printer returned an invalid printable page size.");
        }

        var info = new DocInfo
        {
            Size = Marshal.SizeOf<DocInfo>(),
            DocumentName = documentName
        };
        if (StartDoc(deviceContext, ref info) <= 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "The printer could not start the print job.");
        }

        try
        {
            if (StartPage(deviceContext) <= 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The printer could not start the page.");
            }

            using (var graphics = Graphics.FromHdc(deviceContext))
            {
                graphics.PageUnit = GraphicsUnit.Pixel;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.Clear(Color.White);

                var scale = Math.Min((double)width / image.Width, (double)height / image.Height);
                var drawWidth = Math.Max(1, (int)Math.Round(image.Width * scale));
                var drawHeight = Math.Max(1, (int)Math.Round(image.Height * scale));
                var destination = new Rectangle(
                    (width - drawWidth) / 2,
                    (height - drawHeight) / 2,
                    drawWidth,
                    drawHeight);
                graphics.DrawImage(image, destination);
            }

            if (EndPage(deviceContext) <= 0 || EndDoc(deviceContext) <= 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The printer could not finish the print job.");
            }
        }
        catch
        {
            AbortDoc(deviceContext);
            throw;
        }
    }

    private sealed record DecodedPhoto(byte[] PngBytes, bool IsLandscape);
    private sealed record PrinterJobSettings(string Name, byte[] DevMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
    private struct DevModeHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;
        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public short Orientation;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DocInfo
    {
        public int Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string? DocumentName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Output;
        [MarshalAs(UnmanagedType.LPWStr)] public string? DataType;
        public uint Type;
    }

    [DllImport("winspool.drv", EntryPoint = "GetDefaultPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDefaultPrinter(StringBuilder? buffer, ref uint characterCount);

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenPrinter(string printerName, out nint printerHandle, nint defaults);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClosePrinter(nint printerHandle);

    [DllImport("winspool.drv", EntryPoint = "DocumentPropertiesW", CharSet = CharSet.Unicode)]
    private static extern int DocumentProperties(
        nint owner,
        nint printerHandle,
        string printerName,
        nint outputMode,
        nint inputMode,
        uint mode);

    [DllImport("gdi32.dll", EntryPoint = "CreateDCW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateDC(string driver, string device, string? output, nint devMode);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(nint deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetDeviceCaps(nint deviceContext, int index);

    [DllImport("gdi32.dll", EntryPoint = "StartDocW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int StartDoc(nint deviceContext, ref DocInfo info);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int StartPage(nint deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int EndPage(nint deviceContext);

    [DllImport("gdi32.dll", EntryPoint = "EndDoc", SetLastError = true)]
    private static extern int EndDoc(nint deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int AbortDoc(nint deviceContext);
}
