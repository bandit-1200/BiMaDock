# Versionsverlauf

Hier werden neue Funktionen, Fehlerbehebungen und Optimierungen je Version zusammengefasst. Die historischen Einträge wurden aus Git-Tags und Commit-Historie rekonstruiert. Wiederholte Release- und Build-Tags ohne erkennbare zusätzliche Änderungen sind zusammengefasst; uneinheitliche historische Tag-Namen werden nicht nachträglich umbenannt.

## 26.10.17 – In Arbeit

- Dock wird nach dem Programmstart zuverlässig ausgeblendet; bisher blieb es bis zum ersten Mauskontakt sichtbar, weil das Ausblenden vor dem ersten Layout lief.

## 26.10.16 – In Arbeit

- Automatisierte UI-Tests mit FlaUI (`tests\BiMaDock.UITests`, Start über `.\ui-test.ps1`): Start, Ein-/Ausblenden, Kontextmenü, Kategorien, Bearbeiten, Löschen, Einstellungen, Über-Fenster, Autostart, Drag & Drop von Dateien und Elementen sowie Beenden. Ohne `BIMADOCK_UI_TESTS=1` werden sie übersprungen, damit `dotnet test` und die CI unverändert laufen.
- Datenordner über `BIMADOCK_DATA_DIR` umstellbar; alle Pfade (Log, Einstellungen, Icons) laufen jetzt über `AppPaths`. Mit eigenem Datenordner gilt eine eigene Einzelinstanz-Sperre.
- Update-Prüfung per `BIMADOCK_DISABLE_UPDATE_CHECK=1` abschaltbar, Autostart-Registry-Wertname per `BIMADOCK_STARTUP_VALUE_NAME` umstellbar (für Tests).
- Dock-Elemente tragen einen Automatisierungsnamen und eine -ID (Screenreader und UI-Tests).

## 26.10.15 – In Arbeit

- `CLAUDE.md` mit Projektüberblick für Claude Code ergänzt; verweist auf die Regeln in `AGENTS.md`.
- Ungenutzte NuGet-Pakete `Microsoft.AspNet.WebApi.Client` und `WindowsInput` entfernt.
- Ungenutzte Dateien entfernt: `TestWindow`, `Resources/eintest.cs`, `dummy.txt`, `dockItems.json` sowie die nicht verwendete GitVersion-Konfiguration (`GitVersion.yml`, `.config/dotnet-tools.json`).
- Toten Code entfernt: nie aufgerufene Methoden und Event-Handler, Test-/Debug-Reste, ungenutzte Felder, Usings, XAML-Elemente und Style-Ressourcen sowie auskommentierten Code.
- Doppelte Event-Abonnements für den Einblendverzögerungs-Regler und die Kategorie-Mausbewegung bereinigt.

## 26.10.14 – In Arbeit

- Installer auf Installation pro Windows-Benutzer ohne Administratorrechte umgestellt; Installationsziel, Startmenü, Desktop, Autostart und Icon-Daten liegen im Benutzerbereich.
- Die vorherige systemweite Installationsroute wird beim Upgrade nicht als Standardpfad übernommen.
- Inno-Setup-Warnung zu Admin-Installationen mit benutzerbezogenen Bereichen behoben.

## 26.10.13 – In Arbeit

- Release- und Autostart-Risiken behoben; Release-Hilfsskripte veröffentlichen/committen nicht mehr automatisch.
- Dock-Kategorien, Verknüpfungs-Drops, Dateipfade mit Leerzeichen und Farbvoreinstellungen robuster gemacht.
- Symbol- und Einstellungsdateiressourcen zuverlässig freigegeben bzw. atomar gespeichert; beschädigte Dock-Einstellungen werden gesichert und gemeldet.
- PNG-/ICO-Symbolauswahl, lokale Installerpfade und Swing-Konfiguration vervollständigt.
- Regressionstests ergänzt und die Audit-Befunde in [CODE_REVIEW_2026-10-09.md](CODE_REVIEW_2026-10-09.md) als umgesetzt markiert.

## 26.10.11 – In Arbeit

- Drag-and-Drop für Dateien und Dock-Elemente vereinheitlicht und robuster gemacht.
- Einfügepositionen und Verschiebeeffekte zentral behandelt; visuelle Drop-Vorschau verbessert.
- Tests für Einfügepositionen und Windows-Datei-Dropformate ergänzt.
- Fehler beim Start des Update-Dialogs behoben.
- Projektregeln zur Versions- und Changelog-Pflege zentral dokumentiert und automatische CI-Prüfung ergänzt.
- Branch- und Freigaberegel für die Entwicklung auf `dev` und Releases auf `main` festgehalten.
- Release-Workflow prüft jetzt die Übereinstimmung von Tag, Basisversion und datiertem Changelog-Eintrag.

## 26.10.7 (V26.10.07-Build1 bis Build3) – 2026-10-07

- Release-Workflow für Signierung und Veröffentlichung verbessert.
- GitHub-Token für die Veröffentlichung von Releases verwendet.
- Build- und Release-Korrekturen zusammengefasst; die Build-Tags enthalten überwiegend Workflow- und Release-Folgeänderungen.

## 26.11.2-Build2 – 2026-10-07

- Haupt- und Kategorie-Dock um Scrollpfeile sowie eine optimierte Dock-Breite erweitert.
- Dialoge überarbeitet und das verzögerte Einblenden des Docks stabilisiert.
- Hinweis zur Historie: Der Tag heißt `v26.11.2-Build2`, obwohl die Projektversion in diesem Entwicklungsstand `26.10.0` war.

## 25.11.2-Build1 – 2025-11-02

- Verzögertes Einblenden des Docks eingeführt.
- Für denselben Commit existiert zusätzlich der abweichend benannte Tag `v26.11.2-Build1`.

## 25.4.1-Build2 – 2025-04-12

- Dock nach einer Änderung der Bildschirmauflösung wieder zentriert.
- Fehlerbehebung für die Neupositionierung.

## 24.12.11-Build9 – 2024-12-19 bis 2024-12-22

- Verknüpfungen direkt aus dem Webbrowser im Haupt- und Kategorie-Dock ablegen.
- Drop-Verarbeitung und Platzhalter beim Verschieben überarbeitet.
- Dateipfade suchen und im Datei-Explorer öffnen.
- Drag-Erkennung und Schließen der Bearbeitungsfenster verbessert.

## 24.12.10-Build1 – 2024-12-18

- Release-/Versionsstand aktualisiert; die Git-Historie weist für diesen Tag keine eigenständige Nutzerfunktion aus.

## 24.12.9-Build1, Build7 und Build15 – 2024-12-15 bis 2024-12-18

- Update-Prüfung weiterentwickelt.
- Zielprogramm und Anwendungspfad von Verknüpfungen ermitteln und für Dock-Einträge verwenden.
- Symbole für Webverknüpfungen verbessert.
- Versions- und Release-Abläufe sowie Webseiten-Metadaten angepasst.

## 24.12.8-Buildserie – 2024-12-15

Enthält unter anderem die Tags `v24.12.8`, `v24.12.8-Build9`, `v24.12.8.9`, `v24.12.8.9.9`, `v24.12.8.11`, `v24.12.8.12`, `v24.12.8.12-Build12`, `v24.12.8.13-Build13`, `v24.12.8-Build16` und `v24.12.8-Build18`.

- Autoupdate, Versionsvergabe und Setup angepasst.
- Release-Skripte um automatische Versions- und Build-Tags erweitert.
- Viele Tags dokumentieren wiederholte Release-Skript- und Build-Korrekturen, nicht jeweils neue App-Funktionen.

## 24.12.7 – 2024-12-14

- Release-Stand erstellt; im zugehörigen Tag ist keine zusätzliche eigenständige Nutzeränderung dokumentiert.

## 24.12.6 – 2024-12-12 bis 2024-12-14

- Verhindert, dass mehrere App-Instanzen gleichzeitig laufen.
- Beenden-Dialog und einstellbare Feedback-Farbe ergänzt.
- Remote-Desktop-Verbindungen erkannt.
- Kategorie-Dock-Animationen ergänzt und verbessert.
- Fehler beim Doppelklick auf Kategorien sowie beim Ausblenden des Kategorie-Docks während des Verschiebens behoben.
- Dokumentation und Webseiten-Darstellung erweitert.

## 24.12.5 – 2024-12-12

- Release-Tag ohne separat dokumentierte Nutzeränderung; die Funktionsänderungen dieses Zeitraums sind unter 24.12.4 und 24.12.6 zusammengefasst.

## 24.12.4 – 2024-12-11 bis 2024-12-12

- Update-Prüfung ergänzt und mit einer Option zum zeitweisen Zurückstellen versehen.
- Info-Fenster mit Versions- und Autorenangabe ergänzt.
- Tooltips, Symbolanzeige und Einstellungsfenster verbessert.
- Projektwebseite und Dokumentation erweitert.

## 24.12.3 – 2024-12-09

- Akzentfarbe in den Einstellungen speicher- und ladbar gemacht.

## 24.12.2 – 2024-12-08

- Autostart-Funktion angepasst und stabilisiert.
- Fehler bei Elementnamen, die mit einer Zahl beginnen, behoben.

## 24.12.1 – 2024-12-07

- Versionsschema und Setup-/Release-Abläufe auf die 24.12-Reihe umgestellt.
- Dokumentation und Projekt-Setup aktualisiert.

## 0.0.8 – 2024-12-01

- Fehlerbehandlung beim Kopieren der Standard-Symbole verbessert.
- Setup- und Release-Abläufe weiter angepasst.

## 0.0.7 – 2024-11-28 bis 2024-12-01

- Dock-Animationen und Animationseinstellungen erweitert, darunter Swing-, Scale-, Rotate- und Translate-Effekte.
- Farb- und Akzentfarbeinstellungen ergänzt und Speichern/Laden der Einstellungen verbessert.
- Fehlerbehandlung und Einbindung der Symbole verbessert.

## 0.0.6 – 2024-11-06

- Dock-Symbole zuverlässig anklickbar gemacht.
- Autostart-Funktion ergänzt und Nullwertfehler behoben.
- Drag-and-Drop um visuelles Feedback für Dateitypen und verschiebbare Elemente ergänzt.
- GitHub-Actions-Workflow aktualisiert.

## 0.0.5 – 2024-11-05

- Nullprüfung für Versionsinformationen und Installationspfade verbessert.

## 0.0.4 – 2024-11-05

- Erforderliche Abhängigkeiten im Setup ergänzt.

## 0.0.3 – 2024-11-05

- Installations- und Release-Workflow weiter angepasst.

## 0.0.2 – 2024-11-05

- Setup und Release-Workflow mit eingebettetem Anwendungssymbol verbessert.

## 0.0.1 – 2024-11-04

- Erster getaggter Release-Stand.
- GitHub-Release-Workflow und Installer eingerichtet und korrigiert.

## Entwicklung vor dem ersten Release – 2024-10-21 bis 2024-11-03

- Grundfunktionen des Docks, darunter Elemente hinzufügen, öffnen, bearbeiten, löschen und verschieben, entwickelt.
- Kategorien und Kategorie-Dock mit Drag-and-Drop ergänzt.
- Kontextmenüs, Symbolverwaltung, Ausblenden des Docks und erste Animationen entwickelt.
- Einstellungen und Installer-/Release-Abläufe aufgebaut.
