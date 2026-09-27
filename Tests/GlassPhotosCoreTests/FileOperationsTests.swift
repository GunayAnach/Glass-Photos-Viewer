import XCTest
@testable import GlassPhotosCore

final class FileOperationsTests: XCTestCase {
    func testRenamePreservesOriginalExtension() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }

        let original = folder.appendingPathComponent("old-name.jpg")
        try Data("photo".utf8).write(to: original)

        let renamed = try FileOperations.rename(original, toBaseName: "holiday photo")

        XCTAssertEqual(renamed.lastPathComponent, "holiday photo.jpg")
        XCTAssertTrue(FileManager.default.fileExists(atPath: renamed.path))
        XCTAssertFalse(FileManager.default.fileExists(atPath: original.path))
    }

    func testRenameRejectsEmptyNameAndLeavesFileUntouched() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }

        let original = folder.appendingPathComponent("original.png")
        try Data("photo".utf8).write(to: original)

        XCTAssertThrowsError(try FileOperations.rename(original, toBaseName: "   "))
        XCTAssertTrue(FileManager.default.fileExists(atPath: original.path))
    }

    func testRenameRejectsPathSeparators() throws {
        let original = URL(fileURLWithPath: "/tmp/original.png")
        XCTAssertThrowsError(try FileOperations.destinationURL(for: original, baseName: "folder/name"))
    }
}
