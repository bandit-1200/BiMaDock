# BiMaDock 2

Neuentwicklung von BiMaDock: eine Dock-Leiste am oberen Bildschirmrand für Windows mit Hauptdock und aufklappbarem Kategorie-Dock.

## Stand (Phase 1)

- Hauptdock oben mittig, blendet sich automatisch aus und bei Mausberührung des sichtbaren Rands wieder ein
- Kategorie-Dock klappt animiert unter dem angeklickten Kategorie-Symbol auf
- Starten von Programmen, Dateien, Ordnern, Verknüpfungen und Weblinks
- Scharfe Symbole (128 px) bei jeder Skalierung, Per-Monitor-DPI
- Beim ersten Start werden die Einträge der alten Version aus `%LOCALAPPDATA%\BiMaDock\docksettings.json` übernommen (die alte Datei wird nur gelesen)
- Kontextmenü: Neu laden, Datenordner öffnen, Beenden

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
