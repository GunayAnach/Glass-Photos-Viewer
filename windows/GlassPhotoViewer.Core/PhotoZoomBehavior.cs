namespace GlassPhotoViewer.Core;

public static class PhotoZoomBehavior
{
    public const double MinimumScale = 1.0;
    public const double MaximumScale = 5.0;

    public static double AdjustedScale(double currentScale, double scrollSteps)
    {
        if (!double.IsFinite(currentScale)) return MinimumScale;
        var boundedCurrent = Math.Clamp(currentScale, MinimumScale, MaximumScale);
        if (!double.IsFinite(scrollSteps) || scrollSteps == 0) return boundedCurrent;

        var adjusted = boundedCurrent * Math.Pow(1.12, scrollSteps);
        return Math.Clamp(adjusted, MinimumScale, MaximumScale);
    }

    public static double OffsetForPointer(
        double currentOffset,
        double currentScale,
        double updatedScale,
        double pointerPosition,
        double viewportCenter)
    {
        if (!double.IsFinite(currentOffset) || !double.IsFinite(currentScale) || currentScale <= 0 ||
            !double.IsFinite(updatedScale)) return currentOffset;
        var pointerFromCenter = pointerPosition - viewportCenter;
        return pointerFromCenter - (pointerFromCenter - currentOffset) * (updatedScale / currentScale);
    }

    public static double ClampedOffset(double offset, double viewportSize, double displayedImageSize)
    {
        if (!double.IsFinite(offset) || !double.IsFinite(viewportSize) ||
            !double.IsFinite(displayedImageSize)) return 0;
        var maximumOffset = Math.Max(0, (displayedImageSize - viewportSize) / 2);
        return Math.Clamp(offset, -maximumOffset, maximumOffset);
    }

    public static bool IsPointerInsideDisplayedImage(
        double pointX,
        double pointY,
        double imageWidth,
        double imageHeight,
        double viewportWidth,
        double viewportHeight,
        double scale,
        double offsetX,
        double offsetY,
        bool fitToWindow)
    {
        if (!double.IsFinite(pointX) || !double.IsFinite(pointY) ||
            !double.IsFinite(imageWidth) || !double.IsFinite(imageHeight) ||
            !double.IsFinite(viewportWidth) || !double.IsFinite(viewportHeight) ||
            !double.IsFinite(scale) || !double.IsFinite(offsetX) || !double.IsFinite(offsetY) ||
            imageWidth <= 0 || imageHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0 || scale <= 0)
            return false;

        var baseScale = fitToWindow
            ? Math.Min(viewportWidth / imageWidth, viewportHeight / imageHeight)
            : 1;
        var displayedWidth = imageWidth * baseScale * scale;
        var displayedHeight = imageHeight * baseScale * scale;
        var left = (viewportWidth - displayedWidth) / 2 + offsetX;
        var top = (viewportHeight - displayedHeight) / 2 + offsetY;
        return pointX >= left && pointX <= left + displayedWidth &&
               pointY >= top && pointY <= top + displayedHeight;
    }
}
