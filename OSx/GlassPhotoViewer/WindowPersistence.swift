import AppKit

enum WindowPersistence {
    // Preserve the original key so existing users keep their saved window frame after the rename.
    static let autosaveName = "GlassPhotos.MainWindow"
    static let defaultContentSize = NSSize(width: 1000, height: 700)

    private static func defaultsKey(for autosaveName: String) -> String {
        "NSWindow Frame \(autosaveName)"
    }

    private static func isValidFrameDescriptor(_ descriptor: String) -> Bool {
        let components = descriptor.split(whereSeparator: \.isWhitespace)
        guard components.count == 8 else { return false }
        let values = components.compactMap { Double($0) }
        guard values.count == components.count,
              values.allSatisfy(\.isFinite),
              values[2] > 0,
              values[3] > 0 else {
            return false
        }
        return true
    }

    @discardableResult
    static func configure(
        _ window: NSWindow,
        autosaveName: String = autosaveName
    ) -> Bool {
        let defaultsKey = defaultsKey(for: autosaveName)
        if let frameDescriptor = UserDefaults.standard.string(forKey: defaultsKey) {
            if isValidFrameDescriptor(frameDescriptor) {
                window.setFrame(from: frameDescriptor)
                return true
            }
            UserDefaults.standard.removeObject(forKey: defaultsKey)
        }
        window.setContentSize(defaultContentSize)
        return false
    }

    static func save(_ window: NSWindow, autosaveName: String = autosaveName) {
        UserDefaults.standard.set(window.frameDescriptor, forKey: defaultsKey(for: autosaveName))
    }

    static func isEligibleViewerWindow(_ window: NSWindow) -> Bool {
        guard !(window is NSPanel),
              window.level == .normal,
              window.styleMask.contains(.titled),
              !window.styleMask.contains(.utilityWindow),
              !window.styleMask.contains(.nonactivatingPanel),
              window.parent == nil,
              window.sheetParent == nil else {
            return false
        }
        return true
    }

    static func preferredWindow(
        keyWindow: NSWindow?,
        mainWindow: NSWindow?,
        windows: [NSWindow]
    ) -> NSWindow? {
        if let keyWindow, isEligibleViewerWindow(keyWindow) {
            return keyWindow
        }
        if let mainWindow, isEligibleViewerWindow(mainWindow) {
            return mainWindow
        }
        return windows.first(where: { $0.isVisible && isEligibleViewerWindow($0) })
            ?? windows.first(where: isEligibleViewerWindow)
    }

    final class Tracker {
        private weak var trackedWindow: NSWindow?
        private var trackedAutosaveName = WindowPersistence.autosaveName
        private var observers: [NSObjectProtocol] = []
        private var isSuspendedForFullScreen = false

        func configure(
            _ window: NSWindow,
            autosaveName: String = WindowPersistence.autosaveName
        ) {
            guard WindowPersistence.isEligibleViewerWindow(window) else { return }
            if let trackedWindow {
                guard trackedWindow === window else { return }
                guard trackedAutosaveName != autosaveName else { return }
            }

            stopTracking()
            trackedWindow = window
            trackedAutosaveName = autosaveName
            isSuspendedForFullScreen = window.styleMask.contains(.fullScreen)
            WindowPersistence.configure(window, autosaveName: autosaveName)

            let center = NotificationCenter.default
            for name in [
                NSWindow.didMoveNotification,
                NSWindow.didResizeNotification
            ] {
                observers.append(center.addObserver(
                    forName: name,
                    object: nil,
                    queue: .main
                ) { [weak self, weak window] notification in
                    guard let self,
                          let window,
                          notification.object as? NSWindow === window,
                          self.trackedWindow === window,
                          !self.isSuspendedForFullScreen,
                          !window.styleMask.contains(.fullScreen) else { return }
                    WindowPersistence.save(window, autosaveName: self.trackedAutosaveName)
                })
            }

            for name in [
                NSWindow.willEnterFullScreenNotification,
                NSWindow.didEnterFullScreenNotification,
                NSWindow.willExitFullScreenNotification
            ] {
                observers.append(center.addObserver(
                    forName: name,
                    object: nil,
                    queue: .main
                ) { [weak self, weak window] notification in
                    guard let self,
                          let window,
                          notification.object as? NSWindow === window,
                          self.trackedWindow === window else { return }
                    self.isSuspendedForFullScreen = true
                })
            }

            observers.append(center.addObserver(
                forName: NSWindow.didExitFullScreenNotification,
                object: nil,
                queue: .main
            ) { [weak self, weak window] notification in
                guard let self,
                      let window,
                      notification.object as? NSWindow === window,
                      self.trackedWindow === window else { return }
                self.isSuspendedForFullScreen = false
            })
        }

        func stopTracking() {
            let center = NotificationCenter.default
            observers.forEach(center.removeObserver)
            observers.removeAll()
            trackedWindow = nil
            isSuspendedForFullScreen = false
        }

        deinit {
            observers.forEach(NotificationCenter.default.removeObserver)
        }
    }

}
