# Glass Photos for Windows

This directory contains the native Windows port built with C#, .NET, and WinUI 3.
The existing SwiftUI macOS app remains unchanged.

## Current milestone

The first vertical slice is implemented:

- Native WinUI 3 window and toolbar
- Open a supported image from the Windows file picker
- Open an image path passed on the command line (the basis for Explorer **Open with** support)
- Load supported neighboring photos from the same directory
- Natural filename ordering (`photo2.jpg` before `photo10.jpg`)
- Previous/next toolbar controls and Left/Right keyboard navigation
- Persistent filesystem rename while preserving the image extension
- Confirmed deletion to the Windows Recycle Bin (never permanent deletion)
- Self-contained, unpackaged publishing with no installer

Persistent rotation, image metadata, eager decode/cache, and Explorer file associations
are tracked as subsequent milestones.

## Portable distribution

The app is published as a self-contained **portable folder** and ZIP. Users extract
it and run `GlassPhotos.WinUI.exe`; there is no setup program or MSIX installation.

WinUI 3 depends on native Windows App SDK files, so a reliable release is not literally
one standalone file. The EXE and its adjacent runtime files must stay together. The ZIP
is the single download users receive.

On Windows with PowerShell 7 or Windows PowerShell:

```powershell
./windows/publish-windows.ps1 -Architecture x64
```

The output is `dist/Glass-Photos-Windows-x64.zip`.

## Development

Requirements:

- Windows 10 version 2004 or newer
- Visual Studio 2026 with the WinUI application-development workload, or .NET 10 SDK
- Windows SDK 10.0.26100

Build and test:

```powershell
dotnet test windows/GlassPhotos.Core.Tests/GlassPhotos.Core.Tests.csproj
dotnet build windows/GlassPhotos.WinUI/GlassPhotos.WinUI.csproj -p:Platform=x64
```

The cross-platform core tests can also run on macOS and Linux. The WinUI application
itself must be compiled and exercised on Windows; GitHub Actions provides that build gate.
