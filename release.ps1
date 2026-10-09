param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot

$branch = git -C $projectRoot branch --show-current
if ($LASTEXITCODE -ne 0) {
    throw "Could not determine the current Git branch."
}
if ($branch -ne "dev") {
    throw "This local validation script is restricted to the dev branch; current branch is '$branch'."
}

# Prüft zugleich das Versionsformat; die Installer erhalten die Version erst beim Kompilieren.
$version = & (Join-Path $projectRoot "get_version.ps1")
Write-Output "Version: $version"

dotnet test (Join-Path $projectRoot "BiMaDock.sln") --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $projectRoot "BiMaDock.csproj") --configuration $Configuration --runtime win-x64 -p:PublishReadyToRun=true --output (Join-Path $projectRoot "publish")
if ($LASTEXITCODE -ne 0) {
    throw "Publish build failed with exit code $LASTEXITCODE."
}

Write-Output "Local validation and publish build completed. No branch, commit, tag, or remote was changed."
