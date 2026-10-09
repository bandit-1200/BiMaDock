param(
    [string] $BaseSha,

    [string] $HeadSha,

    [string] $ReleaseTag
)

$ErrorActionPreference = "Stop"

if (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $versionData = Get-Content -Raw "version.json" | ConvertFrom-Json
    $version = [string] $versionData.version
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "version.json must contain a three-part version in Jahr.Monat.Tag format; found '$version'."
    }

    if ($ReleaseTag -cne "v$version") {
        throw "Release tag '$ReleaseTag' must exactly match version.json as 'v$version'."
    }

    foreach ($installer in @("BiMaDock.iss", "BiMaDock_local.iss")) {
        $installerContent = Get-Content -Raw $installer
        if ($installerContent -notmatch ('(?m)^#define MyAppVersion "' + [regex]::Escape($version) + '"\s*$')) {
            throw "$installer must define MyAppVersion as '$version'."
        }
    }

    $changelog = Get-Content -Raw "CHANGELOG.md"
    $releaseHeading = '(?m)^##\s+' + [regex]::Escape($version) + '\s+(?:-|\u2013)\s+\d{4}-\d{2}-\d{2}\s*$'
    if ($changelog -notmatch $releaseHeading) {
        throw "CHANGELOG.md must have a dated release heading for '$version' in YYYY-MM-DD format, without an 'In Arbeit' status."
    }

    Write-Output "Release tag '$ReleaseTag', version '$version', installer versions, and dated changelog entry are consistent."
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

$requiredFiles = @(
    "version.json",
    "CHANGELOG.md",
    "BiMaDock.iss",
    "BiMaDock_local.iss"
)
$missingFiles = @($requiredFiles | Where-Object { $_ -notin $changedFiles })
if ($missingFiles.Count -gt 0) {
    throw "Every repository change must update version.json, CHANGELOG.md, BiMaDock.iss, and BiMaDock_local.iss. Missing changes: $($missingFiles -join ', ')"
}

$versionData = Get-Content -Raw "version.json" | ConvertFrom-Json
$version = [string] $versionData.version
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "version.json must contain a three-part version in Jahr.Monat.Tag format; found '$version'."
}

foreach ($installer in @("BiMaDock.iss", "BiMaDock_local.iss")) {
    $installerContent = Get-Content -Raw $installer
    if ($installerContent -notmatch ('(?m)^#define MyAppVersion "' + [regex]::Escape($version) + '"\s*$')) {
        throw "$installer must define MyAppVersion as '$version'."
    }
}

$changelog = Get-Content -Raw "CHANGELOG.md"
$versionHeading = '(?m)^##\s+' + [regex]::Escape($version) + '(\s|$)'
if ($changelog -notmatch $versionHeading) {
    throw "CHANGELOG.md must have a heading for version '$version'."
}

$baseVersionJson = git show "${BaseSha}:version.json"
if ($LASTEXITCODE -ne 0) {
    throw "Could not read version.json from base commit $BaseSha."
}
$baseVersion = ([string] ($baseVersionJson | ConvertFrom-Json).version) -split '\.'
$currentVersion = $version -split '\.'
for ($index = 0; $index -lt $currentVersion.Count; $index++) {
    $basePart = [int] $baseVersion[$index]
    $currentPart = [int] $currentVersion[$index]
    if ($currentPart -gt $basePart) {
        Write-Output "Version increased from $($baseVersion -join '.') to $version."
        exit 0
    }
    if ($currentPart -lt $basePart) {
        throw "Version '$version' must be greater than base version '$($baseVersion -join '.')'."
    }
}

throw "Version '$version' must be greater than base version '$($baseVersion -join '.')'."
