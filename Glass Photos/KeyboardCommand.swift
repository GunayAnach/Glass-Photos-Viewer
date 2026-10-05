enum KeyboardCommand: Equatable {
    case passThrough
    case previous
    case next
    case rotateClockwise
    case rotateCounterClockwise
    case toggleFit
    case beginRename
    case toggleInfo
    case toggleFullScreen
    case delete
    case escape
    case unhandled

    static func resolve(
        keyCode: UInt16,
        charactersIgnoringModifiers: String? = nil,
        hasDisallowedModifiers: Bool = false,
        isRenaming: Bool,
        isDeleteConfirmationVisible: Bool
    ) -> KeyboardCommand {
        if isDeleteConfirmationVisible || isRenaming || hasDisallowedModifiers { return .passThrough }

        switch keyCode {
        case 123: return .previous
        case 124: return .next
        case 126: return .rotateCounterClockwise
        case 125: return .rotateClockwise
        case 49: return .toggleFit
        case 36, 76, 120: return .beginRename
        case 103: return .toggleFullScreen
        case 51, 117: return .delete
        case 53: return .escape
        default:
            switch charactersIgnoringModifiers?.lowercased() {
            case "i": return .toggleInfo
            case "f": return .toggleFullScreen
            default: return .unhandled
            }
        }
    }
}
