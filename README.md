# BiMaDock

**Die Dock-Leiste, die Windows gefehlt hat.**

Unsichtbar, bis du sie brauchst. Ein Stups an den oberen Bildschirmrand, und all deine Programme, Ordner, Dateien und Webseiten gleiten herein. Ein Klick, und du bist da. Kein Suchen im Startmenü, kein Desktop voller Verknüpfungen, keine überladene Taskleiste.

[**Herunterladen**](https://github.com/bandit-1200/BiMaDock/releases/latest) · [Website](https://bandit-1200.github.io/BiMaDock/) · [Versionsverlauf](CHANGELOG.md)

Kostenlos, ohne Werbung, ohne Konto, Open Source. Windows 10 und 11.

## Warum BiMaDock

- **Schneller an allem:** Programme, Server-Verbindungen, Projektordner und Webseiten liegen da, wo deine Maus ohnehin hinfährt.
- **Aufgeräumter Bildschirm:** Das Dock verschwindet von selbst und tritt bei Vollbild (Remotedesktop, Spiele, Videos) automatisch zurück. Startmenü und Suche zählen nicht als Vollbild, das Dock bleibt davor sichtbar.
- **Macht Spaß:** Lupe wie beim Mac, Kategorien, die wie Wasser aus dem Dock fließen, und jede Animation nach deinem Geschmack.

## Funktionen

- **Hauptdock und Kategorie-Dock:** Kategorien fassen Einträge zusammen und fließen animiert unter dem Hauptdock hervor
- **Drag & Drop:** Dateien, Ordner, Verknüpfungen und Links aus Explorer und Browser sowie Programme aus dem Startmenü (auch Store-Apps) oder dem Ordner `shell:AppsFolder` ins Dock ziehen (dazu mit gedrückter Maustaste an den oberen Rand fahren, das Dock gleitet heraus), verschieben mit animierter Lücke
- **Dateien öffnen:** Eine Datei auf ein Programm im Dock ziehen, und sie öffnet sich darin
- **Als Administrator starten:** einmalig per Rechtsklick oder fest pro Eintrag, dazu Startparameter und Arbeitsordner
- **Trenner** gliedern das Dock in Gruppen
- **Themes:** acht mitgelieferte Looks, eigene speichern und als Datei weitergeben
- **Lupe** und Hover-Effekte (Vergrößern, Hüpfen, Schaukeln, Wackeln, Pulsieren), jeweils mit eigenen Reglern
- **Scharfe, einheitliche Symbole** bei jeder Skalierung, dazu eine Bibliothek mit 98 Symbolen
- **Aufräumen:** findet fehlende Ziele, doppelte Einträge und leere Kategorien; fehlende Ziele erscheinen schon vorher blass
- **Rückgängig** für bis zu 20 Schritte
- **Updates** automatisch oder auf Nachfrage
- Bewusst **keine Anzeige laufender Programme**, dafür gibt es die Taskleiste

## Installation

Setup `BiMaDock2-Setup-…exe` aus den [Releases](https://github.com/bandit-1200/BiMaDock/releases/latest) starten, wahlweise nur für dich (ohne Adminrechte) oder für alle Benutzer. Das Setup ist nicht signiert: Bei der SmartScreen-Warnung auf *Weitere Informationen* → *Trotzdem ausführen* klicken.

Umstieg von Version 1: Einträge, Kategorien und Symbole werden beim ersten Start übernommen, die alte Version wird danach still entfernt. Sie liegt weiterhin auf dem Branch `legacy-v1`.

## Für Entwickler

### Aufbau

| Projekt | Inhalt |
|---|---|
| `src/BiMaDock.Core` | Modell (`DockEntry`, `DockLayout`), Speichern, Import alter Daten, Einstellungen und Themes, Aufräumen, Startparameter. Ohne WPF, voll testbar. |
| `src/BiMaDock` | WPF-App: MVVM (CommunityToolkit.Mvvm), Dependency Injection, Dienste für Symbole, Starten, Updates und Protokoll |
| `tests/BiMaDock.Core.Tests` | xUnit-Tests für den Kern |

Daten liegen in `%LOCALAPPDATA%\BiMaDock2`:

| Pfad | Inhalt |
|---|---|
| `dock.json` | Einträge und Kategorien |
| `settings.json` | Einstellungen |
| `Themes\*.json` | eigene Themes, eine Datei je Theme |
| `Icons\` | eigene Symbole |
| `IconCache\` | Zwischenspeicher für Programmsymbole, darf gelöscht werden |
| `logs\` | Protokolle |

Mit der Umgebungsvariable `BIMADOCK_DATA_DIR` lässt sich ein anderer Datenordner verwenden. `BIMADOCK_PERF=1` schreibt Bildrate und Rechenzeit der Lupe ins Protokoll.

### Befehle

```
dotnet build BiMaDock2.sln
dotnet test BiMaDock2.sln
dotnet run --project src/BiMaDock
```

Voraussetzung: .NET 10 SDK.

### Versionen und Releases

- Versionsnummer in `Directory.Build.props` nach dem Schema Jahr.Monat.Nummer (z. B. 26.11.6), Tags `vJJ.M.N`
- Release: Version erhöhen, Abschnitt `## <Version>` in `CHANGELOG.md`, dann `.\tools\release.ps1`. Das Skript prüft, testet, setzt den Tag und pusht nach Gitea und GitHub
- GitHub Actions (`.github/workflows/release.yml`) baut daraus Installer und Release, signiert, wenn die Secrets `CERTIFICATE_FILE` und `CERTIFICATE_PASSWORD` gesetzt sind. Jedes Release enthält das Setup zusätzlich als `BiMaDockSetup.exe`, den Namen sucht der Updater der alten Version 1
- Die App sucht über `api.github.com/repos/bandit-1200/BiMaDock/releases/latest` nach dem Anhang `BiMaDock2-Setup-*.exe`

## Lizenz

MIT, siehe [LICENSE](LICENSE). Entwickler: Marco Bilz.
