# Versionsverlauf

Hier werden neue Funktionen, Fehlerbehebungen und Optimierungen je Version zusammengefasst. Die historischen Einträge wurden aus Git-Tags und Commit-Historie rekonstruiert. Wiederholte Release- und Build-Tags ohne erkennbare zusätzliche Änderungen sind zusammengefasst; uneinheitliche historische Tag-Namen werden nicht nachträglich umbenannt.

## Unveröffentlicht

Sammelt alle Änderungen seit 26.10.7. Die Zwischenstände 26.10.11 bis 26.10.18 waren reine Entwicklungsversionen und wurden nie veröffentlicht.

### Neuigkeiten

- Verschieben wie beim macOS-Dock: Beim Ziehen öffnet sich im Haupt- und im Kategorie-Dock eine animierte Lücke in Elementgröße; vorhandene Elemente gleiten zur Seite, sobald das gezogene Element über die Hälfte eines Nachbarn hinausgeht. Das gezogene Element verschwindet an seinem alten Platz. Gilt auch für Dateien und Links von außen.
- Beim Verschieben folgt ein halbtransparentes Bild des Elements der Maus, auch außerhalb des Docks.

- Automatisierte UI-Tests mit FlaUI (`tests\BiMaDock.UITests`, Start über `.\ui-test.ps1`): Start, erster Start ohne Daten, Ein-/Ausblenden, Kontextmenü, Kategorien, Bearbeiten, Löschen, Einstellungen, Über-Fenster, Autostart, Drag & Drop von Dateien und Elementen sowie Beenden. Ohne `BIMADOCK_UI_TESTS=1` werden sie übersprungen.
- Datenordner über `BIMADOCK_DATA_DIR` umstellbar, Update-Prüfung per `BIMADOCK_DISABLE_UPDATE_CHECK=1` abschaltbar, Autostart-Wertname per `BIMADOCK_STARTUP_VALUE_NAME` umstellbar (für Tests); mit eigenem Datenordner gilt eine eigene Einzelinstanz-Sperre.
- Dock-Elemente tragen einen Automatisierungsnamen und eine -ID (Screenreader und UI-Tests).
- Installer auf Installation pro Windows-Benutzer ohne Administratorrechte umgestellt; Installationsziel, Startmenü, Desktop, Autostart und Icon-Daten liegen im Benutzerbereich. Die vorherige systemweite Installationsroute wird beim Upgrade nicht als Standardpfad übernommen.

### Verbesserungen

- Schärfere Icons auf Bildschirmen mit hoher Skalierung: Programm-, Datei- und Ordnersymbole werden über `IShellItemImageFactory` in 96 px statt 32 px geladen (mit Transparenz), PNG-Icons ebenfalls in 96 px dekodiert und hochwertig verkleinert.
- `MainWindow` und `SettingsWindow` in thematische Teildateien aufgeteilt (`MainWindow.DragDrop.cs`, `.CategoryDock.cs`, `.ContextMenu.cs`, `.Visibility.cs`, `.Native.cs`; `SettingsWindow.Colors.cs`, `.Animations.cs`, `.AnimationEditors.cs`); reine Verschiebung ohne Verhaltensänderung.
- Drag & Drop flüssiger: Der Einfügestrich wird nur noch bei einer echten Positionsänderung verschoben statt bei jeder Mausbewegung neu eingefügt; das Dock wackelt beim Ziehen nicht mehr.
- Beim Ziehen öffnet sich eine Kategorie zuverlässig, sobald man über sie fährt, und wird nicht mehr mehrfach neu aufgebaut; kein Flackern mehr beim Wechsel zwischen Elementen.
- Der Cursor zeigt nur noch dort „Ablegen möglich“, wo tatsächlich abgelegt werden kann; bei nicht ablegbaren Daten blendet sich das Dock gar nicht erst ein.
- Überfüllte Docks scrollen beim Ziehen automatisch, wenn man an den linken oder rechten Rand kommt.
- Nach dem Ablegen in einer Kategorie bleibt diese sichtbar, statt das Dock aus- und die Kategorie neu aufzubauen.
- Abgelegte Links werden geprüft (http, https, file) und erhalten den Hostnamen als Namen; Links aus dem Browser werden bevorzugt aus dem URL-Format gelesen. Verknüpfungen (`.lnk`) erhalten ihren Namen ohne Endung, Laufwerke einen lesbaren Namen; doppelt abgelegte Dateien werden übersprungen.

- Schnellerer Start (Hauptfenster-Aufbau etwa 0,7 statt 2 Sekunden bei rund 50 Einträgen): Icons werden zwischengespeichert, PNG-Icons nur in benötigter Größe dekodiert und ohne Dateisperre geladen; Shell-Icons ohne GDI+-Umweg erzeugt. `System.Drawing.Common` wird nicht mehr benötigt.
- Release-Builds werden mit ReadyToRun veröffentlicht (schnellerer Kaltstart).
- `StyleSettings.json` wird typisiert statt über `dynamic` gelesen; ein ungültiger Inhalt führt nicht mehr zu einem Fehler.
- Hover-Animationen verwenden pro Element eine wiederverwendete Transformation und kehren immer in die Ruhelage zurück.
- Weniger Rechenarbeit bei jeder Mausbewegung über dem Dock; Prozessobjekte beim Öffnen von Einträgen werden freigegeben.

- Versionierung vereinfacht: Schema `JJ.MM.N` (Jahr, Monat, fortlaufende Release-Nummer), Version wird nur noch für ein Release erhöht, Änderungen sammeln sich bis dahin unter „Unveröffentlicht“. `version.json` ist die einzige Quelle; die Installer erhalten die Version beim Kompilieren (`ISCC /DMyAppVersion`), `update_version.ps1` wurde durch `get_version.ps1` ersetzt. Die Release-Prüfung verlangt eine Version größer als alle bisherigen Tags.
- Globaler Maus-Hook ist nur noch aktiv, solange das Dock sichtbar ist, und wertet Klicks asynchron aus.
- Remotedesktop-Erkennung prüft das Vordergrundfenster statt regelmäßig die Prozessliste; ein minimiertes RDP-Fenster nimmt dem Dock nicht mehr den Vordergrund.
- Drag-and-Drop für Dateien und Dock-Elemente vereinheitlicht; Einfügepositionen zentral behandelt, Drop-Vorschau verbessert.
- Dock-Kategorien, Verknüpfungs-Drops, Dateipfade mit Leerzeichen und Farbvoreinstellungen robuster gemacht; Einstellungsdateien werden atomar gespeichert, beschädigte Dock-Einstellungen gesichert und gemeldet.
- Alle Datenpfade (Log, Einstellungen, Icons) laufen über `AppPaths`.
- Aufräumarbeiten: ungenutzte NuGet-Pakete (`Microsoft.AspNet.WebApi.Client`, `WindowsInput`), Dateien (`TestWindow`, `Resources/eintest.cs`, `dummy.txt`, `dockItems.json`, GitVersion-Konfiguration), toter Code, auskommentierter Code und Debug-Ausgaben in Maus-, Drag- und Animationspfaden entfernt.
- Release-Workflow prüft Tag, Version und datierten Changelog-Eintrag; Release-Hilfsskripte veröffentlichen oder committen nicht mehr automatisch. Projektregeln in `AGENTS.md`, Projektüberblick für Claude Code in `CLAUDE.md`.
- Tests für Einfügepositionen, Windows-Datei-Dropformate, Einstellungsspeicherung, Versionen und Autostart ergänzt.

### Fehlerbehebungen

- Beliebiger oder leerer Text wird beim Ablegen nicht mehr als Dock-Element übernommen.
- Ablegen in einer Kategorie geht nicht mehr verloren, wenn die geöffnete Kategorie zwischenzeitlich zurückgesetzt wurde.
- Nach abgebrochenem Ziehen (Esc) oder Ablegen außerhalb werden Einfügestrich und Hervorhebung zuverlässig entfernt.
- Verschieben aus einer Kategorie ins Hauptdock bleibt bei einem Fehler konsistent (Rückgängig statt halb verschobenem Element).

- Eine fehlende oder beschädigte Icon-Datei bricht das Laden des Docks nicht mehr ab; einzelne fehlerhafte Einträge werden übersprungen, Lesefehler der Dock-Einstellungen abgefangen.
- Speicherleck behoben: Hover-Zustände hielten alle jemals erzeugten Dock-Buttons im Speicher.
- Ausblende-Timer läuft nach dem Ausblenden nicht mehr endlos alle 0,3 Sekunden weiter.

- Dock wird nach dem Programmstart zuverlässig ausgeblendet; bisher blieb es bis zum ersten Mauskontakt sichtbar.
- Standardelemente (Explorer, Eingabeaufforderung) werden beim allerersten Start gespeichert.
- Standard-Icons werden mit der Anwendung ausgeliefert und auch in installierten Versionen in den Icon-Ordner kopiert.
- Farben und Einblendverzögerung werden beim Start ohne unsichtbares Einstellungsfenster angewendet und nach dem Speichern vor dem Schließen übernommen.
- Autostart-Menüpunkt schreibt den Registry-Eintrag nur noch einmal; doppelte Event-Abonnements entfernt.
- Tippfehler im Kontextmenü („Dateipfad öffnen“), Fehler beim Start des Update-Dialogs und eine Inno-Setup-Warnung behoben.
- PNG-/ICO-Symbolauswahl, lokale Installerpfade und Swing-Konfiguration vervollständigt.

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
