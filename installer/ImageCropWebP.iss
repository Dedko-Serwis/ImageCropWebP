#ifndef MyAppVersion
  #error "MyAppVersion must be provided by build-release.ps1"
#endif
#ifndef SourceDir
  #error "SourceDir must be provided by build-release.ps1"
#endif
#define MyAppName "ImageCropWebP"
#define MyAppPublisher "DEDKO-Serwis"
[Setup]
AppId={{D583D13C-7F89-4E3B-95CE-AB5B1D6B39B8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\dist
OutputBaseFilename=ImageCropWebP-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\ImageCropWebP.exe
ShowLanguageDialog=no
LanguageDetectionMethod=uilanguage
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ImageCropWebP"; Filename: "{app}\ImageCropWebP.exe"
Name: "{autodesktop}\ImageCropWebP"; Filename: "{app}\ImageCropWebP.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\ImageCropWebP.exe"; Description: "{cm:LaunchProgram,ImageCropWebP}"; Flags: nowait postinstall skipifsilent
