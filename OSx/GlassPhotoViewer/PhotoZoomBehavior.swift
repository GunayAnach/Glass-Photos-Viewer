import Foundation

public enum PhotoZoomBehavior {
    public static let minimumScale: CGFloat = 1.0
    public static let maximumScale: CGFloat = 5.0

    public static func adjustedScale(current: CGFloat, scrollDelta: CGFloat) -> CGFloat {
        guard current.isFinite else { return minimumScale }
        let boundedCurrent = min(max(current, minimumScale), maximumScale)
        guard scrollDelta.isFinite, scrollDelta != 0 else { return boundedCurrent }

        let adjusted = Double(boundedCurrent) * pow(1.12, Double(scrollDelta))
        return CGFloat(min(max(adjusted, Double(minimumScale)), Double(maximumScale)))
    }

    public static func offsetForPointer(
        currentOffset: CGFloat,
        currentScale: CGFloat,
        updatedScale: CGFloat,
        pointerPosition: CGFloat,
        viewportCenter: CGFloat
    ) -> CGFloat {
        guard currentOffset.isFinite, currentScale.isFinite, currentScale > 0,
              updatedScale.isFinite else { return currentOffset }
        let pointerFromCenter = pointerPosition - viewportCenter
        return pointerFromCenter - (pointerFromCenter - currentOffset) * (updatedScale / currentScale)
    }

    public static func clampedOffset(
        _ offset: CGFloat,
        viewportSize: CGFloat,
        displayedImageSize: CGFloat
    ) -> CGFloat {
        guard offset.isFinite, viewportSize.isFinite, displayedImageSize.isFinite else { return 0 }
        let maximumOffset = max(0, (displayedImageSize - viewportSize) / 2)
        return min(max(offset, -maximumOffset), maximumOffset)
    }

    public static func isPointerInsideDisplayedImage(
        point: CGPoint,
        imageSize: CGSize,
        viewportSize: CGSize,
        scale: CGFloat,
        offset: CGSize,
        fitToWindow: Bool
    ) -> Bool {
        guard point.x.isFinite, point.y.isFinite,
              imageSize.width.isFinite, imageSize.height.isFinite,
              viewportSize.width.isFinite, viewportSize.height.isFinite,
              scale.isFinite, offset.width.isFinite, offset.height.isFinite,
              imageSize.width > 0, imageSize.height > 0,
              viewportSize.width > 0, viewportSize.height > 0, scale > 0 else { return false }

        let baseScale = fitToWindow
            ? min(viewportSize.width / imageSize.width, viewportSize.height / imageSize.height)
            : 1
        let displayedSize = CGSize(
            width: imageSize.width * baseScale * scale,
            height: imageSize.height * baseScale * scale
        )
        let displayedRect = CGRect(
            x: (viewportSize.width - displayedSize.width) / 2 + offset.width,
            y: (viewportSize.height - displayedSize.height) / 2 + offset.height,
            width: displayedSize.width,
            height: displayedSize.height
        )
        return displayedRect.contains(point)
    }
}
