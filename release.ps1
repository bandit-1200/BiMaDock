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

$version = (Get-Content -Raw (Join-Path $projectRoot "version.json") | ConvertFrom-Json).version
$installerFiles = @("BiMaDock.iss", "BiMaDock_local.iss")
foreach ($installer in $installerFiles) {
    $installerPath = Join-Path $projectRoot $installer
    if (-not (Select-String -Path $installerPath -Pattern ('^#define MyAppVersion "' + [regex]::Escape($version) + '"$'))) {
        throw "$installer is not synchronized with version.json ($version)."
    }
}

dotnet test (Join-Path $projectRoot "BiMaDock.sln") --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $projectRoot "BiMaDock.csproj") --configuration $Configuration --runtime win-x64 --output (Join-Path $projectRoot "publish")
if ($LASTEXITCODE -ne 0) {
    throw "Publish build failed with exit code $LASTEXITCODE."
}

Write-Output "Local validation and publish build completed. No branch, commit, tag, or remote was changed."
