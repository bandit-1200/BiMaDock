# Veröffentlicht eine Version: Installer bauen, Tag setzen, nach Gitea und GitHub pushen, GitHub-Release mit Installer anlegen.
# Vorher: Version in Directory.Build.props erhöhen und in CHANGELOG.md einen Abschnitt "## <Version>" anlegen.
# Aufruf aus dem Repo-Stamm: .\tools\release.ps1        (braucht: gh auth login)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

[xml]$props = Get-Content 'Directory.Build.props'
$version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
$tag = "v$version"

if (git status --porcelain) { throw 'Es gibt nicht committete Änderungen.' }
if ((git branch --show-current) -ne 'main') { throw 'Releases nur von main.' }
if (git tag --list $tag) { throw "Tag $tag gibt es schon. Version in Directory.Build.props erhöhen." }
gh auth status *> $null
if ($LASTEXITCODE) { throw 'GitHub CLI nicht angemeldet: gh auth login' }

# Release-Notizen aus dem passenden CHANGELOG-Abschnitt
$changelog = Get-Content 'CHANGELOG.md' -Raw -Encoding utf8
$match = [regex]::Match($changelog, "(?ms)^## $([regex]::Escape($version))\b[^\n]*\n(.*?)(?=^## |\z)")
if (-not $match.Success) { throw "In CHANGELOG.md fehlt der Abschnitt '## $version'." }
$notesFile = Join-Path ([IO.Path]::GetTempPath()) "bimadock-notes-$version.md"
$match.Groups[1].Value.Trim() | Set-Content $notesFile -Encoding utf8

& (Join-Path $root 'installer\build.ps1')
$setup = Join-Path $root "artifacts\installer\BiMaDock2-Setup-$version.exe"
if (-not (Test-Path $setup)) { throw "Installer fehlt: $setup" }

git tag -a $tag -m "BiMaDock $version"
git push origin main $tag
git push github main $tag
gh release create $tag $setup --repo bandit-1200/BiMaDock --title "BiMaDock $version" --notes-file $notesFile --latest
Write-Host "Fertig: https://github.com/bandit-1200/BiMaDock/releases/tag/$tag"
