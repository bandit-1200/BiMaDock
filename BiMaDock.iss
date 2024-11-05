[Setup]
AppName=BiMaDock
AppVersion=1.0.1-beta
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=D:\a\BiMaDock\BiMaDock\publish
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes
AppPublisher=Marco Bilz

[Files]
; Hauptanwendungsdateien
Source: "D:\a\BiMaDock\BiMaDock\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; .NET Framework 4.8 Installer zum temporären Verzeichnis hinzufügen
Source: "C:\Pfad\zu\dotNetFx48.exe"; DestDir: "{tmp}"; Flags: ignoreversion

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; IconFilename: "{app}\Resources\Icons\BiMaDock_V3.ico"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"; IconFilename: "{app}\Resources\Icons\BiMaDock_V3.ico"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; IconFilename: "{app}\Resources\Icons\BiMaDock_V3.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BiMaDock"; ValueData: """{app}\BiMaDock.exe"""; Flags: uninsdeletevalue

[Messages]
BevelMessage=Willkommen bei der Installation von BiMaDock!

[Code]
function NeedsDotNet48: Boolean;
begin
  Result := not IsDotNetDetected('v4.8');
end;

[Run]
; Führt die Installation von .NET Framework 4.8 aus, falls nicht vorhanden
Filename: "{tmp}\dotNetFx48.exe"; Parameters: "/q"; StatusMsg: "Installing .NET Framework 4.8..."; Check: NeedsDotNet48
