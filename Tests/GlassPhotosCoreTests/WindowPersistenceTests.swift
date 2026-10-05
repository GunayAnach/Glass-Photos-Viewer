import AppKit
import XCTest
@testable import GlassPhotosCore

final class WindowPersistenceTests: XCTestCase {
    func testMainWindowUsesStableAutosaveName() {
        XCTAssertEqual(WindowPersistence.autosaveName, "GlassPhotos.MainWindow")
    }

    @MainActor
    func testSavedFrameIsRestoredInsteadOfApplyingTheDefaultSize() {
        let autosaveName = "GlassPhotos.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let expectedFrame = NSRect(x: 140, y: 120, width: 760, height: 520)
        let source = makeWindow()
        source.setFrame(expectedFrame, display: false)
        source.saveFrame(usingName: autosaveName)

        let restoredWindow = makeWindow()
        XCTAssertTrue(WindowPersistence.configure(restoredWindow, autosaveName: autosaveName))
        XCTAssertEqual(restoredWindow.frame.origin.x, expectedFrame.origin.x, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.origin.y, expectedFrame.origin.y, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.width, expectedFrame.size.width, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.height, expectedFrame.size.height, accuracy: 1)
    }

    @MainActor
    func testDefaultSizeIsAppliedOnlyWithoutASavedFrame() {
        let autosaveName = "GlassPhotos.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let window = makeWindow()
        XCTAssertFalse(WindowPersistence.configure(window, autosaveName: autosaveName))
        guard let contentSize = window.contentView?.frame.size else {
            return XCTFail("Window has no content view")
        }
        XCTAssertEqual(contentSize.width, WindowPersistence.defaultContentSize.width, accuracy: 1)
        XCTAssertEqual(contentSize.height, WindowPersistence.defaultContentSize.height, accuracy: 1)
    }

    @MainActor
    private func makeWindow() -> NSWindow {
        NSWindow(
            contentRect: NSRect(x: 40, y: 40, width: 420, height: 320),
            styleMask: [.titled, .closable, .resizable],
            backing: .buffered,
            defer: false
        )
    }
}
