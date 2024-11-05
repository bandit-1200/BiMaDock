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

[Code]
function IsDotNet48Installed: Boolean;
var
  Success: Boolean;
  InstallFlag: Cardinal;
begin
  Success := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', InstallFlag);
  Result := Success and (InstallFlag >= 528040); // 528040 entspricht .NET Framework 4.8
end;

procedure DownloadFile(URL, DestFile: string);
var
  WinInet: Variant;
  FileStream: TFileStream;
begin
  WinInet := CreateOleObject('WinHttp.WinHttpRequest.5.1');
  WinInet.Open('GET', URL, False);
  WinInet.Send;
  
  FileStream := TFileStream.Create(DestFile, fmCreate);
  try
    FileStream.WriteBuffer(WinInet.ResponseBody[1], Length(WinInet.ResponseBody));
  finally
    FileStream.Free;
  end;
end;

procedure InstallDotNet48;
begin
  DownloadFile('https://download.microsoft.com/download/2/D/8/2D8F6DD1-3AD0-4D57-A9E9-1D2B654CE396/NDP48-x86-x64-AllOS-ENU.exe', ExpandConstant('{tmp}\NDP48-x86-x64-AllOS-ENU.exe'));
  Exec(ExpandConstant('{tmp}\NDP48-x86-x64-AllOS-ENU.exe'), '/quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet48Installed then
  begin
    MsgBox('Die .NET Framework 4.8-Komponente wird installiert.', mbInformation, MB_OK);
    InstallDotNet48;
  end;
  Result := True;
end;
