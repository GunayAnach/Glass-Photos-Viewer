import AppKit
import ImageIO

/// Loads and fully decodes images away from the main thread, coalesces duplicate
/// requests, and keeps decoded pixels in a memory-bounded cache.
final class ImagePipeline: @unchecked Sendable {
    typealias Decoder = (URL) -> NSImage?
    typealias Completion = (NSImage?) -> Void

    private let cache = NSCache<NSURL, NSImage>()
    private let decodeQueue = DispatchQueue(
        label: "ns.glass-photo-viewer.image-decoder",
        qos: .userInitiated
    )
    private let stateLock = NSLock()
    private var completions: [URL: [Completion]] = [:]
    private let decoder: Decoder

    init(
        cacheByteLimit: Int = 512 * 1_024 * 1_024,
        decoder: @escaping Decoder = ImagePipeline.decodeImage
    ) {
        cache.totalCostLimit = cacheByteLimit
        self.decoder = decoder
    }

    func cachedImage(for url: URL) -> NSImage? {
        cache.object(forKey: url as NSURL)
    }

    func removeCachedImage(for url: URL) {
        cache.removeObject(forKey: url as NSURL)
    }

    func request(_ url: URL, completion: @escaping Completion) {
        if let image = cachedImage(for: url) {
            DispatchQueue.main.async {
                completion(image)
            }
            return
        }

        stateLock.lock()
        if completions[url] != nil {
            completions[url, default: []].append(completion)
            stateLock.unlock()
            return
        }
        completions[url] = [completion]
        stateLock.unlock()

        decodeQueue.async { [self] in
            let image = decoder(url)
            if let image {
                cache.setObject(image, forKey: url as NSURL, cost: Self.decodedCost(of: image))
            }

            stateLock.lock()
            let callbacks = completions.removeValue(forKey: url) ?? []
            stateLock.unlock()

            DispatchQueue.main.async {
                callbacks.forEach { $0(image) }
            }
        }
    }

    private static func decodeImage(at url: URL) -> NSImage? {
        let sourceOptions = [kCGImageSourceShouldCache: false] as CFDictionary
        guard let source = CGImageSourceCreateWithURL(url as CFURL, sourceOptions) else {
            return nil
        }

        let properties = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any]
        let width = properties?[kCGImagePropertyPixelWidth] as? Int ?? 0
        let height = properties?[kCGImagePropertyPixelHeight] as? Int ?? 0
        let maximumDimension = max(width, height)
        guard maximumDimension > 0 else { return nil }

        let decodeOptions = [
            kCGImageSourceCreateThumbnailFromImageAlways: true,
            kCGImageSourceCreateThumbnailWithTransform: true,
            kCGImageSourceThumbnailMaxPixelSize: maximumDimension,
            kCGImageSourceShouldCacheImmediately: true
        ] as CFDictionary
        guard let cgImage = CGImageSourceCreateThumbnailAtIndex(source, 0, decodeOptions) else {
            return nil
        }

        return NSImage(cgImage: cgImage, size: .zero)
    }

    private static func decodedCost(of image: NSImage) -> Int {
        guard let representation = image.representations.first else { return 0 }
        return max(representation.pixelsWide, 1) * max(representation.pixelsHigh, 1) * 4
    }
}
