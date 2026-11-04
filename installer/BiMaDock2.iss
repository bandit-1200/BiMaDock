; Installer für BiMaDock 2 (Inno Setup 6). Bauen über installer\build.ps1.
;
; Wichtig zu den Symbolen (Fehler der alten Version): Der Installer schreibt NICHTS in ein Benutzerprofil.
; Die mitgelieferte Symbolbibliothek liegt im Programmordner ({app}\Icons) und gilt damit für alle Benutzer,
; egal ob ein Admin für alle oder ein Benutzer nur für sich installiert. Eigene Symbole, Dock und Einstellungen
; legt die App beim ersten Start im Profil des Benutzers an, der sie ausführt (%LOCALAPPDATA%\BiMaDock2).
; Aus demselben Grund setzt der Installer keinen Autostart (HKCU wäre beim Admin gelandet); das macht die App
; selbst unter Einstellungen.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
LicenseFile=..\LICENSE
AppId={{6E3C2B1D-9A7F-4C35-B0E2-6B1D2C8F4A27}
AppName=BiMaDock 2
AppVersion={#AppVersion}
AppVerName=BiMaDock 2 {#AppVersion}
AppPublisher=Marco Bilz
DefaultDirName={autopf}\BiMaDock2
DefaultGroupName=BiMaDock 2
DisableProgramGroupPage=yes
; Standard: nur für den aktuellen Benutzer, ohne Adminrechte. Wahlweise für alle Benutzer (fragt nach Adminrechten).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
OutputDir=..\artifacts\installer
OutputBaseFilename=BiMaDock2-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\BiMaDock.exe
SetupIconFile=..\src\BiMaDock\BiMaDock.ico
; Laufende Instanz vor dem Überschreiben schließen
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Programm samt Symbolbibliothek (publish\Icons\...) in den Programmordner
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Bibliothek bei Updates sauber ersetzen (umbenannte Symbole sollen nicht doppelt liegen)
Type: filesandordirs; Name: "{app}\Icons"

[Icons]
Name: "{autoprograms}\BiMaDock 2"; Filename: "{app}\BiMaDock.exe"
Name: "{autodesktop}\BiMaDock 2"; Filename: "{app}\BiMaDock.exe"; Tasks: desktopicon

[Run]
; runasoriginaluser: auch nach einer Admin-Installation startet das Dock als der angemeldete Benutzer,
; damit dessen Daten in seinem eigenen Profil landen.
Filename: "{app}\BiMaDock.exe"; Description: "{cm:LaunchProgram,BiMaDock 2}"; Flags: nowait postinstall skipifsilent runasoriginaluser
; Update aus der App (/UPDATE): auch im stillen Modus danach wieder starten.
Filename: "{app}\BiMaDock.exe"; Flags: nowait runasoriginaluser; Check: IsUpdate

[Code]
function IsUpdate: Boolean;
begin
  Result := ExpandConstant('{param:UPDATE|0}') <> '0';
end;

// Alte Version 1 ("BiMaDock_is1") vor der Installation entfernen. Die Daten in %LOCALAPPDATA%\BiMaDock
// bleiben dabei stehen, das neue Dock übernimmt sie beim ersten Start.
const
  LegacyKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\BiMaDock_is1';
  LegacyArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART';

function LegacyUninstaller(RootKey: Integer): String;
begin
  Result := '';
  if RegQueryStringValue(RootKey, LegacyKey, 'UninstallString', Result) then
  begin
    Result := RemoveQuotes(Trim(Result));
    if not FileExists(Result) then
      Result := '';
  end;
end;

procedure StopLegacyDock;
var
  Code: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM BiMaDock.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

procedure RemoveLegacy(RootKey: Integer; Verb: String);
var
  Exe: String;
  Code: Integer;
begin
  Exe := LegacyUninstaller(RootKey);
  if Exe = '' then
    Exit;
  StopLegacyDock;
  Log('Alte Version wird deinstalliert: ' + Exe);
  // Für alle Benutzer installiert: runas löst die Windows-Abfrage nach Adminrechten aus.
  if not ShellExec(Verb, Exe, LegacyArgs, '', SW_HIDE, ewWaitUntilTerminated, Code) then
    Log('Alte Version nicht deinstalliert (Code ' + IntToStr(Code) + ')');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  RemoveLegacy(HKCU, '');
  RemoveLegacy(HKLM32, 'runas');
  if IsWin64 then
    RemoveLegacy(HKLM64, 'runas');
end;
