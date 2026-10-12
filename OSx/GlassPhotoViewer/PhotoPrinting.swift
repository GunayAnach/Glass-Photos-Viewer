import Foundation

public enum PhotoPrintLayout {
    public enum Orientation: Equatable {
        case portrait
        case landscape
    }

    public static func orientation(width: Double, height: Double) -> Orientation {
        width > height ? .landscape : .portrait
    }
}
