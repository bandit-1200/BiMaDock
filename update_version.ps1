# Installieren Sie Nerdbank.GitVersioning CLI, falls noch nicht geschehen
dotnet tool install --global nbgv

# Abrufen der Versionsnummer aus Nerdbank.GitVersioning
$versionInfo = nbgv get-version
$version = $versionInfo.SemVer2

# Lesen Sie die Inno Setup-Datei
$innoSetupFile = "D:\a\BiMaDock\BiMaDock\BiMaDock.iss"
$content = Get-Content $innoSetupFile

# Aktualisieren der Versionsnummer in der Inno Setup-Datei
$updatedContent = $content -replace "#define MyAppVersion .*", "#define MyAppVersion `"$version`""

# Schreiben Sie die aktualisierte Datei zurück
Set-Content $innoSetupFile -Value $updatedContent
