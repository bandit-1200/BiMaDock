# Baut den Installer: Tests, Veröffentlichung (self-contained, x64) und Inno Setup.
# Aufruf aus dem Repo-Stamm: .\installer\build.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root 'artifacts\publish'

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 nicht gefunden (winget install JRSoftware.InnoSetup).' }

Write-Host "Tests ..."
dotnet test (Join-Path $root 'BiMaDock2.sln') -c Release --nologo -v q
if ($LASTEXITCODE) { throw 'Tests fehlgeschlagen.' }

Write-Host "Veröffentlichen $version ..."
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $root 'src\BiMaDock\BiMaDock.csproj') -c Release -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true -o $publish --nologo -v q
if ($LASTEXITCODE) { throw 'Veröffentlichen fehlgeschlagen.' }

$icons = (Get-ChildItem (Join-Path $publish 'Icons') -Recurse -File).Count
if ($icons -eq 0) { throw 'Symbolbibliothek fehlt im Veröffentlichungsordner.' }
Write-Host "$icons Symbole im Paket."

Write-Host "Installer ..."
& $iscc "/DAppVersion=$version" "/DPublishDir=$publish" (Join-Path $PSScriptRoot 'BiMaDock2.iss')
if ($LASTEXITCODE) { throw 'Inno Setup fehlgeschlagen.' }
Get-ChildItem (Join-Path $root 'artifacts\installer') -Filter *.exe | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
