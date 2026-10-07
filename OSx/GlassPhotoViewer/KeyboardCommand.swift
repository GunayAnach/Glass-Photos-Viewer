enum KeyboardCommand: Equatable {
    case passThrough
    case previous
    case next
    case rotateClockwise
    case rotateCounterClockwise
    case toggleFit
    case beginRename
    case beginCrop
    case toggleInfo
    case toggleFullScreen
    case delete
    case exitApplication
    case unhandled

    static func resolve(
        keyCode: UInt16,
        charactersIgnoringModifiers: String? = nil,
        hasDisallowedModifiers: Bool = false,
        isRenaming: Bool,
        isDeleteConfirmationVisible: Bool
    ) -> KeyboardCommand {
        if keyCode == 53 && !hasDisallowedModifiers { return .exitApplication }
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
        default:
            switch charactersIgnoringModifiers?.lowercased() {
            case "c": return .beginCrop
            case "i": return .toggleInfo
            case "f": return .toggleFullScreen
            default: return .unhandled
            }
        }
    }
}
