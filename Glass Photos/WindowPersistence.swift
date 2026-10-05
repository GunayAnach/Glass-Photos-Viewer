import AppKit

enum WindowPersistence {
    static let autosaveName = "GlassPhotos.MainWindow"
    static let defaultContentSize = NSSize(width: 1000, height: 700)

    @discardableResult
    static func configure(
        _ window: NSWindow,
        autosaveName: String = autosaveName
    ) -> Bool {
        let restored = window.setFrameUsingName(autosaveName)
        if !restored {
            window.setContentSize(defaultContentSize)
        }
        window.setFrameAutosaveName(autosaveName)
        return restored
    }
}
