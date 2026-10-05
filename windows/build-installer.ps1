param(
    [ValidateSet("x64")]
    [string]$Architecture = "x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$publishOutput = Join-Path $repositoryRoot "dist/Glass-Photos-Windows-$Architecture"
$application = Join-Path $publishOutput "GlassPhotos.WinUI.exe"
$installerScript = Join-Path $PSScriptRoot "installer/GlassPhotos.iss"
$installerOutput = Join-Path $repositoryRoot "dist/Glass-Photos-Windows-$Architecture-Setup.exe"
$projectFile = Join-Path $PSScriptRoot "GlassPhotos.WinUI/GlassPhotos.WinUI.csproj"

if (-not (Test-Path $application)) {
    & (Join-Path $PSScriptRoot "publish-windows.ps1") `
        -Architecture $Architecture `
        -Configuration $Configuration
}

[xml]$project = Get-Content $projectFile
$version = [string]($project.Project.PropertyGroup.Version | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not determine the application version from $projectFile."
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
    throw "Inno Setup 6 was not found. Install it from https://jrsoftware.org/isinfo.php"
}

Remove-Item $installerOutput -Force -ErrorAction SilentlyContinue
& $compilerPath `
    "/DSourceDir=$publishOutput" `
    "/DOutputDir=$(Split-Path -Parent $installerOutput)" `
    "/DAppVersion=$version" `
    $installerScript

if ($LASTEXITCODE -ne 0 -or -not (Test-Path $installerOutput)) {
    throw "Inno Setup did not create the expected installer: $installerOutput"
}

Write-Host "Windows installer created: $installerOutput"
Write-Host "The installer registers Glass Photos in Open with and Windows Default Apps."
