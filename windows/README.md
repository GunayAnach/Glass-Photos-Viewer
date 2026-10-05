# Glass Photos for Windows

This directory contains the native Windows 10/11 port of [Glass Photos](https://github.com/GunayAnach/Glass-Photos-Viewer), built with C#, .NET, and WinUI 3.

**Current development version: 1.2.0** — aligned with the macOS application and the project [changelog](../CHANGELOG.md).

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
- Dynamic image metadata sidebar
- Native Windows Share panel
- Confirmed deletion to the Windows Recycle Bin
- Fullscreen and fit-to-window controls
- Current-user installer with Open with and Windows Default Apps registration
- Self-contained portable ZIP for no-install use

## Installer

The Inno Setup installer installs Glass Photos under the current user's local applications folder, creates Start-menu integration, and registers every supported image extension. Windows will then list Glass Photos in **Open with** and **Default apps** without silently overriding the user's existing defaults.

```powershell
./windows/build-installer.ps1 -Architecture x64
```

Output: `dist/Glass-Photos-Windows-x64-Setup.exe`. Inno Setup 6 is required to build it.

## Portable distribution

Users extract the complete ZIP and run `GlassPhotos.WinUI.exe`. WinUI 3 requires the adjacent runtime, DLL, PRI, and resource files, so the application is distributed as one ZIP rather than one isolated executable.

```powershell
./windows/publish-windows.ps1 -Architecture x64
```

Output: `dist/Glass-Photos-Windows-x64.zip`

## Keyboard controls

| Key | Action |
|---|---|
| `←` / `→` | Previous or next photo |
| `↑` / `↓` | Rotate counter-clockwise or clockwise |
| `Space` | Toggle fit-to-window |
| `Enter` | Rename; confirms crop while cropping |
| `Backspace` / `Delete` | Move to Recycle Bin |
| `F` | Toggle fullscreen |
| `Esc` | Exit fullscreen or cancel crop |
| `Ctrl+O` | Open folder |

## Development

Requirements:

- Windows 10 version 2004 or newer, or Windows 11
- .NET 10 SDK
- Windows SDK 10.0.26100
- Visual Studio with WinUI application-development support when using the IDE

```powershell
dotnet test windows/GlassPhotos.Core.Tests/GlassPhotos.Core.Tests.csproj
dotnet build windows/GlassPhotos.WinUI/GlassPhotos.WinUI.csproj -p:Platform=x64
```

The cross-platform core tests also run on macOS and Linux. The WinUI application must be compiled and exercised on Windows; GitHub Actions provides the Windows build, publish, resource-validation, and startup-smoke-test gate.
