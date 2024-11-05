[Setup]
AppName=BiMaDock
AppVersion=1.0
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=D:\a\BiMaDock\BiMaDock\publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
SetupIconFile={app}\BiMaDock.exe
AppPublisher=Marco Bilz

[Files]
Source: "D:\a\BiMaDock\BiMaDock\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BiMaDock"; ValueData: """{app}\BiMaDock.exe"""; Flags: uninsdeletevalue

[Messages]
BevelMessage=Willkommen bei der Installation von BiMaDock!
