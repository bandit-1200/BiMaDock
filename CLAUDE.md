@AGENTS.md

# Projektüberblick

BiMaDock ist eine personalisierbare Dock-Leiste für Windows (WPF, `net8.0-windows`): ein rahmenloses, oben liegendes Fenster am Bildschirmrand mit automatischem Ein-/Ausblenden zum Starten von Programmen, Dateien, Ordnern, Weblinks und Kategorien. Einträge werden per Drag & Drop hinzugefügt.

## Befehle

- Build: `dotnet build BiMaDock.sln -c Release`
- Tests: `.\test.ps1` oder `dotnet test BiMaDock.sln -c Release`
- UI-Tests (FlaUI, steuern Maus und Tastatur): `.\ui-test.ps1 -c Release` – ohne `BIMADOCK_UI_TESTS=1` werden sie übersprungen
- Publish: `dotnet publish BiMaDock.csproj -c Release -r win-x64 -o publish`
- Lokaler Installer: `BiMaDock_local_setup.bat` (Inno Setup 6, Ausgabe nach `setup\`)
- Version lesen/prüfen: `.\get_version.ps1` (Installer erhalten sie per `ISCC /DMyAppVersion`)

## Wichtige Dateien

- `MainWindow.xaml.cs` (Konstruktor, gemeinsame Felder) plus Teildateien: `MainWindow.Visibility.cs` (Ein-/Ausblenden), `MainWindow.DragDrop.cs`, `MainWindow.CategoryDock.cs`, `MainWindow.ContextMenu.cs` (Menü, Autostart, Update-Prüfung), `MainWindow.Native.cs` (P/Invoke, RDP)
- `DockManager.cs`: Laden/Speichern, Einträge und Kategorien, Drop-Verarbeitung
- `DockDropPosition.cs`: Drop-Formate und Einfügeposition
- `DockItemSettingsStore.cs` / `SettingsManager.cs`: atomares Speichern von `docksettings.json`
- `SettingsWindow.xaml.cs` plus `SettingsWindow.Colors.cs`, `.Animations.cs`, `.AnimationEditors.cs`; Einstellungen in `StyleSettings.json` (typisiert über `StyleSettingsFile.cs`)
- `UpdateChecker.cs` / `ReleaseVersion.cs`: GitHub-Release-Prüfung (Asset `BiMaDockSetup.exe`)
- `StartupManager.cs`: Autostart über `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `GlobalMouseHook.cs`: Klick außerhalb blendet das Dock aus
- `AppPaths.cs`: Benutzerdaten unter `%LOCALAPPDATA%\BiMaDock\`
- Tests: `tests\BiMaDock.Tests` (xUnit, Logik) und `tests\BiMaDock.UITests` (FlaUI, startet die App mit temporärem Datenordner über `BIMADOCK_DATA_DIR`, ohne Update-Prüfung und mit eigenem Autostart-Wertnamen)

## Konventionen

- UI-Texte und Kommentare auf Deutsch, Bezeichner überwiegend Englisch.
- Dateien verwenden CRLF-Zeilenenden.
- Die CI (`.github\scripts\validate-versioning.ps1`) verlangt bei jeder Änderung einen Eintrag in `CHANGELOG.md` unter `## Unveröffentlicht`; die Version wird nur für Releases gesetzt.
