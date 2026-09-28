import CoreGraphics
import ImageIO
import UniformTypeIdentifiers
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
