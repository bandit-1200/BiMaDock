# Installiere Nerdbank.GitVersioning, falls noch nicht installiert
dotnet tool install --global nbgv

# Erhalte die Version mit Nerdbank.GitVersioning und wandle die Ausgabe in JSON um
$versionInfoJson = nbgv get-version --format json
$versionInfo = $versionInfoJson | ConvertFrom-Json

# Überprüfe, ob die VersionInfo korrekt ermittelt wurde
if ($versionInfo -and $versionInfo.Version -and $versionInfo.VersionHeight) {
    # Extrahiere die vollständige Version und die VersionHeight
    $fullVersion = $versionInfo.Version
    $versionHeight = $versionInfo.VersionHeight
    Write-Output "The full version is: $fullVersion"
    Write-Output "The build number is: $versionHeight"
    
    # Erstelle den Release-Tag im Format vX.Y.Z-BuildN
    $releaseTag = "v$fullVersion-Build$versionHeight"
    Write-Output "The release tag would be: $releaseTag"

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
        Write-Output "Debug: git commit -m 'Release $releaseTag Build $versionHeight'"
        git commit -m "Release $releaseTag Build $versionHeight"
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

    Write-Output "Debug: git tag $releaseTag"
    git tag $releaseTag
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to create tag $releaseTag"
        exit $LASTEXITCODE
    }

    Write-Output "Debug: git push origin $releaseTag"
    git push origin $releaseTag
    if ($LASTEXITCODE -ne 0) {
        Write-Output "Error: Failed to push tag $releaseTag"
        exit $LASTEXITCODE
    }

    # Erstelle ein neues GitHub-Release
    $releaseUrl = "https://api.github.com/repos/bandit-1200/BiMaDock/releases"
    $releaseData = @{
        tag_name = $releaseTag
        name = "Release $fullVersion Build $versionHeight"
        body = "Release notes for $releaseTag"
        draft = $false
        prerelease = $false
    } | ConvertTo-Json

    Write-Output "Debug: Creating GitHub release"
    Invoke-RestMethod -Uri $releaseUrl -Method Post -Headers @{ "Accept" = "application/vnd.github.v3+json" } -Body $releaseData -ContentType "application/json"
    
} else {
    Write-Output "Failed to retrieve the version information."
}
