# Code-Audit BiMaDock – 2026-10-09

## Umsetzung

Die folgenden Punkte aus dem statischen Audit wurden auf `dev` umgesetzt. Ziel war, das vorhandene Verhalten zu erhalten und die dokumentierten Problemfälle abzusichern. Der aktuelle Quellcode ist in den angegebenen Dateien maßgeblich; die ursprünglichen Fundstellen können sich durch die Änderungen verschoben haben.

| # | Priorität | Befund | Umsetzung |
|---|---|---|---|
| 1 | Hoch | Release-Hilfsskript committete/pushte Änderungen und erzeugte inkompatible Build-Tags. | `release.ps1` ist jetzt ein lokaler Test-/Publish-Runner. Er prüft Branch und Versionskonsistenz und ändert weder Git-Index, Commit, Tag noch Remote. |
| 2 | Hoch | Autostart-Pfad war nicht quotiert. | `StartupManager` schreibt nun den ausführbaren Pfad in Anführungszeichen. Das Setzen des vorhandenen UI-Zustands schreibt damit keinen ungültigen Pfad mehr. |
| 3 | Mittel | „Öffnen“ für Kategorien lud eine leere Kategorie. | Der Kategorie-Stack erhält die ID des Dock-Elements als `Tag`, wie beim regulären Öffnen. |
| 4 | Mittel | Nicht gesetzte Farben konnten beim Speichern auf transparent zurückfallen. | Farbwähler werden mit den aktuellen Ressourcenwerten initialisiert; fehlende Auswahl verwendet denselben Ressourcenwert. Die bestehende Alpha-Untergrenze bleibt erhalten. |
| 5 | Mittel | Shortcut-Ziele verloren Argumente/Arbeitsverzeichnis oder konnten leer gespeichert werden. | Drops behalten die ursprüngliche `.lnk`- bzw. `.url`-Datei, die über die Windows-Shell geöffnet wird. Der nicht mehr benötigte Zielpfad-Parser wurde entfernt. |
| 6 | Mittel | Explorer-Auswahlargumente waren bei Leerzeichen nicht sicher getrennt. | `OpenFilePath_Click` übergibt den `/select,`-Wert als einzelnes `ProcessStartInfo.ArgumentList`-Argument. |
| 7 | Mittel | Icon-Rendering ließ HICONs, HBITMAPs und GDI-Bitmaps offen. | Gemeinsame Konvertierungs- und Platzhalterpfade disposen verwaltete Bitmaps und geben HICON/HBITMAP deterministisch frei. |
| 8 | Mittel | Beschädigte oder teilweise geschriebene Dock-Einstellungen konnten den Start stören. | Einstellungen werden zuerst in eine temporäre Datei geschrieben und dann ersetzt. Ungültiges JSON wird mit Zeitstempel gesichert, protokolliert und durch eine leere Liste ersetzt; sonstige I/O-Fehler bleiben sichtbar. |
| 9 | Mittel | `test.ps1` veröffentlichte mit Token ein GitHub-Release. | Das Skript führt jetzt ausschließlich `dotnet test` aus und reicht Filterargumente weiter. Es benötigt kein Token und ruft keine Release-API auf. |
| 10 | Niedrig | `.ico` wurde im Upload angeboten, aber nicht in der Symbolgalerie angezeigt. | Die Galerie zeigt PNG- und ICO-Dateien an. Der Upload-Dialog benennt PNG als unterstütztes Format und nimmt nur PNG entgegen. |
| 11 | Niedrig | Installer und lokales Setup verwendeten absolute Rechnerpfade. | Inno-Skripte lösen Quell- und Ausgabeordner relativ zum Skriptverzeichnis auf. Das lokale Setup publiziert projektrelativ und findet ISCC über PATH bzw. übliche Installationsordner. |
| 12 | Niedrig | Dokumentation verwies auf ein nicht vorhandenes `autotag.ps1`. | Nicht vorhandene Helper-Verweise wurden entfernt; die Build- und Testskripte sind dokumentiert. `update_version.ps1` arbeitet relativ zum Skriptverzeichnis und synchronisiert beide Installer. |
| 13 | Niedrig | Swing war auswählbar, aber nicht einstellbar. | Swing-Dauer und -Winkel sind über Slider einstellbar und werden in `StyleSettings.json` gespeichert. Die Standardwerte erhalten die bisherige 3,5-s-/30°-Animation. |

## Ergänzte Tests und Validierung

- `DockItemSettingsStoreTests`: Speichern/Ersetzen, Laden gültiger Daten und Sichern fehlerhafter JSON-Dateien.
- `StartupManagerTests`: Quotierung von Autostartpfaden mit und ohne Leerzeichen.
- Zusätzlich werden Testsuite, Release-Build/Publish und `git diff --check` als Abschlussvalidierung ausgeführt.
- Windows-Explorer-Integration, reale Registry-Anmeldung und das Erstellen eines Inno-Setup-Pakets bleiben manuelle Windows-Prüfungen. Ein echtes GitHub-Release wurde nicht ausgelöst.

## Status und Versionsregel

Alle im Audit aufgeführten Befunde sind adressiert. Der Umfang wurde versioniert als `26.10.13`; die Version in `version.json`, beiden Installerdateien und `CHANGELOG.md` muss synchron bleiben.
