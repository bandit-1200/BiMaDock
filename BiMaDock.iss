[Setup]
AppName=BiMaDock
AppVersion=1.0
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=D:\a\BiMaDock\BiMaDock\publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
SetupIconFile=C:\Temp\icon.ico
AppPublisher="Marco Bilz"

[Files]
Source: "D:\a\BiMaDock\BiMaDock\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\marco\Documents\code\BiMaDock\Resources\Icons\BiMaDock_V3.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; IconFilename: "{app}\BiMaDock_V3.ico"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"; IconFilename: "{app}\BiMaDock_V3.ico"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; IconFilename: "{app}\BiMaDock_V3.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent

[Messages]
BevelMessage="Willkommen bei der Installation von BiMaDock!"
