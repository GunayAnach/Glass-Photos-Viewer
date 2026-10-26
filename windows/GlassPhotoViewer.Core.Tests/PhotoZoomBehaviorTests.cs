namespace GlassPhotoViewer.Core.Tests;

public sealed class PhotoZoomBehaviorTests
{
    [Fact]
    public void WheelForwardIncreasesZoom()
    {
        Assert.True(PhotoZoomBehavior.AdjustedScale(1.0, 1.0) > 1.0);
    }

    [Fact]
    public void WheelBackwardDecreasesZoomButDoesNotGoBelowFitScale()
    {
        var scale = PhotoZoomBehavior.AdjustedScale(2.0, -1.0);

        Assert.InRange(scale, 1.0, 2.0);
        Assert.Equal(1.0, PhotoZoomBehavior.AdjustedScale(1.0, -1.0));
    }

    [Fact]
    public void WheelZoomIsClampedToFiveTimes()
    {
        Assert.Equal(5.0, PhotoZoomBehavior.AdjustedScale(4.9, 100.0));
    }

    [Fact]
    public void ZeroDeltaDoesNotChangeZoom()
    {
        Assert.Equal(1.75, PhotoZoomBehavior.AdjustedScale(1.75, 0.0));
    }

    [Fact]
    public void PointerZoomKeepsHoveredImagePointStationary()
    {
        const double currentOffset = 12;
        const double currentScale = 1.5;
        const double updatedScale = 2.0;
        const double pointer = 35;
        const double center = 100;

        var updatedOffset = PhotoZoomBehavior.OffsetForPointer(
            currentOffset, currentScale, updatedScale, pointer, center);
        var imagePointBefore = (pointer - center - currentOffset) / currentScale;
        var pointerAfter = center + updatedOffset + imagePointBefore * updatedScale;

        Assert.Equal(pointer, pointerAfter, precision: 4);
    }

    [Fact]
    public void PointerHitTestingRejectsLetterboxAndOutsideImagePoints()
    {
        Assert.False(PhotoZoomBehavior.IsPointerInsideDisplayedImage(
            250, 50, 400, 200, 500, 500, 1, 0, 0, fitToWindow: true));
        Assert.True(PhotoZoomBehavior.IsPointerInsideDisplayedImage(
            250, 250, 400, 200, 500, 500, 1, 0, 0, fitToWindow: true));
        Assert.False(PhotoZoomBehavior.IsPointerInsideDisplayedImage(
            50, 250, 400, 200, 500, 500, 1, 100, 0, fitToWindow: true));
    }

    [Fact]
    public void LetterboxedAxisStaysCenteredWhenZoomCreatesOverflowOnTheOtherAxis()
    {
        var proposedOffset = PhotoZoomBehavior.OffsetForPointer(0, 1, 1.12, 100, 250);

        Assert.NotEqual(0, proposedOffset);
        Assert.Equal(0, PhotoZoomBehavior.ClampedOffset(proposedOffset, viewportSize: 500, displayedImageSize: 280));
    }

    [Fact]
    public void OffsetClampsToDisplayedImageBounds()
    {
        Assert.Equal(100, PhotoZoomBehavior.ClampedOffset(300, viewportSize: 100, displayedImageSize: 300));
        Assert.Equal(0, PhotoZoomBehavior.ClampedOffset(30, viewportSize: 100, displayedImageSize: 80));
    }

    [Fact]
    public void MouseDragMovesZoomedImageByPointerDelta()
    {
        Assert.Equal(40, PhotoZoomBehavior.OffsetAfterDrag(
            currentOffset: 0, dragDelta: 40, viewportSize: 200, displayedImageSize: 400));
    }

    [Fact]
    public void MouseDragCannotPanPastImageEdgesOrPanAnAxisWithoutOverflow()
    {
        Assert.Equal(100, PhotoZoomBehavior.OffsetAfterDrag(
            currentOffset: 90, dragDelta: 50, viewportSize: 200, displayedImageSize: 400));
        Assert.Equal(0, PhotoZoomBehavior.OffsetAfterDrag(
            currentOffset: 0, dragDelta: 50, viewportSize: 300, displayedImageSize: 200));
    }
}
