# Changelog

All notable changes to **Glass Photos** are recorded here. macOS and Windows now share one release version.

## [1.2.0] - In development

### Added
- Native Windows 10/11 x64 application built with WinUI 3.
- Matching SwiftUI and WinUI interfaces, including the welcome screen, header controls, image counter, inline rename, metadata sidebar, share, fullscreen, and deletion controls.
- Interactive crop mode on macOS and Windows:
  - Drag the crop area to reposition it.
  - Drag any corner handle to resize it.
  - Press the green tick or `Enter`/`Return` to crop and overwrite the current image.
  - Press `Esc` to cancel.
- Complete Windows keyboard controls matching the macOS application.
- Simple current-user Windows installer with Open with and Default Apps registration for every supported image format.
- Portable self-contained Windows ZIP distribution for no-install use.
- Windows Share panel and Recycle Bin integration.
- Repository link in the macOS Help menu and project documentation.

### Changed
- macOS and Windows application versions are aligned at `1.2.0`.
- The project and repository documentation now describe both supported platforms.
- The Windows interface now follows the macOS visual hierarchy and interaction model.

### Fixed
- Renaming a file to its existing name on Windows is now treated as a successful no-op instead of reporting that the file already exists.
- Windows portable builds now include the compiled WinUI PRI resources required at startup.

## [1.1.7] - 2026

### Fixed
- Corrected the macOS application bundle signature and release packaging.
- Added Gatekeeper launch and quarantine-removal guidance.

## [1.1.1–1.1.6] - 2026

- Incremental macOS releases covering photo navigation, asynchronous image loading and caching, rename, persistent rotation, Trash deletion, metadata, keyboard controls, file opening, and interface refinements.

[1.2.0]: https://github.com/GunayAnach/Glass-Photos-Viewer/compare/v1.1.7...HEAD
[1.1.7]: https://github.com/GunayAnach/Glass-Photos-Viewer/releases/tag/v1.1.7
