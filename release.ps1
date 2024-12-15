# Installiere Nerdbank.GitVersioning, falls noch nicht installiert
dotnet tool install --global nbgv

# Erhalte die Version mit Nerdbank.GitVersioning und wandle die Ausgabe in JSON um
$versionInfoJson = nbgv get-version --format json
$versionInfo = $versionInfoJson | ConvertFrom-Json

# Überprüfe, ob die VersionInfo korrekt ermittelt wurde
if ($versionInfo -and $versionInfo.SimpleVersion -and $versionInfo.VersionHeight) {
    # Extrahiere die SimpleVersion und die VersionHeight
    $simpleVersion = $versionInfo.SimpleVersion
    $versionHeight = $versionInfo.VersionHeight
    Write-Output "The simple version is: $simpleVersion"
    Write-Output "The build number is: $versionHeight"
    
    # Erstelle den Tag-Namen inklusive Build-Nummer
    $tagName = "v$simpleVersion-Build$versionHeight"
    Write-Output "The tag name would be: $tagName"

    # Git-Befehle ausführen mit klaren Debug-Nachrichten
    Write-Output "Debug: git checkout dev"
    git checkout dev
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to checkout dev branch"
        exit $LASTEXITCODE
    }

    Write-Output "Debug: git add ."
    git add .
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to add changes"
        exit $LASTEXITCODE
    }

    # Überprüfe, ob es Änderungen gibt, die committed werden können
    $changes = git status --porcelain
    if ($changes) {
        Write-Output "Debug: git commit -m 'Release $tagName Build $versionHeight'"
        git commit -m "Release $tagName Build $versionHeight"
        if ($LASTEXITCODE -ne 0) {
            Write-Output "Error: Failed to commit changes"
            exit $LASTEXITCODE
        }

        Write-Output "Debug: git push origin dev"
        git push origin dev
        if ($LASTEXITCODE -ne 0) {
            Write-Output "Error: Failed to push to dev branch"
            exit $LASTEXITCODE
        }
    } else {
        Write-Output "No changes to commit."
    }

    Write-Output "Debug: git tag $tagName"
    git tag $tagName
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to create tag $tagName"
        exit $LASTEXITCODE
    }

    Write-Output "Debug: git push origin $tagName"
    git push origin $tagName
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to push tag $tagName"
        exit $LASTEXITCODE
    }

} else {
    Write-Output "Failed to retrieve the version information."
}
