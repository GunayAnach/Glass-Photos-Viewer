// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "GlassPhotoViewerCore",
    platforms: [.macOS("15.5")],
    products: [
        .library(name: "GlassPhotoViewerCore", targets: ["GlassPhotoViewerCore"])
    ],
    targets: [
        .target(
            name: "GlassPhotoViewerCore",
            path: "GlassPhotoViewer",
            exclude: [
                "Assets.xcassets",
                "Info.plist",
                "GlassPhotoViewer.entitlements",
                "GlassPhotoViewerApp.swift"
            ],
            sources: ["FileOperations.swift", "ImagePipeline.swift", "KeyboardCommand.swift", "WindowPersistence.swift"]
        ),
        .testTarget(
            name: "GlassPhotoViewerCoreTests",
            dependencies: ["GlassPhotoViewerCore"]
        )
    ]
)
