#ifndef SourceDir
  #define SourceDir "..\..\dist\Glass-Photos-Windows-x64-Legacy-Fixture"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist"
#endif

[Setup]
AppId={{9A61D15E-34ED-4697-A8DB-773AD0DC8AA4}
AppName=Glass Photos
AppVersion=1.1.0
AppVerName=Glass Photos 1.1.0
AppPublisher=Gunay Anach
DefaultDirName={localappdata}\Programs\Glass Photos
DefaultGroupName=Glass Photos
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Glass-Photos-Windows-x64-Legacy-Setup
SetupIconFile=..\GlassPhotoViewer.WinUI\Assets\GlassPhotoViewer.ico
UninstallDisplayIcon={app}\GlassPhotos.WinUI.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
CloseApplications=force
CloseApplicationsFilter=GlassPhotos.WinUI.exe,*.dll,*.chm

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Glass Photos"; Filename: "{app}\GlassPhotos.WinUI.exe"

[Registry]
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image"; ValueType: string; ValueName: ""; ValueData: "Glass Photos Image"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\GlassPhotos.WinUI.exe,0"
Root: HKCU; Subkey: "Software\Classes\GlassPhotos.Image\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\GlassPhotos.WinUI.exe"" ""%1"""
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Glass Photos"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Fast native photo viewer for Windows"
Root: HKCU; Subkey: "Software\GlassPhotos\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "GlassPhotos.Image"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Glass Photos"; ValueData: "Software\GlassPhotos\Capabilities"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "GlassPhotos.Image"; ValueData: ""; Flags: uninsdeletevalue
