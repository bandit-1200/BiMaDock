# Projektregeln

- Entwicklung erfolgt standardmäßig ausschließlich auf `dev`. Nicht eigenständig zu `main` wechseln, dort Änderungen einbringen, mergen, taggen oder veröffentlichen; dafür ist zuerst eine ausdrückliche Nutzeranweisung erforderlich.
- Bei jeder Änderung an diesem Repository die Version in `version.json` erhöhen und einen passenden Eintrag oben in `CHANGELOG.md` ergänzen.
- Die Versionsnummer folgt dem Schema `Jahr.Monat.Tag`; die `MyAppVersion`-Definitionen in `BiMaDock.iss` und `BiMaDock_local.iss` müssen exakt mit `version.json` übereinstimmen.
- Den Changelog-Eintrag unter der neuen Versionsnummer führen und unveröffentlichte Änderungen als „In Arbeit“ kennzeichnen.
- Für ein ausdrücklich beauftragtes Release den Changelog-Eintrag mit dem Veröffentlichungsdatum versehen und „In Arbeit“ entfernen. Der Release-Tag muss `v<VERSION>` entsprechen, zum Beispiel `v26.10.11`; veröffentlicht wird ausschließlich ein Commit, der auf `main` liegt.
- Vor Abschluss `git diff --check` ausführen und passende Builds oder Tests für Codeänderungen laufen lassen.
- Details zum Release-Prozess stehen in [Beschreibung.md](Beschreibung.md).
