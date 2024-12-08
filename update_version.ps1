# Wechseln zum Verzeichnis des Projekts (falls nötig, den Pfad anpassen)
Set-Location -Path "D:\a\BiMaDock\BiMaDock"

# Debugging-Ausgaben aktivieren
Write-Host "Debug: Start des Skripts zum Aktualisieren der Versionsnummer"

# Version aus version.json auslesen
$versionJson = Get-Content -Raw -Path ".\version.json" | ConvertFrom-Json
$version = $versionJson.version
Write-Host "Debug: Versionsnummer ist $version"

# Lesen Sie die Inno Setup-Datei
$innoSetupFile = "D:\a\BiMaDock\BiMaDock\BiMaDock.iss"
$content = Get-Content $innoSetupFile -Raw

# Debugging-Ausgabe des Datei-Inhalts vor der Änderung
Write-Host "Inhalt vor der Änderung:`n$content"

# Sicherstellen, dass MyAppVersion definiert ist
if ($content -match '#define MyAppVersion') {
    $updatedContent = $content -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$version`""
    Write-Host "Debug: MyAppVersion gefunden und ersetzt"
} else {
    $updatedContent = "#define MyAppVersion `"$version`"" + [Environment]::NewLine + $content
    Write-Host "Debug: MyAppVersion nicht gefunden, Definition hinzugefügt"
}

# Debugging-Ausgabe des Datei-Inhalts nach der Änderung
Write-Host "Inhalt nach der Änderung:`n$updatedContent"

# Schreiben Sie die aktualisierte Datei zurück
Set-Content $innoSetupFile -Value $updatedContent

# Debugging-Ausgabe zur Bestätigung, dass die Datei geschrieben wurde
Write-Host "Debug: Die Datei wurde erfolgreich aktualisiert"
