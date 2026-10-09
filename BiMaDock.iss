; Die Version wird beim Kompilieren aus version.json uebergeben: ISCC /DMyAppVersion=<Version>
; (siehe get_version.ps1). Ohne Angabe entsteht ein erkennbarer Entwicklungs-Build.
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#define ProjectDir AddBackslash(SourcePath)

[Setup]
AppName=BiMaDock
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\BiMaDock
DefaultGroupName=BiMaDock
PrivilegesRequired=lowest
UsePreviousAppDir=no
OutputDir={#ProjectDir}publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
AppPublisher=Marco Bilz


[Files]
Source: "{#ProjectDir}publish\*"; DestDir: "{app}"; Excludes: "BiMaDockSetup.exe"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ProjectDir}Resources\Icons\*"; DestDir: "{localappdata}\BiMaDock\Icons"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start BiMaDock mit Windows"; GroupDescription: "Autostart"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent; Tasks: autostart

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BiMaDock"; ValueData: """{app}\BiMaDock.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Messages]
BeveledLabel=Willkommen bei der Installation von BiMaDock!
