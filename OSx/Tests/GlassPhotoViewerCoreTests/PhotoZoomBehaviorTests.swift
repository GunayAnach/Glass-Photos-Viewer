import XCTest
@testable import GlassPhotoViewerCore

final class PhotoZoomBehaviorTests: XCTestCase {
    func testScrollingUpIncreasesZoom() {
        let scale = PhotoZoomBehavior.adjustedScale(current: 1.0, scrollDelta: 1.0)
        XCTAssertGreaterThan(scale, 1.0)
    }

    func testScrollingDownDecreasesZoomButDoesNotGoBelowFitScale() {
        let scale = PhotoZoomBehavior.adjustedScale(current: 2.0, scrollDelta: -1.0)
        XCTAssertLessThan(scale, 2.0)
        XCTAssertGreaterThanOrEqual(scale, 1.0)
        XCTAssertEqual(PhotoZoomBehavior.adjustedScale(current: 1.0, scrollDelta: -1.0), 1.0)
    }

    func testWheelZoomIsClampedToFiveTimes() {
        XCTAssertEqual(PhotoZoomBehavior.adjustedScale(current: 4.9, scrollDelta: 100), 5.0)
    }

    func testZeroDeltaDoesNotChangeZoom() {
        XCTAssertEqual(PhotoZoomBehavior.adjustedScale(current: 1.75, scrollDelta: 0), 1.75)
    }

    func testPointerZoomKeepsHoveredImagePointStationary() {
        let currentOffset: CGFloat = 12
        let currentScale: CGFloat = 1.5
        let updatedScale: CGFloat = 2.0
        let pointer: CGFloat = 35
        let center: CGFloat = 100

        let updatedOffset = PhotoZoomBehavior.offsetForPointer(
            currentOffset: currentOffset,
            currentScale: currentScale,
            updatedScale: updatedScale,
            pointerPosition: pointer,
            viewportCenter: center
        )
        let imagePointBefore = (pointer - center - currentOffset) / currentScale
        let pointerAfter = center + updatedOffset + imagePointBefore * updatedScale

        XCTAssertEqual(pointerAfter, pointer, accuracy: 0.0001)
    }

    func testPointerHitTestingRejectsLetterboxAndOutsideImagePoints() {
        let imageSize = CGSize(width: 400, height: 200)
        let viewportSize = CGSize(width: 500, height: 500)

        XCTAssertFalse(PhotoZoomBehavior.isPointerInsideDisplayedImage(
            point: CGPoint(x: 250, y: 50), imageSize: imageSize, viewportSize: viewportSize,
            scale: 1, offset: .zero, fitToWindow: true
        ))
        XCTAssertTrue(PhotoZoomBehavior.isPointerInsideDisplayedImage(
            point: CGPoint(x: 250, y: 250), imageSize: imageSize, viewportSize: viewportSize,
            scale: 1, offset: .zero, fitToWindow: true
        ))
        XCTAssertFalse(PhotoZoomBehavior.isPointerInsideDisplayedImage(
            point: CGPoint(x: 50, y: 250), imageSize: imageSize, viewportSize: viewportSize,
            scale: 1, offset: CGSize(width: 100, height: 0), fitToWindow: true
        ))
    }

    func testLetterboxedAxisStaysCenteredWhenZoomCreatesOverflowOnTheOtherAxis() {
        let proposedOffset = PhotoZoomBehavior.offsetForPointer(
            currentOffset: 0, currentScale: 1, updatedScale: 1.12,
            pointerPosition: 100, viewportCenter: 250
        )
        XCTAssertNotEqual(proposedOffset, 0)
        XCTAssertEqual(PhotoZoomBehavior.clampedOffset(proposedOffset, viewportSize: 500, displayedImageSize: 280), 0)
    }

    func testOffsetClampsToDisplayedImageBounds() {
        XCTAssertEqual(PhotoZoomBehavior.clampedOffset(300, viewportSize: 100, displayedImageSize: 300), 100)
        XCTAssertEqual(PhotoZoomBehavior.clampedOffset(30, viewportSize: 100, displayedImageSize: 80), 0)
    }

    func testMouseDragMovesZoomedImageByPointerDelta() {
        XCTAssertEqual(
            PhotoZoomBehavior.offsetAfterDrag(
                currentOffset: 0,
                dragDelta: 40,
                viewportSize: 200,
                displayedImageSize: 400
            ),
            40
        )
    }

    func testMouseDragCannotPanPastImageEdgesOrPanAnAxisWithoutOverflow() {
        XCTAssertEqual(
            PhotoZoomBehavior.offsetAfterDrag(
                currentOffset: 90,
                dragDelta: 50,
                viewportSize: 200,
                displayedImageSize: 400
            ),
            100
        )
        XCTAssertEqual(
            PhotoZoomBehavior.offsetAfterDrag(
                currentOffset: 0,
                dragDelta: 50,
                viewportSize: 300,
                displayedImageSize: 200
            ),
            0
        )
    }
}
