# Wechseln zum Verzeichnis des Projekts (falls nötig, den Pfad anpassen)
Set-Location -Path "D:\a\BiMaDock\BiMaDock"

# Installieren Sie Nerdbank.GitVersioning CLI, falls noch nicht geschehen
dotnet tool install --global nbgv

# Abrufen der Versionsnummer aus Nerdbank.GitVersioning mit JSON-Format
$versionJson = nbgv get-version --format json
Write-Host "VersionJson: $versionJson"

# Konvertieren der JSON-Ausgabe in ein PowerShell-Objekt
$versionInfo = $versionJson | ConvertFrom-Json
$version = $versionInfo.SemVer2

# Debugging-Ausgabe zur Überprüfung der Versionsermittlung
if ($version -eq $null -or $version -eq "") {
    Write-Host "Fehler: Die abgerufene Versionsnummer ist leer."
} else {
    Write-Host "Ermittelte Version: $version"

    # Lesen Sie die Inno Setup-Datei
    $innoSetupFile = "D:\a\BiMaDock\BiMaDock\BiMaDock.iss"
    $content = Get-Content $innoSetupFile

    # Debugging-Ausgabe zur Überprüfung des Datei-Inhalts vor der Änderung
    Write-Host "Inhalt vor der Aenderung:`n$content"

    # Aktualisieren der Versionsnummer in der Inno Setup-Datei
    $updatedContent = $content -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$version`""

    # Debugging-Ausgabe zur Überprüfung des Datei-Inhalts nach der Änderung
    Write-Host ("Inhalt nach der Aenderung:`n" + $updatedContent)

    # Schreiben Sie die aktualisierte Datei zurück
    Set-Content $innoSetupFile -Value $updatedContent
}
