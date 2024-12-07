[Setup]
AppName=BiMaDock
AppVersion=24.12.07
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=D:\a\BiMaDock\BiMaDock\publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
AppPublisher=Marco Bilz


[Files]
Source: "D:\a\BiMaDock\BiMaDock\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D:\a\BiMaDock\BiMaDock\Resources\Icons\*"; DestDir: "{localappdata}\BiMaDock\Icons"; Flags: ignoreversion recursesubdirs createallsubdirs

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
