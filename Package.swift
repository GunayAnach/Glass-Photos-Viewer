// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "GlassPhotosCore",
    platforms: [.macOS("15.0")],
    products: [
        .library(name: "GlassPhotosCore", targets: ["GlassPhotosCore"])
    ],
    targets: [
        .target(
            name: "GlassPhotosCore",
            path: "Glass Photos",
            exclude: [
                "Assets.xcassets",
                "Info.plist",
                "glass_photo_viewer.entitlements",
                "glass_photo_viewerApp.swift"
            ],
            sources: ["FileOperations.swift", "ImagePipeline.swift", "KeyboardCommand.swift", "WindowPersistence.swift"]
        ),
        .testTarget(
            name: "GlassPhotosCoreTests",
            dependencies: ["GlassPhotosCore"]
        )
    ]
)
