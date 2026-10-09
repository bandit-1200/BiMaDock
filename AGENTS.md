# Projektregeln

- Entwicklung erfolgt standardmäßig ausschließlich auf `dev`. Nicht eigenständig zu `main` wechseln, dort Änderungen einbringen, mergen, taggen oder veröffentlichen; dafür ist zuerst eine ausdrückliche Nutzeranweisung erforderlich.
- Jede Änderung an diesem Repository oben in `CHANGELOG.md` unter `## Unveröffentlicht` eintragen (Abschnitt anlegen, falls er fehlt), gruppiert nach Neuigkeiten, Verbesserungen und Fehlerbehebungen. Die Version in `version.json` wird dabei nicht erhöht.
- Die Versionsnummer folgt dem Schema `JJ.MM.N`: zweistelliges Jahr, Monat (1–12) und eine fortlaufende Release-Nummer, zum Beispiel `26.11.3`. `version.json` ist die einzige Quelle; die Installer erhalten die Version beim Kompilieren über `ISCC /DMyAppVersion=<VERSION>` (siehe `get_version.ps1`).
- Erst für ein ausdrücklich beauftragtes Release die Version in `version.json` setzen und `## Unveröffentlicht` in `## <VERSION> – YYYY-MM-DD` umbenennen. Die Version muss größer sein als jede bisher getaggte Version, sonst bietet die Update-Prüfung sie installierten Versionen nicht an. Der Release-Tag muss `v<VERSION>` entsprechen, zum Beispiel `v26.11.3`; veröffentlicht wird ausschließlich ein Commit, der auf `main` liegt.
- `README.md` aktualisieren, wenn sich für Nutzer sichtbare Funktionen, Voraussetzungen oder Build- und Testbefehle ändern; rein interne Änderungen brauchen keinen README-Eintrag.
- Vor Abschluss `git diff --check` ausführen und passende Builds oder Tests für Codeänderungen laufen lassen.
- Details zum Release-Prozess stehen in [Beschreibung.md](Beschreibung.md).
