# BiMaDock 2

> Ab Version 2.0.0 ist BiMaDock komplett neu entwickelt. Die erste Version (26.10.x) liegt auf dem Branch `legacy-v1`.
> Installer und Updates: [Releases](https://github.com/bandit-1200/BiMaDock/releases) · Website: [bandit-1200.github.io/BiMaDock](https://bandit-1200.github.io/BiMaDock/)

Neuentwicklung von BiMaDock: eine Dock-Leiste am oberen Bildschirmrand für Windows mit Hauptdock und aufklappbarem Kategorie-Dock.

## Stand (Phase 1)

- Hauptdock oben mittig, blendet sich automatisch aus und bei Mausberührung des sichtbaren Rands wieder ein
- Kategorie-Dock klappt animiert unter dem angeklickten Kategorie-Symbol auf
- Starten von Programmen, Dateien, Ordnern, Verknüpfungen und Weblinks
- Scharfe Symbole (128 px) bei jeder Skalierung, Per-Monitor-DPI
- Beim ersten Start werden die Einträge der alten Version aus `%LOCALAPPDATA%\BiMaDock\docksettings.json` übernommen (die alte Datei wird nur gelesen)
- Kontextmenü: Einstellungen, Symbolbibliothek, Aufräumen, Updates, Datenordner, Über, Beenden

## Versionen und Updates

- Versionsnummer in `Directory.Build.props` (SemVer), Tags `vX.Y.Z`
- Release: Version erhöhen, Abschnitt in `CHANGELOG.md`, dann `.	oolselease.ps1` (baut den Installer, setzt den Tag, pusht nach Gitea und GitHub, legt das GitHub-Release an)
- Die App sucht über `api.github.com/repos/bandit-1200/BiMaDock/releases/latest` nach dem Anhang `BiMaDock2-Setup-*.exe`

## Aufbau

| Projekt | Inhalt |
|---|---|
| `src/BiMaDock.Core` | Modell (`DockEntry`, `DockLayout`), Speichern (`DockStore`), Import alter Daten. Ohne WPF, voll testbar. |
| `src/BiMaDock` | WPF-App: MVVM (CommunityToolkit.Mvvm), Dependency Injection, Dienste für Symbole, Starten und Protokoll |
| `tests/BiMaDock.Core.Tests` | xUnit-Tests für den Kern |

Daten: `%LOCALAPPDATA%\BiMaDock2\dock.json`, Protokolle in `logs\`. Mit der Umgebungsvariable `BIMADOCK_DATA_DIR` lässt sich ein anderer Datenordner verwenden.

## Befehle

```
dotnet build BiMaDock2.sln
dotnet test BiMaDock2.sln
dotnet run --project src/BiMaDock
```

Voraussetzung: .NET 10 SDK.

## Lizenz

MIT, siehe [LICENSE](LICENSE). Entwickler: Marco Bilz.
