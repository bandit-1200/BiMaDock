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
