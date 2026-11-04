# Veröffentlicht eine Version: prüfen, Tag setzen, nach Gitea und GitHub pushen.
# Den Installer baut GitHub (.github/workflows/release.yml) und legt das Release mit dem Setup an.
# Vorher: Version in Directory.Build.props erhöhen und in CHANGELOG.md einen Abschnitt "## <Version>" anlegen.
# Aufruf aus dem Repo-Stamm: .\tools\release.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

[xml]$props = Get-Content 'Directory.Build.props'
$version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
$tag = "v$version"

if (git status --porcelain) { throw 'Es gibt nicht committete Änderungen.' }
if ((git branch --show-current) -ne 'main') { throw 'Releases nur von main.' }
if (git tag --list $tag) { throw "Tag $tag gibt es schon. Version in Directory.Build.props erhöhen." }

$changelog = Get-Content 'CHANGELOG.md' -Raw -Encoding utf8
if ($changelog -notmatch "(?m)^## $([regex]::Escape($version))\b") { throw "In CHANGELOG.md fehlt der Abschnitt '## $version'." }

Write-Host "Tests ..."
dotnet test (Join-Path $root 'BiMaDock2.sln') -c Release --nologo -v q
if ($LASTEXITCODE) { throw 'Tests fehlgeschlagen.' }

git tag -a $tag -m "BiMaDock $version"
git push origin main $tag
git -c core.sshCommand=ssh push github main $tag   # GitHub mit dem normalen SSH-Schlüssel, nicht dem Gitea-Deploy-Key
Write-Host "Tag $tag gepusht. GitHub baut jetzt den Installer:"
Write-Host "  https://github.com/bandit-1200/BiMaDock/actions"
Write-Host "  danach: https://github.com/bandit-1200/BiMaDock/releases/tag/$tag"
