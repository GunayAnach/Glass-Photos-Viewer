import Foundation
import CoreImage
import ImageIO

enum FileOperationsError: LocalizedError {
    case emptyName
    case invalidName
    case destinationExists
    case unreadableImage
    case unsupportedImageFormat
    case imageWriteFailed

    var errorDescription: String? {
        switch self {
        case .emptyName:
            return "The file name cannot be empty."
        case .invalidName:
            return "The file name cannot contain a slash or colon."
        case .destinationExists:
            return "A file with that name already exists."
        case .unreadableImage:
            return "The image could not be read."
        case .unsupportedImageFormat:
            return "This image format cannot be rotated and saved."
        case .imageWriteFailed:
            return "The rotated image could not be saved."
        }
    }
}

enum FileOperations {
    enum RotationDirection {
        case clockwise
        case counterClockwise
    }

    static func basicInfo(for url: URL) -> [(String, String)] {
        [
            ("File Name", url.lastPathComponent),
            ("File Path", url.path)
        ]
    }

    static func destinationURL(for url: URL, baseName: String) throws -> URL {
        let trimmedName = baseName.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedName.isEmpty else { throw FileOperationsError.emptyName }
        guard !trimmedName.contains("/"), !trimmedName.contains(":") else {
            throw FileOperationsError.invalidName
        }

        let ext = url.pathExtension
        let filename = ext.isEmpty ? trimmedName : "\(trimmedName).\(ext)"
        return url.deletingLastPathComponent().appendingPathComponent(filename)
    }

    static func rename(_ url: URL, toBaseName baseName: String) throws -> URL {
        let destination = try destinationURL(for: url, baseName: baseName)
        if destination.standardizedFileURL == url.standardizedFileURL {
            return url
        }
        guard !FileManager.default.fileExists(atPath: destination.path) else {
            throw FileOperationsError.destinationExists
        }
        try FileManager.default.moveItem(at: url, to: destination)
        return destination
    }

    static func rotate(_ url: URL, direction: RotationDirection) throws {
        guard let source = CGImageSourceCreateWithURL(url as CFURL, nil),
              let sourceType = CGImageSourceGetType(source),
              let inputImage = CIImage(
                contentsOf: url,
                options: [.applyOrientationProperty: true]
              ) else {
            throw FileOperationsError.unreadableImage
        }
        let writableTypes = CGImageDestinationCopyTypeIdentifiers() as? [String] ?? []
        guard writableTypes.contains(sourceType as String) else {
            throw FileOperationsError.unsupportedImageFormat
        }

        let orientation: CGImagePropertyOrientation = direction == .clockwise ? .right : .left
        let rotatedImage = inputImage.oriented(orientation)
        let context = CIContext(options: [.cacheIntermediates: false])
        guard let outputImage = context.createCGImage(rotatedImage, from: rotatedImage.extent) else {
            throw FileOperationsError.imageWriteFailed
        }

        let temporaryURL = url.deletingLastPathComponent().appendingPathComponent(
            ".glass-photos-\(UUID().uuidString).\(url.pathExtension)"
        )
        defer { try? FileManager.default.removeItem(at: temporaryURL) }

        guard let destination = CGImageDestinationCreateWithURL(
            temporaryURL as CFURL,
            sourceType,
            1,
            nil
        ) else {
            throw FileOperationsError.unsupportedImageFormat
        }

        var properties = (CGImageSourceCopyPropertiesAtIndex(source, 0, nil)
            as? [CFString: Any]) ?? [:]
        properties[kCGImagePropertyOrientation] = 1
        properties[kCGImageDestinationLossyCompressionQuality] = 1.0
        CGImageDestinationAddImage(destination, outputImage, properties as CFDictionary)
        guard CGImageDestinationFinalize(destination) else {
            throw FileOperationsError.imageWriteFailed
        }

        _ = try FileManager.default.replaceItemAt(url, withItemAt: temporaryURL)
    }

    static func moveToTrash(_ url: URL) throws {
        try FileManager.default.trashItem(at: url, resultingItemURL: nil)
    }
}
