namespace GlassPhotoViewer.Core;

public readonly record struct PixelCropRect(int X, int Y, int Width, int Height);

public readonly record struct NormalizedCropRect(double X, double Y, double Width, double Height)
{
    public NormalizedCropRect Clamp(double minimumSize = 0.05)
    {
        minimumSize = Math.Clamp(minimumSize, 0.001, 1);
        var x = Math.Clamp(X, 0, 1 - minimumSize);
        var y = Math.Clamp(Y, 0, 1 - minimumSize);
        var width = Math.Clamp(Width, minimumSize, 1 - x);
        var height = Math.Clamp(Height, minimumSize, 1 - y);
        return new NormalizedCropRect(x, y, width, height);
    }

    public PixelCropRect ToPixels(int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "Image dimensions must be positive.");

        var crop = Clamp();
        var x = Math.Clamp((int)Math.Round(crop.X * imageWidth), 0, imageWidth - 1);
        var y = Math.Clamp((int)Math.Round(crop.Y * imageHeight), 0, imageHeight - 1);
        var width = Math.Clamp((int)Math.Round(crop.Width * imageWidth), 1, imageWidth - x);
        var height = Math.Clamp((int)Math.Round(crop.Height * imageHeight), 1, imageHeight - y);
        return new PixelCropRect(x, y, width, height);
    }
}
