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
Source: "D:\a\BiMaDock\BiMaDock\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D:\Pfad\zu\deinem\dotNetFx48.exe"; DestDir: "{tmp}"; Flags: dontcopy

[Icons]
Name: "{group}\BiMaDock"; Filename: "{app}\BiMaDock.exe"
Name: "{group}\{cm:UninstallProgram,BiMaDock}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\BiMaDock"; Filename: "{app}\BiMaDock.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent
Filename: "{tmp}\dotNetFx48.exe"; Parameters: "/q"; StatusMsg: "Installing .NET Framework 4.8..."; Check: NeedsDotNet48

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BiMaDock"; ValueData: """{app}\BiMaDock.exe"""; Flags: uninsdeletevalue

[Messages]
BevelMessage=Willkommen bei der Installation von BiMaDock!

[Code]
function IsDotNetDetected(version: string): Boolean;
var
  install: Integer;
begin
  Result := RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version + '\Full', 'Install', install) and (install = 1);
end;

function NeedsDotNet48: Boolean;
begin
  Result := not IsDotNetDetected('v4\Full');
end;
