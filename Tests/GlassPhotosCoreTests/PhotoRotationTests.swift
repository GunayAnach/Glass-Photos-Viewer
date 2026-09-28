import XCTest
@testable import GlassPhotosCore

final class PhotoRotationTests: XCTestCase {
    func testClockwiseRotationAdvancesByNinetyDegrees() {
        var rotation = PhotoRotation()
        rotation.rotateClockwise()
        XCTAssertEqual(rotation.degrees, 90)
    }

    func testCounterClockwiseRotationWrapsToTwoHundredSeventyDegrees() {
        var rotation = PhotoRotation()
        rotation.rotateCounterClockwise()
        XCTAssertEqual(rotation.degrees, 270)
    }

    func testFourClockwiseRotationsReturnToZero() {
        var rotation = PhotoRotation()
        for _ in 0..<4 { rotation.rotateClockwise() }
        XCTAssertEqual(rotation.degrees, 0)
    }
}
