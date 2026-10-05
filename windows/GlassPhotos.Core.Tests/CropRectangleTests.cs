using GlassPhotos.Core;

namespace GlassPhotos.Core.Tests;

public sealed class CropRectangleTests
{
    [Fact]
    public void ConvertsNormalizedSelectionToPixelBounds()
    {
        var crop = new NormalizedCropRect(0.1, 0.25, 0.5, 0.5);

        var pixels = crop.ToPixels(100, 80);

        Assert.Equal(new PixelCropRect(10, 20, 50, 40), pixels);
    }

    [Fact]
    public void ClampsSelectionInsideImageAndKeepsMinimumSize()
    {
        var crop = new NormalizedCropRect(-0.2, 0.95, 0.01, 0.3).Clamp(0.05);

        Assert.Equal(0, crop.X);
        Assert.Equal(0.95, crop.Y, precision: 6);
        Assert.Equal(0.05, crop.Width, precision: 6);
        Assert.Equal(0.05, crop.Height, precision: 6);
    }
}
