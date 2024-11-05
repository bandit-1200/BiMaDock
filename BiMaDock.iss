[Setup]
AppName=BiMaDock
AppVersion=1.0
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=D:\a\BiMaDock\BiMaDock\publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
SetupIconFile=D:\a\BiMaDock\BiMaDock\publish\BiMaDock.exe  ; Verwendet das eingebettete Symbol der EXE-Datei
AppPublisher=Marco Bilz

[Files]
Source: "D:\a\BiMaDock\BiMaDock\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent

[Messages]
BevelMessage=Willkommen bei der Installation von BiMaDock!
