[Setup]
AppName=BiMaDock
AppVersion=1.0
DefaultDirName={commonpf}\BiMaDock
DefaultGroupName=BiMaDock
OutputDir=C:\Users\marco\Documents\code\BiMaDock\setup
OutputBaseFilename=BiMaDockSetup
Compression=lzma
SolidCompression=yes

[Files]
Source: "D:\a\BiMaDock\BiMaDock\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs



[Run]
Filename: "{app}\BiMaDock.exe"; Description: "Start BiMaDock"; Flags: nowait postinstall skipifsilent
