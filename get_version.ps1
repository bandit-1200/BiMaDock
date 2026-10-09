$ErrorActionPreference = "Stop"

# Einzige Quelle der Versionsnummer ist version.json. Schema JJ.MM.N:
# zweistelliges Jahr, Monat (1-12) und eine fortlaufende Nummer des Releases.
# Die Installer erhalten die Version beim Kompilieren über ISCC /DMyAppVersion=<Version>.

$versionPath = Join-Path $PSScriptRoot "version.json"
$version = [string] (Get-Content -Raw $versionPath | ConvertFrom-Json).version
if ($version -notmatch '^\d{2}\.(?:[1-9]|1[0-2])\.\d+$') {
    throw "version.json must contain a version in JJ.MM.N format (e.g. 26.10.3); found '$version'."
}

Write-Output $version
