$ErrorActionPreference = "Stop"

if (-not (Get-Command nbgv -ErrorAction SilentlyContinue)) {
    dotnet tool install --global nbgv
    if ($LASTEXITCODE -ne 0) {
        throw "Nerdbank.GitVersioning konnte nicht installiert werden."
    }

    $env:PATH = "$(Join-Path $env:USERPROFILE '.dotnet\tools');$env:PATH"
}

$versionJson = (& nbgv get-version --format json) -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0) {
    throw "Die Version konnte mit Nerdbank.GitVersioning nicht ermittelt werden."
}

$versionInfo = $versionJson | ConvertFrom-Json
$version = $versionInfo.SemVer2
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Nerdbank.GitVersioning hat keine gültige SemVer2-Version geliefert."
}

$innoSetupFile = Join-Path $PSScriptRoot "BiMaDock_local.iss"
$content = Get-Content -Raw -Path $innoSetupFile
$versionPattern = '(?m)(^#define MyAppVersion ")[^"]*(")'
if ($content -notmatch $versionPattern) {
    throw "Die Versionsdefinition wurde in BiMaDock_local.iss nicht gefunden."
}

$updatedContent = [regex]::Replace(
    $content,
    $versionPattern,
    ('${1}' + $version + '${2}'))

if ($updatedContent -ne $content) {
    [System.IO.File]::WriteAllText(
        $innoSetupFile,
        $updatedContent,
        [System.Text.UTF8Encoding]::new($false))
}

Write-Host "BiMaDock_local.iss ist auf Version $version synchronisiert."
