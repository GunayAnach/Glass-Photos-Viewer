import XCTest
@testable import GlassPhotosCore

final class KeyboardCommandTests: XCTestCase {
    func testReturnIsPassedToActiveDeleteConfirmationInsteadOfStartingRename() {
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 36, isRenaming: false, isDeleteConfirmationVisible: true),
            .passThrough
        )
    }

    func testReturnStartsRenameWhenNoDeleteConfirmationIsVisible() {
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 36, isRenaming: false, isDeleteConfirmationVisible: false),
            .beginRename
        )
    }

    func testUpRotatesCounterClockwiseAndDownRotatesClockwise() {
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 126, isRenaming: false, isDeleteConfirmationVisible: false),
            .rotateCounterClockwise
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 125, isRenaming: false, isDeleteConfirmationVisible: false),
            .rotateClockwise
        )
    }
}
