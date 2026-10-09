# Release- und Versionsverwaltung

Dieses Dokument beschreibt den aktuellen Ablauf für das Veröffentlichen und Taggen einer neuen Version von BiMaDock.

## Wichtig

Die Versionierung wird in diesem Projekt über die Datei `version.json` (Nerdbank.GitVersioning) gesteuert. Ein manueller Tag ist nur dann nötig, wenn ein Release bewusst auf GitHub veröffentlicht werden soll oder ein Build mit einer neuen Versionsnummer ausgelöst werden muss.

Die Version folgt dem Schema `JJ.MM.N`: zweistelliges Jahr, Monat (1–12) und eine fortlaufende Release-Nummer, zum Beispiel `26.11.3`. Sie wird **nur für ein Release** gesetzt; Nerdbank.GitVersioning ergänzt für jeden Build automatisch eine eindeutige Build-Nummer. `version.json` ist die einzige Quelle der Version: `get_version.ps1` liest und prüft sie, und die Inno-Setup-Skripte erhalten sie beim Kompilieren über `ISCC /DMyAppVersion=<VERSION>`. Ohne diese Angabe baut Inno Setup einen als `0.0.0-dev` erkennbaren Entwicklungs-Installer.

Eine neue Release-Version muss größer sein als jede bisher getaggte Version. Die Update-Prüfung der App vergleicht Versionsnummern; ein kleineres oder gleiches Release würde bereits installierten Versionen nie angeboten. Achtung: Der historische Tag `v26.11.2-Build2` liegt über allen `26.10.x`-Versionen, das nächste Release muss daher mindestens `26.11.3` sein.

Die Neuerungen werden in [CHANGELOG.md](CHANGELOG.md) festgehalten. Jede Änderung kommt oben unter `## Unveröffentlicht` hinzu, gruppiert nach Neuigkeiten, Verbesserungen und Fehlerbehebungen. Beim Release wird dieser Abschnitt in `## <VERSION> – YYYY-MM-DD` umbenannt.

Beide Inno-Installer richten BiMaDock ausschließlich für den aktuellen Windows-Benutzer ein: Die Anwendung liegt standardmäßig unter `%LOCALAPPDATA%\Programs\BiMaDock`, benötigt keine Administratorrechte und verwendet benutzerbezogene Startmenü-, Desktop-, Autostart- und Icon-Pfade.

Die Projektanweisung in [AGENTS.md](AGENTS.md) macht diese Regeln für Coding-Assistenten verbindlich. Der Workflow [versioning.yml](.github/workflows/versioning.yml) prüft bei Änderungen auf `dev` und `main` sowie in Pull Requests dorthin, dass der Changelog ergänzt wurde, `version.json` ein gültiges `JJ.MM.N` enthält und die Version nicht kleiner wird. Der Release-Workflow [release.yml](.github/workflows/release.yml) prüft außerdem Tag, Version, datierten Changelog-Eintrag ohne verbleibenden Abschnitt „Unveröffentlicht“, dass die Version größer als alle bisherigen Tags ist und dass der veröffentlichte Commit auf `main` liegt.

## Branch- und Freigaberegel

Die gesamte reguläre Entwicklung findet auf `dev` statt. `main` ist ausschließlich für freigegebene Releases vorgesehen. Ohne ausdrückliche Nutzeranweisung wird weder zu `main` gewechselt noch dorthin gemergt, ein Release-Tag erstellt oder veröffentlicht. Eine erfolgreiche CI-Prüfung ersetzt diese Freigabe nicht.

Nach ausdrücklicher Freigabe wird der geprüfte Stand von `dev` nach `main` übertragen. Vor der Veröffentlichung wird der Abschnitt „Unveröffentlicht“ in `## <VERSION> – YYYY-MM-DD` umbenannt. Der Tag muss exakt `v<VERSION>` entsprechen (zum Beispiel `v26.11.3`), wobei `<VERSION>` der Basisversion in `version.json` entspricht. Der Release-Workflow akzeptiert nur Tags dieses Formats und Commits, die auf `main` liegen.

## Standard-Workflow für die Entwicklung

```bash
# Reguläre Entwicklung ausschließlich auf dev
git checkout dev

# Aktuellen Entwicklungsstand holen
git pull origin dev

# Änderungen prüfen und committen
git status
git add .
git commit -m "feat: Beschreibung des Releases"

# Entwicklungsstand veröffentlichen
git push origin dev
```

## Release nach ausdrücklicher Nutzerfreigabe

Vor dem Wechsel zu `main` wird auf `dev` die Release-Version vorbereitet: Version in `version.json` setzen (größer als alle bisherigen Tags) und `## Unveröffentlicht` im Changelog in `## <VERSION> – YYYY-MM-DD` umbenennen. Danach passende Tests ausführen und die Änderungen auf `dev` pushen. So bleiben Änderungen an `main` auf einen geprüften Merge beschränkt.

```powershell
# Release-Vorbereitung auf dev abschließen und pushen
git checkout dev
# Version in version.json setzen und "## Unveröffentlicht" im Changelog datieren.
dotnet test BiMaDock.sln --configuration Release
git status
git add version.json CHANGELOG.md
git commit -m "chore: prepare release"
git push origin dev

# Erst nach ausdrücklicher Freigabe den geprüften Stand von dev nach main übertragen
git checkout main
git pull origin main
git merge --no-ff dev
git push origin main

$version = .\get_version.ps1
git tag -a "v$version" -m "Release v$version"

# Release-Tag pushen; dadurch startet der Release-Workflow
git push origin "v$version"
```

## Build-Skripte

`release.ps1` führt auf `dev` Tests und einen lokalen Publish-Build aus. Es wechselt keine Branches, erzeugt keine Tags, committet und pusht keine Änderungen. `test.ps1` startet die Testsuite; zusätzliche Argumente werden an `dotnet test` weitergereicht. Veröffentlichungen erfolgen ausschließlich über den freigegebenen `main`-Releaseprozess.

`get_version.ps1` liest die Version aus `version.json` und prüft das Format. `BiMaDock_local_setup.bat` und der Release-Workflow übergeben sie an Inno Setup; eine Synchronisation der `.iss`-Dateien ist nicht mehr nötig. `ui-test.ps1` startet die FlaUI-UI-Tests.

## Hinweise

- Tags sollten nur für reale Releases verwendet werden.
- Das manuelle Löschen von Tags ist nur in Ausnahmefällen sinnvoll.
- Für normale Veröffentlichungen ist die automatisierte Versionierungslogik besser als Handarbeit.

## Beispiel: alle lokalen Tags entfernen

```bash
git tag -l | ForEach-Object { git tag -d $_ }
```

## Beispiel: alle Remote-Tags entfernen

```bash
git tag -l | ForEach-Object { git push origin --delete $_ }
```

> Diese Befehle sind nur für Notfälle oder bei Problemen mit veralteten Tags gedacht und sollten nicht im normalen Release-Prozess verwendet werden.

> Das Dokument wurde auf den aktuellen Projekt-Workflow bereinigt. Veraltete manuelle Tag- und Merge-Anweisungen wurden entfernt, da sie nicht mehr zum realen Release-Prozess passen.
