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

    func testF2AlsoStartsRename() {
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 120, isRenaming: false, isDeleteConfirmationVisible: false),
            .beginRename
        )
    }

    func testInfoAndFullScreenShortcutsMatchAcrossPlatforms() {
        XCTAssertEqual(
            KeyboardCommand.resolve(
                keyCode: 34,
                charactersIgnoringModifiers: "i",
                isRenaming: false,
                isDeleteConfirmationVisible: false
            ),
            .toggleInfo
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(
                keyCode: 3,
                charactersIgnoringModifiers: "f",
                isRenaming: false,
                isDeleteConfirmationVisible: false
            ),
            .toggleFullScreen
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 103, isRenaming: false, isDeleteConfirmationVisible: false),
            .toggleFullScreen
        )
    }

    func testLetterShortcutsFollowTheTypedCharacterAndIgnoreCommandCombos() {
        XCTAssertEqual(
            KeyboardCommand.resolve(
                keyCode: 99,
                charactersIgnoringModifiers: "I",
                isRenaming: false,
                isDeleteConfirmationVisible: false
            ),
            .toggleInfo
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(
                keyCode: 34,
                charactersIgnoringModifiers: "i",
                hasDisallowedModifiers: true,
                isRenaming: false,
                isDeleteConfirmationVisible: false
            ),
            .passThrough
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
