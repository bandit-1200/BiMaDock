@echo off
setlocal enabledelayedexpansion

REM Zum main-Branch wechseln und mit dev mergen
git checkout main
git merge dev

REM Holen der neuesten Tag-Version
for /f "delims=" %%i in ('git tag --sort=-creatordate') do (
    if not defined latest_tag set latest_tag=%%i
)

REM Extrahieren der Versionsnummer
set "version_number=%latest_tag:~1%"

REM Versionsnummer erhoehen
for /f "tokens=1,2,3 delims=." %%a in ("%version_number%") do (
    set /a major=%%a
    set /a minor=%%b
    set /a patch=%%c+1
)
set new_version=v%major%.%minor%.%patch%

REM Aenderungen zum main-Branch committen und pushen
git add .
git commit -m "Increment version number to %new_version%"
git push origin main

REM Neuen Tag erstellen und pushen
git tag -a %new_version% -m "Release %new_version%"
git push origin %new_version%

REM Zurueck zum dev-Branch wechseln
git checkout dev

echo New tag created and pushed: %new_version%
endlocal
