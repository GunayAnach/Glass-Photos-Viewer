import Foundation

enum FileOperationsError: LocalizedError {
    case emptyName
    case invalidName
    case destinationExists

    var errorDescription: String? {
        switch self {
        case .emptyName:
            return "The file name cannot be empty."
        case .invalidName:
            return "The file name cannot contain a slash or colon."
        case .destinationExists:
            return "A file with that name already exists."
        }
    }
}

enum FileOperations {
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

    static func moveToTrash(_ url: URL) throws {
        try FileManager.default.trashItem(at: url, resultingItemURL: nil)
    }
}
