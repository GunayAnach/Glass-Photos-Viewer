import AppKit
import XCTest
@testable import GlassPhotoViewerCore

final class WindowPersistenceTests: XCTestCase {
    func testMainWindowUsesStableAutosaveName() {
        XCTAssertEqual(WindowPersistence.autosaveName, "GlassPhotos.MainWindow")
    }

    @MainActor
    func testSavedFrameIsRestoredInsteadOfApplyingTheDefaultSize() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
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
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
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
    func testMalformedStoredFramesAreDiscardedAndDefaultSizeIsApplied() {
        for descriptor in [
            "10 20 300 200",
            "10 20 300 200 0 0 nan 690",
            "10 20 300 200 0 0 1280 inf",
            "10 20 300 200 0 0 1280 690 1"
        ] {
            let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
            let defaultsKey = "NSWindow Frame \(autosaveName)"
            UserDefaults.standard.set(descriptor, forKey: defaultsKey)
            defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

            let window = makeWindow()
            XCTAssertFalse(WindowPersistence.configure(window, autosaveName: autosaveName))
            XCTAssertNil(UserDefaults.standard.string(forKey: defaultsKey))
            guard let contentSize = window.contentView?.frame.size else {
                return XCTFail("Window has no content view")
            }
            XCTAssertEqual(contentSize.width, WindowPersistence.defaultContentSize.width, accuracy: 1)
            XCTAssertEqual(contentSize.height, WindowPersistence.defaultContentSize.height, accuracy: 1)
        }
    }

    @MainActor
    func testTrackerSavesMovedAndResizedFrameUsingStableKeyEvenWhenSwiftUIOwnsAutosaveName() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        let swiftUIAutosaveName = "SwiftUI.Dynamic.Window.\(UUID().uuidString)"
        let swiftUIDefaultsKey = "NSWindow Frame \(swiftUIAutosaveName)"
        defer {
            UserDefaults.standard.removeObject(forKey: defaultsKey)
            UserDefaults.standard.removeObject(forKey: swiftUIDefaultsKey)
        }

        let expectedFrame = NSRect(x: 175, y: 135, width: 840, height: 610)
        let source = makeWindow()
        XCTAssertTrue(source.setFrameAutosaveName(swiftUIAutosaveName))
        let tracker = WindowPersistence.Tracker()
        tracker.configure(source, autosaveName: autosaveName)
        source.setFrame(expectedFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: source)

        XCTAssertNotNil(UserDefaults.standard.string(forKey: defaultsKey))
        let restoredWindow = makeWindow()
        XCTAssertTrue(WindowPersistence.configure(restoredWindow, autosaveName: autosaveName))
        XCTAssertEqual(restoredWindow.frame.origin.x, expectedFrame.origin.x, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.origin.y, expectedFrame.origin.y, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.width, expectedFrame.size.width, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.height, expectedFrame.size.height, accuracy: 1)
    }

    @MainActor
    func testPreferredWindowUsesKeyThenMainThenVisibleWindow() {
        let hiddenWindow = makeWindow()
        hiddenWindow.orderOut(nil)
        let visibleWindow = makeWindow()
        visibleWindow.orderFront(nil)
        let mainWindow = makeWindow()
        let keyWindow = makeWindow()

        XCTAssertTrue(WindowPersistence.preferredWindow(
            keyWindow: keyWindow,
            mainWindow: mainWindow,
            windows: [hiddenWindow, visibleWindow]
        ) === keyWindow)
        XCTAssertTrue(WindowPersistence.preferredWindow(
            keyWindow: nil,
            mainWindow: mainWindow,
            windows: [hiddenWindow, visibleWindow]
        ) === mainWindow)
        XCTAssertTrue(WindowPersistence.preferredWindow(
            keyWindow: nil,
            mainWindow: nil,
            windows: [hiddenWindow, visibleWindow]
        ) === visibleWindow)
    }

    @MainActor
    func testTrackerRejectsPanelsAndTransientWindows() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let tracker = WindowPersistence.Tracker()
        let panel = NSPanel(
            contentRect: NSRect(x: 20, y: 20, width: 300, height: 200),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        tracker.configure(panel, autosaveName: autosaveName)
        NotificationCenter.default.post(name: NSWindow.didMoveNotification, object: panel)

        let parentWindow = makeWindow()
        let transientWindow = makeWindow()
        parentWindow.addChildWindow(transientWindow, ordered: .above)
        tracker.configure(transientWindow, autosaveName: autosaveName)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: transientWindow)

        XCTAssertNil(UserDefaults.standard.string(forKey: defaultsKey))
    }

    @MainActor
    func testTrackerNeverSwitchesAwayFromALiveViewerWindow() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let tracker = WindowPersistence.Tracker()
        let viewerWindow = makeWindow()
        let secondWindow = makeWindow()
        let viewerFrame = NSRect(x: 155, y: 125, width: 810, height: 590)
        let secondFrame = NSRect(x: 35, y: 45, width: 510, height: 390)

        tracker.configure(viewerWindow, autosaveName: autosaveName)
        tracker.configure(secondWindow, autosaveName: autosaveName)
        secondWindow.setFrame(secondFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: secondWindow)
        viewerWindow.setFrame(viewerFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: viewerWindow)

        assertStoredFrame(autosaveName: autosaveName, equals: viewerFrame)
    }

    @MainActor
    func testTrackerAllowsReplacementAfterTheViewerWindowDeallocates() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let tracker = WindowPersistence.Tracker()
        weak var weakViewerWindow: NSWindow?
        autoreleasepool {
            let viewerWindow = makeWindow()
            weakViewerWindow = viewerWindow
            tracker.configure(viewerWindow, autosaveName: autosaveName)
        }
        XCTAssertNil(weakViewerWindow)

        let replacementWindow = makeWindow()
        let replacementFrame = NSRect(x: 205, y: 165, width: 880, height: 640)
        tracker.configure(replacementWindow, autosaveName: autosaveName)
        replacementWindow.setFrame(replacementFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: replacementWindow)

        assertStoredFrame(autosaveName: autosaveName, equals: replacementFrame)
    }

    @MainActor
    func testTrackerPreservesNormalFrameDuringFullScreenAndResumesAfterExit() {
        let autosaveName = "GlassPhotoViewer.WindowPersistenceTests.\(UUID().uuidString)"
        let defaultsKey = "NSWindow Frame \(autosaveName)"
        defer { UserDefaults.standard.removeObject(forKey: defaultsKey) }

        let tracker = WindowPersistence.Tracker()
        let window = makeWindow()
        let normalFrame = NSRect(x: 145, y: 115, width: 820, height: 600)
        let transitionFrame = NSRect(x: 0, y: 0, width: 1280, height: 690)
        let fullScreenFrame = NSRect(x: 0, y: 0, width: 1280, height: 720)
        let frameAfterExit = NSRect(x: 225, y: 175, width: 900, height: 650)

        tracker.configure(window, autosaveName: autosaveName)
        window.setFrame(normalFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: window)

        NotificationCenter.default.post(name: NSWindow.willEnterFullScreenNotification, object: window)
        window.setFrame(transitionFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didMoveNotification, object: window)
        NotificationCenter.default.post(name: NSWindow.didEnterFullScreenNotification, object: window)
        window.setFrame(fullScreenFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: window)

        assertStoredFrame(autosaveName: autosaveName, equals: normalFrame)

        NotificationCenter.default.post(name: NSWindow.willExitFullScreenNotification, object: window)
        window.setFrame(normalFrame, display: false)
        NotificationCenter.default.post(name: NSWindow.didResizeNotification, object: window)
        NotificationCenter.default.post(name: NSWindow.didExitFullScreenNotification, object: window)
        window.setFrame(frameAfterExit, display: false)
        NotificationCenter.default.post(name: NSWindow.didMoveNotification, object: window)

        assertStoredFrame(autosaveName: autosaveName, equals: frameAfterExit)
    }

    @MainActor
    private func assertStoredFrame(autosaveName: String, equals expectedFrame: NSRect) {
        let restoredWindow = makeWindow()
        XCTAssertTrue(WindowPersistence.configure(restoredWindow, autosaveName: autosaveName))
        XCTAssertEqual(restoredWindow.frame.origin.x, expectedFrame.origin.x, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.origin.y, expectedFrame.origin.y, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.width, expectedFrame.size.width, accuracy: 1)
        XCTAssertEqual(restoredWindow.frame.size.height, expectedFrame.size.height, accuracy: 1)
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
