# Release- und Versionsverwaltung

Dieses Dokument beschreibt den aktuellen Ablauf für das Veröffentlichen und Taggen einer neuen Version von BiMaDock.

## Wichtig

Die Versionierung wird in diesem Projekt über die Dateien `version.json` und `GitVersion.yml` gesteuert. Ein manueller Tag ist nur dann nötig, wenn ein Release bewusst auf GitHub veröffentlicht werden soll oder ein Build mit einer neuen Versionsnummer ausgelöst werden muss.

## Standard-Workflow für ein Release

```bash
# 1) Auf den richtigen Branch wechseln
git checkout dev

# 2) Aktuellen Stand holen
git pull origin dev

# 3) Änderungen prüfen und ggf. committen
git status
git add .
git commit -m "feat: Beschreibung des Releases"

# 4) Änderungen auf GitHub pushen
git push origin dev
```

## Manuelles Taggen einer Version

```bash
# Aktuellste Tags anzeigen
git tag --sort=-creatordate

# Neuen Tag anlegen
git tag -a v1.2.3 -m "Release v1.2.3"

# Tag zu GitHub pushen
git push origin v1.2.3
```

## Release auf main übertragen

Wenn der Stand auf `dev` stabil ist und auf `main` veröffentlicht werden soll:

```bash
git checkout main
git pull origin main
git merge dev
git push origin main
```

Danach kann das Release mit einem Tag und dem GitHub-Release-Prozess abgeschlossen werden.

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
