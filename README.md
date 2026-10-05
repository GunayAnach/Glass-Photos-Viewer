# <img src="OSx/GlassPhotoViewer/Assets.xcassets/AppIcon.appiconset/icon_128x128.png" alt="Glass Photo Viewer logo" width="32" height="32" style="vertical-align: middle; margin-right: 8px;"> Glass Photo Viewer

A fast, native photo viewer for **macOS and Windows** with a consistent interface and workflow across both platforms.

- **Current development version:** `1.2.0`
- **Repository:** [github.com/GunayAnach/Glass-Photos-Viewer](https://github.com/GunayAnach/Glass-Photos-Viewer)
- **Downloads:** [GitHub Releases](https://github.com/GunayAnach/Glass-Photos-Viewer/releases)
- **Version history:** [CHANGELOG.md](CHANGELOG.md)

## Platforms

| Platform | Technology | Distribution |
|---|---|---|
| macOS | SwiftUI and AppKit | `Glass Photo Viewer.app` in a ZIP |
| Windows 10/11 x64 | C#, .NET, and WinUI 3 | Current-user installer with file associations, plus portable ZIP |

The two applications use the same dark image canvas, welcome screen, top controls, inline rename flow, image counter, metadata sidebar, cropping workflow, and keyboard controls while retaining native platform integration.

## Repository layout

```text
OSx/                               Native macOS application
  GlassPhotoViewer/                SwiftUI/AppKit source and assets
  GlassPhotoViewer.xcodeproj/      Xcode project
  Tests/GlassPhotoViewerCoreTests/ Swift package tests
  Package.swift                    Host-runnable macOS core test package
windows/                           Native Windows application
  GlassPhotoViewer.Core/           Platform-neutral Windows logic
  GlassPhotoViewer.Core.Tests/     xUnit test suite
  GlassPhotoViewer.WinUI/          WinUI 3 application
  installer/                       Inno Setup installer
assets/                            Shared documentation screenshots
```

Shared documentation, changelog, licensing, security policy, and CI configuration remain at the repository root.

## Screenshots

![Interface](assets/1.jpg)
![Opened photo](assets/4.jpg)
![Renaming photos with Enter](assets/5.jpg)
![Navigate with the arrow keys](assets/6.jpg)

## Features

- Open a folder or an associated image file and browse neighboring photos.
- Natural filename ordering, so `photo2.jpg` appears before `photo10.jpg`.
- Asynchronous image loading, bounded caching, and neighboring-photo prefetch.
- Persistent rename while preserving the file extension.
- Persistent clockwise and counter-clockwise rotation.
- Interactive cropping with a movable and resizable crop border.
- Metadata sidebar with filename, path, size, dimensions, dates, camera details, and GPS data when available.
- Remembered window size and position across launches, with safe on-screen restoration.
- Native sharing support.
- Fullscreen viewing and fit-to-window/actual-size modes.
- Safe deletion to macOS Trash or the Windows Recycle Bin after confirmation.
- Native file-opening support from Finder or Windows command-line/Explorer integration.

## Keyboard controls

| Key | Action |
|---|---|
| `←` / `→` | Previous or next photo |
| `↑` | Rotate counter-clockwise and save |
| `↓` | Rotate clockwise and save |
| `Space` | Toggle fit-to-window and actual size |
| `Enter` / `Return` / `F2` | Rename the current photo |
| `I` | Toggle image information |
| `Backspace` / `Delete` | Move the current photo to Trash or Recycle Bin |
| `F` / `F11` | Toggle fullscreen |
| `Esc` | Exit fullscreen or cancel cropping |
| `Cmd+O` on macOS | Open folder |
| `Ctrl+O` on Windows | Open folder |

Keyboard commands remain inactive while a rename field or confirmation dialog is open.

## Cropping

1. Open a photo and press the **Crop** button.
2. Drag inside the crop rectangle to move it.
3. Drag any corner handle to resize it.
4. Press the green **tick** or `Enter`/`Return` to crop and overwrite the current image.
5. Press `Esc` to cancel without changing the file.

Cropping and rotation rewrite the current image, so retain a backup when editing irreplaceable originals.

## Supported formats

- Common formats: JPG, JPEG, PNG, WebP, HEIC, HEIF, TIFF, GIF, BMP
- RAW formats: DNG, NEF, CR2, ARW, RAF

Viewing support depends on the codecs installed by the operating system. Persistent editing is available only where the platform provides a matching image encoder.

## Installation

### macOS

1. Download the macOS ZIP from [Releases](https://github.com/GunayAnach/Glass-Photos-Viewer/releases).
2. Extract it and move **Glass Photo Viewer.app** to `/Applications`.
3. Right-click the app and choose **Open** on first launch.

Glass Photo Viewer is ad-hoc signed but not Apple-notarized. If macOS reports that the official release is damaged, remove its quarantine attribute:

```bash
xattr -dr com.apple.quarantine "/Applications/Glass Photo Viewer.app"
```

Then right-click the app and choose **Open** again.

When upgrading from the former **Glass Photos.app**, remove the old bundle from `/Applications` after installing and opening **Glass Photo Viewer.app**. Both bundles intentionally share the same bundle identifier for continuity, so keeping both can create duplicate Finder **Open with** entries.

To make it the default viewer for a format, select an image in Finder, choose **Get Info**, select **Glass Photo Viewer.app** under **Open with**, and press **Change All…**.

### Windows

1. Download `Glass-Photo-Viewer-Windows-x64-Setup.exe` from [Releases](https://github.com/GunayAnach/Glass-Photos-Viewer/releases).
2. Run the installer. It installs for the current user and does not require administrator access.
3. Choose **Glass Photo Viewer** from **Open with** or Windows **Default apps** for the image formats you want it to open.

The installer registers JPG, JPEG, PNG, HEIC, HEIF, TIF, TIFF, GIF, BMP, WebP, DNG, NEF, CR2, ARW, and RAF associations. Windows protects each user's existing default-app choices, so installation makes Glass Photo Viewer available without silently replacing current defaults. Re-running the installer closes a running copy when necessary and overwrites stale or read-only private application files. A self-contained portable ZIP remains available for users who prefer no installation.

## System requirements

### macOS

- macOS 15.5 or later
- Apple silicon and Intel Macs (universal app)

### Windows

- Windows 10 version 2004 or later, or Windows 11
- x64 processor for the published portable build

## Development

### macOS

Open `OSx/GlassPhotoViewer.xcodeproj` in Xcode, or run the core tests and an Xcode Debug build:

```bash
swift test --package-path OSx
xcodebuild -project OSx/GlassPhotoViewer.xcodeproj \
  -scheme "Glass Photo Viewer" \
  -configuration Debug \
  -derivedDataPath OSx/.build/xcode-derived \
  CODE_SIGNING_ALLOWED=NO build
```

Create and verify an ad-hoc-signed Release ZIP, optionally launching the extracted test copy:

```bash
./OSx/build-release.sh
./OSx/build-release.sh --launch
```

The release wrapper reads version and identity data from the built app and writes `dist/Glass-Photo-Viewer-v<version>-macOS.zip`.

### Windows

Build on Windows with the .NET 10 SDK and Windows SDK 10.0.26100:

```powershell
dotnet test windows/GlassPhotoViewer.Core.Tests/GlassPhotoViewer.Core.Tests.csproj
dotnet build windows/GlassPhotoViewer.WinUI/GlassPhotoViewer.WinUI.csproj -p:Platform=x64
./windows/publish-windows.ps1 -Architecture x64
./windows/build-installer.ps1 -Architecture x64
```

The published artifacts are `dist/Glass-Photo-Viewer-Windows-x64.zip` and `dist/Glass-Photo-Viewer-Windows-x64-Setup.exe`. Building the installer requires Inno Setup 6.

## Bugs and contributions

Report problems or propose improvements through [GitHub Issues](https://github.com/GunayAnach/Glass-Photos-Viewer/issues). Include the operating system version, image format, and reproduction steps.
