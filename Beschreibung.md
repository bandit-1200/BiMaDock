# Release- und Versionsverwaltung

Dieses Dokument beschreibt den aktuellen Ablauf für das Veröffentlichen und Taggen einer neuen Version von BiMaDock.

## Wichtig

Die Versionierung wird in diesem Projekt über die Dateien `version.json` und `GitVersion.yml` gesteuert. Ein manueller Tag ist nur dann nötig, wenn ein Release bewusst auf GitHub veröffentlicht werden soll oder ein Build mit einer neuen Versionsnummer ausgelöst werden muss.

Bei jeder Änderung an Anwendung, Oberfläche, Tests oder Dokumentation ist die Version in `version.json` passend anzupassen. Sie folgt dem Schema `Jahr.Monat.Tag`; Nerdbank.GitVersioning ergänzt die Build-Nummer automatisch. Die `MyAppVersion`-Definitionen in `BiMaDock.iss` und `BiMaDock_local.iss` müssen mit der Basisversion synchron bleiben. Vor einem Release die Inno-Setup-Version mit dem passenden Skript aktualisieren.

Die Neuerungen jeder Version werden in [CHANGELOG.md](CHANGELOG.md) festgehalten. Neue Einträge kommen oben hinzu und gruppieren Änderungen nach Neuigkeiten, Verbesserungen und Fehlerbehebungen. Unveröffentlichte Änderungen sind entsprechend zu kennzeichnen.

Die Projektanweisung in [AGENTS.md](AGENTS.md) macht diese Regeln für Coding-Assistenten verbindlich. Der Workflow [versioning.yml](.github/workflows/versioning.yml) prüft bei Änderungen auf `dev` und `main` sowie in Pull Requests dorthin, dass die Version erhöht und Changelog sowie beide Installer-Versionen aktualisiert wurden. Der Release-Workflow [release.yml](.github/workflows/release.yml) prüft außerdem Tag, Version, datierten Changelog-Eintrag und dass der veröffentlichte Commit auf `main` liegt.

## Branch- und Freigaberegel

Die gesamte reguläre Entwicklung findet auf `dev` statt. `main` ist ausschließlich für freigegebene Releases vorgesehen. Ohne ausdrückliche Nutzeranweisung wird weder zu `main` gewechselt noch dorthin gemergt, ein Release-Tag erstellt oder veröffentlicht. Eine erfolgreiche CI-Prüfung ersetzt diese Freigabe nicht.

Nach ausdrücklicher Freigabe wird der geprüfte Stand von `dev` nach `main` übertragen. Vor der Veröffentlichung wird der Changelog-Eintrag von „In Arbeit“ auf das tatsächliche Datum im Format `YYYY-MM-DD` umgestellt. Der Tag muss exakt `v<VERSION>` entsprechen (zum Beispiel `v26.10.11`), wobei `<VERSION>` der Basisversion in `version.json` entspricht. Der Release-Workflow akzeptiert nur Tags dieses Formats und Commits, die auf `main` liegen.

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

Vor dem Wechsel zu `main` wird auf `dev` die Release-Version vorbereitet: Version in `version.json` erhöhen, beide Installer-Versionen synchronisieren und den Changelog-Eintrag unter dieser Version mit dem Veröffentlichungsdatum versehen („In Arbeit“ entfernen). Danach passende Tests ausführen und die Änderungen auf `dev` pushen. So bleiben Änderungen an `main` auf einen geprüften Merge beschränkt.

```powershell
# Release-Vorbereitung auf dev abschließen und pushen
git checkout dev
# Version aus version.json ablesen und den Changelog-Eintrag entsprechend datieren.
# Danach die synchronisierten Dateien prüfen und testen.
dotnet test BiMaDock.sln --configuration Release
git status
git add version.json BiMaDock.iss BiMaDock_local.iss CHANGELOG.md
git commit -m "chore: prepare release"
git push origin dev

# Erst nach ausdrücklicher Freigabe den geprüften Stand von dev nach main übertragen
git checkout main
git pull origin main
git merge --no-ff dev
git push origin main

$version = (Get-Content -Raw version.json | ConvertFrom-Json).version
git tag -a "v$version" -m "Release v$version"

# Release-Tag pushen; dadurch startet der Release-Workflow
git push origin "v$version"
```

## Build- und Release-Skripte

Im Projekt gibt es passende Skripte für die Versionierung und Veröffentlichung. Diese sollten bevorzugt verwendet werden, statt manuell unübersichtliche Tag- und Release-Schritte zu kombinieren.

Beispiele:

```bash
# PowerShell
.\release.ps1
.\autotag.ps1
.\update_version.ps1
```

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

## Alternativer Build-Workflow

```bash
# Beispiel für einen Release-ähnlichen Ablauf in diesem Projekt
.\release.ps1
```

> Das Dokument wurde auf den aktuellen Projekt-Workflow bereinigt. Veraltete manuelle Tag- und Merge-Anweisungen wurden entfernt, da sie nicht mehr zum realen Release-Prozess passen.
