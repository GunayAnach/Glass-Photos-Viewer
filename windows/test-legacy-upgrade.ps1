param(
    [ValidateSet("x64")]
    [string]$Architecture = "x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$publishOutput = Join-Path $repositoryRoot "dist/Glass-Photo-Viewer-Windows-$Architecture"
$currentInstaller = Join-Path $repositoryRoot "dist/Glass-Photo-Viewer-Windows-$Architecture-Setup.exe"
$legacySource = Join-Path $repositoryRoot "dist/Glass-Photos-Windows-$Architecture-Legacy-Fixture"
$legacyScript = Join-Path $PSScriptRoot "installer/GlassPhotos.LegacyUpgradeFixture.iss"
$legacyInstaller = Join-Path $repositoryRoot "dist/Glass-Photos-Windows-x64-Legacy-Setup.exe"
$legacyProcessProject = Join-Path ([System.IO.Path]::GetTempPath()) "GlassPhotoViewer-LegacyProcessFixture-$PID"
$legacyInstallDirectory = Join-Path $env:LOCALAPPDATA "Programs/Glass Photos"
$currentPlacementDirectory = Join-Path $env:LOCALAPPDATA "Glass Photo Viewer"
$legacyPlacementDirectory = Join-Path $env:LOCALAPPDATA "Glass Photos"
$legacyPlacementPath = Join-Path $legacyPlacementDirectory "window-placement.json"
$currentPlacementPath = Join-Path $currentPlacementDirectory "window-placement.json"

function Invoke-Installer([string]$Path) {
    $process = Start-Process $Path `
        -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/NORESTARTAPPLICATIONS", "/SP-" `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Installer $Path exited with code $($process.ExitCode)."
    }
}

function Wait-ForMainWindow([System.Diagnostics.Process]$Process) {
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        Start-Sleep -Milliseconds 500
        $Process.Refresh()
        if ($Process.HasExited) {
            throw "Application exited during startup with code $($Process.ExitCode)."
        }
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            return
        }
    }
    throw "Application did not create a main window."
}

$compiler = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($compiler) {
    $compilerPath = $compiler.Source
}
else {
    $compilerPath = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6/ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6/ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs/Inno Setup 6/ISCC.exe")
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}
if (-not $compilerPath) {
    throw "Inno Setup 6 was not found."
}
if (-not (Test-Path $publishOutput)) {
    throw "Portable publish output is missing: $publishOutput"
}
if (-not (Test-Path $currentInstaller)) {
    throw "Current installer is missing: $currentInstaller"
}

$legacyProcess = $null
$upgradedProcess = $null
try {
    Remove-Item $legacySource -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyInstaller -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyProcessProject -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyInstallDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $currentPlacementDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyPlacementDirectory -Recurse -Force -ErrorAction SilentlyContinue

    New-Item -ItemType Directory -Path $legacySource -Force | Out-Null
    Copy-Item (Join-Path $publishOutput "*") $legacySource -Recurse -Force

    $currentExe = Join-Path $legacySource "GlassPhotoViewer.exe"
    $legacyExeSource = Join-Path $legacySource "GlassPhotos.WinUI.exe"
    $legacyProcessOutput = Join-Path $legacyProcessProject "publish"
    New-Item -ItemType Directory -Path $legacyProcessProject -Force | Out-Null
    Set-Content -Path (Join-Path $legacyProcessProject "LegacyProcessFixture.csproj") -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>GlassPhotos.WinUI</AssemblyName>
    <UseAppHost>true</UseAppHost>
  </PropertyGroup>
</Project>
'@
    Set-Content -Path (Join-Path $legacyProcessProject "Program.cs") -Value @'
using System.Threading;

internal static class Program
{
    public static void Main() => Thread.Sleep(Timeout.Infinite);
}
'@
    dotnet publish $legacyProcessProject `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:DebugType=None `
        --output $legacyProcessOutput
    $builtLegacyExe = Join-Path $legacyProcessOutput "GlassPhotos.WinUI.exe"
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $builtLegacyExe)) {
        throw "Could not build the legacy running-process fixture."
    }
    Copy-Item $builtLegacyExe $legacyExeSource -Force
    $assets = Join-Path $legacySource "Assets"
    Copy-Item (Join-Path $assets "GlassPhotoViewer.ico") (Join-Path $assets "GlassPhotos.ico") -Force
    Copy-Item (Join-Path $assets "GlassPhotoViewer.png") (Join-Path $assets "GlassPhotos.png") -Force
    Remove-Item $currentExe -Force

    & $compilerPath `
        "/DSourceDir=$legacySource" `
        "/DOutputDir=$(Split-Path -Parent $legacyInstaller)" `
        $legacyScript
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $legacyInstaller)) {
        throw "Could not build the legacy installer fixture."
    }

    Invoke-Installer $legacyInstaller
    $installedLegacyExe = Join-Path $legacyInstallDirectory "GlassPhotos.WinUI.exe"
    if (-not (Test-Path $installedLegacyExe)) {
        throw "Legacy fixture did not install GlassPhotos.WinUI.exe."
    }

    New-Item -ItemType Directory -Path $legacyPlacementDirectory -Force | Out-Null
    Set-Content -Path $legacyPlacementPath -Value '{"X":120,"Y":90,"Width":640,"Height":480}' -NoNewline

    $legacyProcess = Start-Process $installedLegacyExe -PassThru
    Start-Sleep -Seconds 3
    $legacyProcess.Refresh()
    if ($legacyProcess.HasExited) {
        throw "Legacy running-process fixture exited before the upgrade test."
    }

    Invoke-Installer $currentInstaller
    $legacyProcess.Refresh()
    if (-not $legacyProcess.HasExited) {
        throw "Renamed installer did not close GlassPhotos.WinUI.exe during upgrade."
    }

    $installedCurrentExe = Join-Path $legacyInstallDirectory "GlassPhotoViewer.exe"
    if (-not (Test-Path $installedCurrentExe)) {
        throw "Renamed installer did not preserve the existing install location."
    }
    if (Test-Path $installedLegacyExe) {
        throw "Renamed installer left the legacy executable behind."
    }
    if (Test-Path (Join-Path $legacyInstallDirectory "Assets/GlassPhotos.png")) {
        throw "Renamed installer left the legacy asset behind."
    }

    $registeredApplications = Get-Item "Registry::HKEY_CURRENT_USER\Software\RegisteredApplications"
    if ($null -ne $registeredApplications.GetValue("Glass Photos", $null)) {
        throw "Renamed installer left the legacy RegisteredApplications value behind."
    }
    if ($registeredApplications.GetValue("Glass Photo Viewer", $null) -ne "Software\GlassPhotos\Capabilities") {
        throw "Renamed installer did not register the new product name."
    }
    $openCommand = (Get-Item "Registry::HKEY_CURRENT_USER\Software\Classes\GlassPhotos.Image\shell\open\command").GetValue("")
    if ($openCommand -notlike "*GlassPhotoViewer.exe*%1*") {
        throw "Renamed installer did not update the stable ProgID command: $openCommand"
    }

    # Ensure the renamed app itself must read the legacy placement fallback.
    Remove-Item $currentPlacementDirectory -Recurse -Force -ErrorAction SilentlyContinue
    $upgradedProcess = Start-Process $installedCurrentExe -PassThru
    Wait-ForMainWindow $upgradedProcess

    Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    public static class LegacyUpgradeWindowPlacement {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    }
"@
    $restored = New-Object LegacyUpgradeWindowPlacement+RECT
    if (-not [LegacyUpgradeWindowPlacement]::GetWindowRect($upgradedProcess.MainWindowHandle, [ref]$restored)) {
        throw "Could not read the upgraded app window placement."
    }
    $width = $restored.Right - $restored.Left
    $height = $restored.Bottom - $restored.Top
    if ([Math]::Abs($restored.Left - 120) -gt 16 -or
        [Math]::Abs($restored.Top - 90) -gt 16 -or
        [Math]::Abs($width - 640) -gt 16 -or
        [Math]::Abs($height - 480) -gt 16) {
        throw "Legacy placement was not restored: $($restored.Left),$($restored.Top) ${width}x${height}."
    }

    $upgradedProcess.CloseMainWindow() | Out-Null
    if (-not $upgradedProcess.WaitForExit(10000)) {
        throw "Upgraded application did not close cleanly."
    }
    if (-not (Test-Path $currentPlacementPath)) {
        throw "Upgraded application did not migrate placement into the renamed settings directory."
    }

    Write-Host "Legacy Glass Photos to Glass Photo Viewer upgrade passed."
}
finally {
    foreach ($process in @($legacyProcess, $upgradedProcess)) {
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }

    $uninstaller = Get-ChildItem $legacyInstallDirectory -Filter "unins*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($uninstaller) {
        Start-Process $uninstaller.FullName -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART" -Wait | Out-Null
    }
    Remove-Item $legacyInstallDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacySource -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyInstaller -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyProcessProject -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $currentPlacementDirectory -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $legacyPlacementDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
