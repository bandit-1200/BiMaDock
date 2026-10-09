# Startet die FlaUI-UI-Tests. Während des Laufs werden Maus und Tastatur gesteuert –
# den PC in dieser Zeit nicht benutzen. Die Tests verwenden einen temporären Datenordner,
# die echten Benutzerdaten und der echte Autostart-Eintrag bleiben unberührt.
# Bewusst ohne $ErrorActionPreference = "Stop": dotnet test schreibt Testfehler nach stderr,
# was den Lauf in Windows PowerShell 5.1 sonst vorzeitig abbrechen würde.

$project = Join-Path $PSScriptRoot "tests\BiMaDock.UITests\BiMaDock.UITests.csproj"
$env:BIMADOCK_UI_TESTS = "1"
try {
    dotnet test $project --blame-hang-timeout 2m @args
    $exitCode = $LASTEXITCODE
}
finally {
    Remove-Item Env:\BIMADOCK_UI_TESTS -ErrorAction SilentlyContinue
    # Nach abgebrochenen Läufen verbliebene Testinstanzen beenden.
    Get-Process BiMaDock -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -like "*BiMaDock.UITests*" } |
        Stop-Process -ErrorAction SilentlyContinue
}

if ($exitCode -ne 0) {
    throw "UI-Tests fehlgeschlagen (Exit-Code $exitCode)."
}
