#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
REPOSITORY_ROOT=$(cd -- "$SCRIPT_DIR/.." && pwd)
PROJECT="$SCRIPT_DIR/GlassPhotoViewer.xcodeproj"
SCHEME="Glass Photo Viewer"
DERIVED_DATA="$SCRIPT_DIR/.build/xcode-release"
VERIFY_DIR="$SCRIPT_DIR/.build/release-verify"
APP="$DERIVED_DATA/Build/Products/Release/Glass Photo Viewer.app"
DIST_DIR="$REPOSITORY_ROOT/dist"
LAUNCH_AFTER_BUILD=false

if [[ "${1:-}" == "--launch" ]]; then
    LAUNCH_AFTER_BUILD=true
elif [[ $# -gt 0 ]]; then
    printf 'Usage: %s [--launch]\n' "$0" >&2
    exit 64
fi

printf 'Running Swift package tests...\n'
(
    cd "$SCRIPT_DIR"
    swift test
)

printf 'Building Glass Photo Viewer Release...\n'
xcodebuild \
    -quiet \
    -project "$PROJECT" \
    -scheme "$SCHEME" \
    -configuration Release \
    -destination generic/platform=macOS \
    -derivedDataPath "$DERIVED_DATA" \
    ARCHS="arm64 x86_64" \
    ONLY_ACTIVE_ARCH=NO \
    CODE_SIGNING_ALLOWED=NO \
    clean build

test -d "$APP"
EXECUTABLE="$APP/Contents/MacOS/Glass Photo Viewer"
lipo "$EXECUTABLE" -verify_arch arm64
lipo "$EXECUTABLE" -verify_arch x86_64
INFO_PLIST="$APP/Contents/Info.plist"
VERSION=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$INFO_PLIST")
DISPLAY_NAME=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' "$INFO_PLIST")
EXECUTABLE_NAME=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$INFO_PLIST")
BUNDLE_IDENTIFIER=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$INFO_PLIST")

[[ "$DISPLAY_NAME" == "Glass Photo Viewer" ]]
[[ "$EXECUTABLE_NAME" == "Glass Photo Viewer" ]]
[[ "$BUNDLE_IDENTIFIER" == "ns.glass-photo-viewer" ]]
test -x "$EXECUTABLE"

printf 'Ad-hoc signing and verifying the app...\n'
codesign --force --deep --sign - --timestamp=none "$APP"
codesign --verify --deep --strict --verbose=4 "$APP"

mkdir -p "$DIST_DIR"
ARCHIVE="$DIST_DIR/Glass-Photo-Viewer-v${VERSION}-macOS.zip"
rm -f "$ARCHIVE"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ARCHIVE"

rm -rf "$VERIFY_DIR"
mkdir -p "$VERIFY_DIR"
ditto -x -k "$ARCHIVE" "$VERIFY_DIR"
codesign --verify --deep --strict --verbose=4 "$VERIFY_DIR/Glass Photo Viewer.app"

HASH=$(shasum -a 256 "$ARCHIVE" | cut -d ' ' -f 1)
printf 'Built: %s\n' "$APP"
printf 'Archive: %s\n' "$ARCHIVE"
printf 'Version: %s\n' "$VERSION"
printf 'SHA-256: %s\n' "$HASH"

if [[ "$LAUNCH_AFTER_BUILD" == true ]]; then
    open -n "$VERIFY_DIR/Glass Photo Viewer.app"
fi
