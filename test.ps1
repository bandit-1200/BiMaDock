
$ErrorActionPreference = "Stop"

dotnet test (Join-Path $PSScriptRoot "BiMaDock.sln") @args
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
}
