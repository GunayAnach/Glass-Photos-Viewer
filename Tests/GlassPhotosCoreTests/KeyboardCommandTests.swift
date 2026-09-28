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
}
