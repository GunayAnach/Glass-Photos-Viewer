# Glass Photo Viewer for Windows

This directory contains the native Windows 10/11 implementation of [Glass Photo Viewer](https://github.com/GunayAnach/Glass-Photos-Viewer), built with C#, .NET, and WinUI 3.

**Current release: 1.2.0** — aligned with the macOS application and the project [changelog](../CHANGELOG.md).

## Layout

```text
GlassPhotoViewer.Core/        Platform-neutral navigation, caching, formatting, and placement logic
GlassPhotoViewer.Core.Tests/  Host-runnable xUnit tests
GlassPhotoViewer.WinUI/       Native WinUI 3 application
installer/                    Inno Setup installer definition
publish-windows.ps1           Portable publish and ZIP script
build-installer.ps1           Installer build script
```

## Implemented features

- Swift-style interface matching the macOS application
- Folder opening and command-line image opening
- Supported neighboring-photo discovery and natural filename ordering
- Complete cross-platform keyboard shortcut set
- Asynchronous bounded image cache with neighboring-photo prefetch
- Persistent rename while preserving the extension
- Same-name rename treated as a successful no-op
- Persistent image rotation
- Interactive movable and resizable crop region with tick/Enter confirmation
- Image metadata sidebar matching the macOS fields, including EXIF, TIFF, and GPS details when available
- Remembered window size and position with safe on-screen restoration
- Native Windows Share panel
- Confirmed deletion to the Windows Recycle Bin
- Fullscreen and fit-to-window controls
- Current-user installer with Open with and Windows Default Apps registration
- Self-contained portable ZIP for no-install use

## Installer

The Inno Setup installer installs Glass Photo Viewer under the current user's local applications folder, creates Start-menu integration, and registers every supported image extension. Re-running the installer automatically closes the running viewer when necessary and overwrites existing private application files, including read-only copies, without an overwrite prompt. Windows then lists Glass Photo Viewer in **Open with** and **Default apps** without silently overriding the user's existing defaults.

The rename retains the original installer AppId and file-association ProgID. Existing installations therefore upgrade in place, existing default-app choices remain valid, and stale `GlassPhotos.*` binaries and shortcuts are removed during the upgrade.

```powershell
./windows/build-installer.ps1 -Architecture x64
```

Output: `dist/Glass-Photo-Viewer-Windows-x64-Setup.exe`. Inno Setup 6 is required to build it.

## Portable distribution

Users extract the complete ZIP and run `GlassPhotoViewer.exe`. WinUI 3 requires the adjacent runtime, DLL, PRI, and resource files, so the application is distributed as one ZIP rather than one isolated executable.

```powershell
./windows/publish-windows.ps1 -Architecture x64
```

Output: `dist/Glass-Photo-Viewer-Windows-x64.zip`

## Keyboard controls

| Key | Action |
|---|---|
| `←` / `→` | Previous or next photo |
| `↑` / `↓` | Rotate counter-clockwise or clockwise |
| `Space` | Toggle fit-to-window |
| `Enter` / `F2` | Rename; `Enter` confirms crop while cropping |
| `I` | Toggle image information |
| `Backspace` / `Delete` | Move to Recycle Bin |
| `F` / `F11` | Toggle fullscreen |
| `Esc` | Exit fullscreen or cancel crop |
| `Ctrl+O` | Open folder |

## Development

Requirements:

- Windows 10 version 2004 or newer, or Windows 11
- .NET 10 SDK
- Windows SDK 10.0.26100
- Visual Studio with WinUI application-development support when using the IDE

```powershell
dotnet test windows/GlassPhotoViewer.Core.Tests/GlassPhotoViewer.Core.Tests.csproj
dotnet build windows/GlassPhotoViewer.WinUI/GlassPhotoViewer.WinUI.csproj -p:Platform=x64
```

The cross-platform core tests also run on macOS and Linux. The WinUI application must be compiled and exercised on Windows; GitHub Actions provides the Windows build, publish, installer-upgrade, resource-validation, and startup-smoke-test gate after changes are pushed.
