param(
    [string] $BaseSha,

    [string] $HeadSha,

    [string] $ReleaseTag
)

$ErrorActionPreference = "Stop"

# Versionsschema JJ.MM.N: zweistelliges Jahr, Monat (1-12), fortlaufende Release-Nummer.
# Die Version in version.json wird nur für ein Release erhöht; bis dahin werden Änderungen
# im Changelog unter "## Unveröffentlicht" gesammelt. Die Installer erhalten die Version
# beim Kompilieren über ISCC /DMyAppVersion.
$versionPattern = '^\d{2}\.(?:[1-9]|1[0-2])\.\d+$'

function Get-VersionParts([string] $text) {
    # Akzeptiert auch historische Tags wie "V26.10.07-Build3" oder "v24.12.11-Build9".
    if ($text -match '^[vV]?(\d+)\.(\d+)\.(\d+)') {
        return @([int] $Matches[1], [int] $Matches[2], [int] $Matches[3])
    }
    return $null
}

function Compare-Version([int[]] $left, [int[]] $right) {
    for ($index = 0; $index -lt 3; $index++) {
        if ($left[$index] -ne $right[$index]) {
            return $left[$index].CompareTo($right[$index])
        }
    }
    return 0
}

$versionData = Get-Content -Raw "version.json" | ConvertFrom-Json
$version = [string] $versionData.version
if ($version -notmatch $versionPattern) {
    throw "version.json must contain a version in JJ.MM.N format (e.g. 26.10.3); found '$version'."
}
$versionParts = Get-VersionParts $version

if (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    if ($ReleaseTag -cne "v$version") {
        throw "Release tag '$ReleaseTag' must exactly match version.json as 'v$version'."
    }

    $changelog = Get-Content -Raw -Encoding UTF8 "CHANGELOG.md"
    $releaseHeading = '(?m)^##\s+' + [regex]::Escape($version) + '\s+(?:-|' + [char]0x2013 + ')\s+\d{4}-\d{2}-\d{2}\s*$'
    if ($changelog -notmatch $releaseHeading) {
        throw "CHANGELOG.md must have a dated release heading '## $version - YYYY-MM-DD' (hyphen or en dash)."
    }
    if ($changelog -match ('(?m)^##\s+Unver' + [char]0x00F6 + 'ffentlicht\s*$')) {
        throw "CHANGELOG.md still contains the unreleased section; move its entries under the release heading."
    }

    # Die Update-Prüfung der App vergleicht Versionsnummern. Ein Release, das nicht größer als alle
    # bisherigen ist, würde installierten Versionen nie als Update angeboten.
    $previousTags = @(git tag --list | Where-Object { $_ -cne $ReleaseTag })
    foreach ($tag in $previousTags) {
        $tagParts = Get-VersionParts $tag
        if ($null -ne $tagParts -and (Compare-Version $versionParts $tagParts) -le 0) {
            throw "Version '$version' must be greater than every previous release; tag '$tag' is equal or newer."
        }
    }

    Write-Output "Release tag '$ReleaseTag', version '$version' and dated changelog entry are consistent; version is newer than all previous tags."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($BaseSha) -or [string]::IsNullOrWhiteSpace($HeadSha)) {
    throw "BaseSha and HeadSha are required when validating a development change."
}

$changedFiles = @(git diff --name-only $BaseSha $HeadSha)
if ($LASTEXITCODE -ne 0) {
    throw "Could not determine changed files between $BaseSha and $HeadSha."
}

if ($changedFiles.Count -eq 0) {
    Write-Output "No changed files; versioning check is not required."
    exit 0
}

if ("CHANGELOG.md" -notin $changedFiles) {
    throw "Every repository change must be described in CHANGELOG.md (under the unreleased section heading)."
}

$baseVersionJson = git show "${BaseSha}:version.json"
if ($LASTEXITCODE -ne 0) {
    throw "Could not read version.json from base commit $BaseSha."
}
$baseVersion = [string] ($baseVersionJson | ConvertFrom-Json).version
$baseParts = Get-VersionParts $baseVersion
if ($null -ne $baseParts -and (Compare-Version $versionParts $baseParts) -lt 0) {
    throw "Version '$version' must not be lower than base version '$baseVersion'."
}

Write-Output "Changelog updated; version '$version' is valid."
