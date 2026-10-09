$ErrorActionPreference = "Stop"

$versionPath = Join-Path $PSScriptRoot "version.json"
$version = [string] (Get-Content -Raw $versionPath | ConvertFrom-Json).version
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "version.json must contain a three-part version in Jahr.Monat.Tag format; found '$version'."
}

foreach ($installer in @("BiMaDock.iss", "BiMaDock_local.iss")) {
    $installerPath = Join-Path $PSScriptRoot $installer
    $content = Get-Content -Raw $installerPath
    $pattern = '(?m)^#define MyAppVersion "[^"]*"$'
    if ($content -notmatch $pattern) {
        throw "Version definition was not found in $installer."
    }

    $updatedContent = [regex]::Replace(
        $content,
        $pattern,
        ('#define MyAppVersion "' + $version + '"'))
    [System.IO.File]::WriteAllText(
        $installerPath,
        $updatedContent,
        [System.Text.UTF8Encoding]::new($false))
}

Write-Output "Installer scripts synchronized with version.json ($version)."
