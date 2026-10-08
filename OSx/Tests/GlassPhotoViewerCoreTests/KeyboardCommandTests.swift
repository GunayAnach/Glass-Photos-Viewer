import XCTest
@testable import GlassPhotoViewerCore

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

    func testCropAndDeleteShortcutsMatchAcrossPlatforms() {
        XCTAssertEqual(
            KeyboardCommand.resolve(
                keyCode: 8,
                charactersIgnoringModifiers: "c",
                isRenaming: false,
                isDeleteConfirmationVisible: false
            ),
            .beginCrop
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 117, isRenaming: false, isDeleteConfirmationVisible: false),
            .delete
        )
    }

    func testEscapeRequestsApplicationExitEvenDuringEditingStates() {
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 53, isRenaming: false, isDeleteConfirmationVisible: false),
            .exitApplication
        )
        XCTAssertEqual(
            KeyboardCommand.resolve(keyCode: 53, isRenaming: true, isDeleteConfirmationVisible: true),
            .exitApplication
        )
    }

    func testMacToolbarPlacesFullScreenBeforeDeleteAndRoutesEscapeToExit() throws {
        let sourceURL = URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent()
            .deletingLastPathComponent()
            .deletingLastPathComponent()
            .appendingPathComponent("GlassPhotoViewer/GlassPhotoViewerApp.swift")
        let source = try String(contentsOf: sourceURL, encoding: .utf8)
        let fullScreenIndex = try XCTUnwrap(source.range(of: "Button(action: onFullScreen)")?.lowerBound)
        let deleteIndex = try XCTUnwrap(source.range(of: "Button(action: onDelete)")?.lowerBound)

        XCTAssertLessThan(fullScreenIndex, deleteIndex)
        XCTAssertTrue(source.contains("case .exitApplication:"))
        XCTAssertTrue(source.contains("NSApp.terminate(nil)"))
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
