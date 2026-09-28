import AppKit
import XCTest
@testable import GlassPhotosCore

final class ImagePipelineTests: XCTestCase {
    func testRequestDecodesOffMainThreadAndCachesResult() throws {
        let decoded = try XCTUnwrap(makeImage())
        let decodeStarted = expectation(description: "decode started")
        let completed = expectation(description: "completion")
        let url = URL(fileURLWithPath: "/tmp/photo.jpg")
        var decodedOnMainThread = true

        let pipeline = ImagePipeline(cacheByteLimit: 16 * 1_024 * 1_024) { requestedURL in
            XCTAssertEqual(requestedURL, url)
            decodedOnMainThread = Thread.isMainThread
            decodeStarted.fulfill()
            return decoded
        }

        pipeline.request(url) { image in
            XCTAssertTrue(Thread.isMainThread)
            XCTAssertNotNil(image)
            completed.fulfill()
        }

        wait(for: [decodeStarted, completed], timeout: 2)
        XCTAssertFalse(decodedOnMainThread)
        XCTAssertNotNil(pipeline.cachedImage(for: url))
    }

    func testConcurrentRequestsForSameURLDecodeOnlyOnce() throws {
        let decoded = try XCTUnwrap(makeImage())
        let bothCompleted = expectation(description: "both completions")
        bothCompleted.expectedFulfillmentCount = 2
        let decodeLock = NSLock()
        var decodeCount = 0
        let url = URL(fileURLWithPath: "/tmp/photo.jpg")

        let pipeline = ImagePipeline { _ in
            decodeLock.lock()
            decodeCount += 1
            decodeLock.unlock()
            Thread.sleep(forTimeInterval: 0.05)
            return decoded
        }

        pipeline.request(url) { _ in bothCompleted.fulfill() }
        pipeline.request(url) { _ in bothCompleted.fulfill() }

        wait(for: [bothCompleted], timeout: 2)
        decodeLock.lock()
        let finalCount = decodeCount
        decodeLock.unlock()
        XCTAssertEqual(finalCount, 1)
    }

    func testCachedRequestCompletesWithoutAnotherDecode() throws {
        let decoded = try XCTUnwrap(makeImage())
        let first = expectation(description: "first request")
        let second = expectation(description: "cached request")
        let countLock = NSLock()
        var decodeCount = 0
        let url = URL(fileURLWithPath: "/tmp/photo.jpg")

        let pipeline = ImagePipeline { _ in
            countLock.lock()
            decodeCount += 1
            countLock.unlock()
            return decoded
        }

        pipeline.request(url) { _ in first.fulfill() }
        wait(for: [first], timeout: 2)
        pipeline.request(url) { _ in second.fulfill() }
        wait(for: [second], timeout: 2)

        countLock.lock()
        let finalCount = decodeCount
        countLock.unlock()
        XCTAssertEqual(finalCount, 1)
    }

    func testRemovingCachedImageForcesFreshDecode() throws {
        let decoded = try XCTUnwrap(makeImage())
        let first = expectation(description: "first request")
        let second = expectation(description: "fresh request")
        let lock = NSLock()
        var decodeCount = 0
        let url = URL(fileURLWithPath: "/tmp/photo.jpg")
        let pipeline = ImagePipeline { _ in
            lock.lock()
            decodeCount += 1
            lock.unlock()
            return decoded
        }

        pipeline.request(url) { _ in first.fulfill() }
        wait(for: [first], timeout: 2)
        pipeline.removeCachedImage(for: url)
        pipeline.request(url) { _ in second.fulfill() }
        wait(for: [second], timeout: 2)

        lock.lock()
        let finalCount = decodeCount
        lock.unlock()
        XCTAssertEqual(finalCount, 2)
    }

    private func makeImage() -> NSImage? {
        let image = NSImage(size: NSSize(width: 8, height: 8))
        image.lockFocus()
        NSColor.red.setFill()
        NSRect(x: 0, y: 0, width: 8, height: 8).fill()
        image.unlockFocus()
        return image
    }
}
