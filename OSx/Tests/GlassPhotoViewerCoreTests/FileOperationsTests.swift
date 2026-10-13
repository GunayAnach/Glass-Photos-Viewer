import CoreGraphics
import ImageIO
import UniformTypeIdentifiers
import XCTest
@testable import GlassPhotoViewerCore

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

    func testRenameToCaseVariantOfSameFileIsSuccessfulOnCaseInsensitiveVolume() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }

        let original = folder.appendingPathComponent("unchanged.jpg")
        try Data("original contents".utf8).write(to: original)
        let caseVariant = folder.appendingPathComponent("UNCHANGED.jpg")
        guard FileManager.default.fileExists(atPath: caseVariant.path) else {
            throw XCTSkip("The temporary volume is case-sensitive; a case-only rename is distinct there.")
        }

        let result = try FileOperations.rename(original, toBaseName: "UNCHANGED")

        XCTAssertEqual(result, original)
        XCTAssertEqual(try Data(contentsOf: original), Data("original contents".utf8))
    }

    func testSubmittingUnchangedNameSucceedsWithoutReplacingThePhoto() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }

        let original = folder.appendingPathComponent("unchanged.jpg")
        let contents = Data("original contents".utf8)
        try contents.write(to: original)

        let result = try FileOperations.rename(original, toBaseName: "unchanged")

        XCTAssertEqual(result, original)
        XCTAssertEqual(try Data(contentsOf: original), contents)
    }

    func testRenameDoesNotOverwriteDifferentExistingDestination() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }

        let original = folder.appendingPathComponent("before.jpg")
        let destination = folder.appendingPathComponent("after.jpg")
        try Data("source".utf8).write(to: original)
        try Data("destination".utf8).write(to: destination)

        XCTAssertThrowsError(try FileOperations.rename(original, toBaseName: "after"))
        XCTAssertEqual(try Data(contentsOf: original), Data("source".utf8))
        XCTAssertEqual(try Data(contentsOf: destination), Data("destination".utf8))
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

    func testBasicInfoContainsFilenameAndFullPath() {
        let url = URL(fileURLWithPath: "/Users/example/Pictures/holiday.jpg")

        let info = FileOperations.basicInfo(for: url)

        XCTAssertEqual(info.map(\.0), ["File Name", "File Path"])
        XCTAssertEqual(info.map(\.1), ["holiday.jpg", "/Users/example/Pictures/holiday.jpg"])
    }

    func testClockwiseRotationPersistsSwappedPixelDimensions() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }
        let url = folder.appendingPathComponent("wide.png")
        try writePNG(width: 4, height: 2, to: url)

        try FileOperations.rotate(url, direction: .clockwise)

        let source = try XCTUnwrap(CGImageSourceCreateWithURL(url as CFURL, nil))
        let properties = try XCTUnwrap(
            CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any]
        )
        XCTAssertEqual(properties[kCGImagePropertyPixelWidth] as? Int, 2)
        XCTAssertEqual(properties[kCGImagePropertyPixelHeight] as? Int, 4)
    }

    func testFirstRotationNormalizesExistingExifOrientationAndRotatesOnlyOneQuarterTurn() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }
        let url = folder.appendingPathComponent("camera-oriented.jpg")
        try writeJPEG(width: 4, height: 2, orientation: 6, to: url)

        try FileOperations.rotate(url, direction: .counterClockwise)

        let source = try XCTUnwrap(CGImageSourceCreateWithURL(url as CFURL, nil))
        let properties = try XCTUnwrap(
            CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any]
        )
        XCTAssertEqual(properties[kCGImagePropertyOrientation] as? Int, 1)
        XCTAssertEqual(properties[kCGImagePropertyPixelWidth] as? Int, 4)
        XCTAssertEqual(properties[kCGImagePropertyPixelHeight] as? Int, 2)
    }

    func testCropPersistsRequestedNormalizedRegion() throws {
        let folder = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: folder) }
        let url = folder.appendingPathComponent("crop.png")
        try writePNG(width: 100, height: 80, to: url)

        try FileOperations.crop(
            url,
            normalizedRect: CGRect(x: 0.1, y: 0.25, width: 0.5, height: 0.5)
        )

        let source = try XCTUnwrap(CGImageSourceCreateWithURL(url as CFURL, nil))
        let properties = try XCTUnwrap(
            CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any]
        )
        XCTAssertEqual(properties[kCGImagePropertyPixelWidth] as? Int, 50)
        XCTAssertEqual(properties[kCGImagePropertyPixelHeight] as? Int, 40)
    }

    private func writePNG(width: Int, height: Int, to url: URL) throws {
        let colorSpace = try XCTUnwrap(CGColorSpace(name: CGColorSpace.sRGB))
        let context = try XCTUnwrap(CGContext(
            data: nil,
            width: width,
            height: height,
            bitsPerComponent: 8,
            bytesPerRow: width * 4,
            space: colorSpace,
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
        ))
        context.setFillColor(CGColor(red: 1, green: 0, blue: 0, alpha: 1))
        context.fill(CGRect(x: 0, y: 0, width: width, height: height))
        let image = try XCTUnwrap(context.makeImage())
        let destination = try XCTUnwrap(
            CGImageDestinationCreateWithURL(url as CFURL, UTType.png.identifier as CFString, 1, nil)
        )
        CGImageDestinationAddImage(destination, image, nil)
        XCTAssertTrue(CGImageDestinationFinalize(destination))
    }

    private func writeJPEG(width: Int, height: Int, orientation: Int, to url: URL) throws {
        let colorSpace = try XCTUnwrap(CGColorSpace(name: CGColorSpace.sRGB))
        let context = try XCTUnwrap(CGContext(
            data: nil,
            width: width,
            height: height,
            bitsPerComponent: 8,
            bytesPerRow: width * 4,
            space: colorSpace,
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
        ))
        context.setFillColor(CGColor(red: 1, green: 0, blue: 0, alpha: 1))
        context.fill(CGRect(x: 0, y: 0, width: width, height: height))
        let image = try XCTUnwrap(context.makeImage())
        let destination = try XCTUnwrap(
            CGImageDestinationCreateWithURL(url as CFURL, UTType.jpeg.identifier as CFString, 1, nil)
        )
        CGImageDestinationAddImage(
            destination,
            image,
            [kCGImagePropertyOrientation: orientation] as CFDictionary
        )
        XCTAssertTrue(CGImageDestinationFinalize(destination))
    }
}
