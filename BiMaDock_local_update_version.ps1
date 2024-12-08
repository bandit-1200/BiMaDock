# Installieren Sie Nerdbank.GitVersioning CLI, falls noch nicht geschehen
dotnet tool install --global nbgv

# Debugging-Ausgabe zur Überprüfung der Git-Konfiguration
git status

# Abrufen der Versionsnummer aus Nerdbank.GitVersioning
$versionInfo = nbgv get-version

# Debugging-Ausgabe zur Überprüfung der Versionsermittlung
if ($versionInfo -eq $null) {
    Write-Host "Fehler: Konnte Versionsinformationen nicht abrufen."
} else {
    $version = $versionInfo.SemVer2
    if ($version -eq $null -or $version -eq "") {
        Write-Host "Fehler: Die abgerufene Versionsnummer ist leer."
    } else {
        Write-Host "Ermittelte Version: $version"

        # Lesen Sie die Inno Setup-Datei
        $innoSetupFile = "C:\Users\Marco\Documents\VisualStudioCode\BiMaDock\BiMaDock_local.iss"
        $content = Get-Content $innoSetupFile

        # Debugging-Ausgabe zur Überprüfung des Datei-Inhalts vor der Änderung
        Write-Host "Inhalt vor der Aenderung:`n$content"

        # Überprüfen, ob die #define MyAppVersion-Zeile existiert
        if ($content -notcontains '#define MyAppVersion') {
            $content = "#define MyAppVersion `"$version`"" + [Environment]::NewLine + $content
        } else {
            # Aktualisieren der Versionsnummer in der Inno Setup-Datei
            $content = $content -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$version`""
        }

        # Debugging-Ausgabe zur Überprüfung des Datei-Inhalts nach der Änderung
        Write-Host ("Inhalt nach der Aenderung:`n" + $content)

        # Schreiben Sie die aktualisierte Datei zurück
        Set-Content $innoSetupFile -Value $content
    }
}
