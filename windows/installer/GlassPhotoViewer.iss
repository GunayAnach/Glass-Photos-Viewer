#ifndef SourceDir
  #define SourceDir "..\..\dist\Glass-Photo-Viewer-Windows-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist"
#endif
#ifndef AppVersion
  #define AppVersion "1.2.0"
#endif

[Setup]
AppId={{9A61D15E-34ED-4697-A8DB-773AD0DC8AA4}
AppName=Glass Photo Viewer
AppVersion={#AppVersion}
AppVerName=Glass Photo Viewer {#AppVersion}
AppPublisher=Gunay Anach
AppPublisherURL=https://github.com/GunayAnach/Glass-Photos-Viewer
AppSupportURL=https://github.com/GunayAnach/Glass-Photos-Viewer/issues
AppUpdatesURL=https://github.com/GunayAnach/Glass-Photos-Viewer/releases
DefaultDirName={localappdata}\Programs\Glass Photo Viewer
DefaultGroupName=Glass Photo Viewer
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Glass-Photo-Viewer-Windows-x64-Setup
SetupIconFile=..\GlassPhotoViewer.WinUI\Assets\GlassPhotoViewer.ico
UninstallDisplayIcon={app}\GlassPhotoViewer.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
CloseApplications=force
CloseApplicationsFilter=GlassPhotoViewer.exe,GlassPhotos.WinUI.exe,*.dll,*.chm

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion overwritereadonly recursesubdirs createallsubdirs

[InstallDelete]
; Remove renamed 1.2-era product files while keeping the stable AppId for in-place upgrades.
Type: files; Name: "{app}\GlassPhotos.*"
Type: files; Name: "{app}\Assets\GlassPhotos.*"
Type: files; Name: "{group}\Glass Photos.lnk"
Type: files; Name: "{autodesktop}\Glass Photos.lnk"

[Icons]
Name: "{group}\Glass Photo Viewer"; Filename: "{app}\GlassPhotoViewer.exe"
Name: "{autodesktop}\Glass Photo Viewer"; Filename: "{app}\GlassPhotoViewer.exe"; Tasks: desktopicon

[Registry]
; One application ProgID, open command, and icon for every supported image type.
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image"; ValueType: string; ValueName: ""; ValueData: "Glass Photo Viewer Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\GlassPhotoViewer.exe,0"
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\GlassPhotoViewer.exe"" ""%1"""

; Keep the stable internal capability/ProgID identity so existing defaults survive the rename.
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Glass Photo Viewer"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Fast native photo viewer for Windows"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: none; ValueName: "Glass Photos"; Flags: deletevalue
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Glass Photo Viewer"; ValueData: "Software\GlassPhotos\Capabilities"; Flags: uninsdeletevalue

Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heic"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heif"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webp"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".dng"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".nef"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cr2"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".arw"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".raf"; ValueData: "GlassPhotos.Image"

; Add Glass Photo Viewer to each extension's Open with list without overriding Windows UserChoice.
Root: HKCU; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.heic\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.heif\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tif\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tiff\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.webp\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.dng\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.nef\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.cr2\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.arw\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.raf\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\GlassPhotoViewer.exe"; Description: "Launch Glass Photo Viewer"; Flags: nowait postinstall skipifsilent
