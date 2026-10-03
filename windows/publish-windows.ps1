param(
    [ValidateSet("x64", "arm64")]
    [string]$Architecture = "x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$runtime = "win-$Architecture"
$output = Join-Path $repositoryRoot "dist/Glass-Photos-Windows-$Architecture"
$archive = "$output.zip"

Remove-Item $output -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $archive -Force -ErrorAction SilentlyContinue

dotnet publish (Join-Path $PSScriptRoot "GlassPhotos.WinUI/GlassPhotos.WinUI.csproj") `
    --configuration $Configuration `
    --runtime $runtime `
    --self-contained true `
    --output $output

Compress-Archive -Path (Join-Path $output "*") -DestinationPath $archive -CompressionLevel Optimal
Write-Host "Portable build created: $archive"
Write-Host "Extract the ZIP and run GlassPhotos.WinUI.exe; no installer is required."
