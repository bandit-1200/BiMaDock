# BiMaDock 2

Neuentwicklung der Dock-Leiste BiMaDock (alte Version: `..\BiMaDock`, GitHub `bandit-1200/BiMaDock`). Ziel: Hauptdock und Kategorie-Dock wie bisher, aber sauber aufgebaut, stabil und modern. Keine Anzeige laufender Programme.

## Regeln

- Architektur: Logik und Daten in `BiMaDock.Core` (ohne WPF) oder in ViewModels; Code-Behind nur für reine Darstellung (Animation, Position). Keine öffentlichen Felder, kein globaler statischer Zustand.
- Fehler nie stillschweigend verschlucken: gezielte Ausnahmen fangen und über `ILogger` protokollieren.
- Warnungen gelten als Fehler (`Directory.Build.props`).
- UI-Texte und Kommentare auf Deutsch, Bezeichner Englisch.
- Jede Änderung in `CHANGELOG.md` unter `## Unveröffentlicht` eintragen.
- Vor dem Commit `dotnet build` und `dotnet test` ausführen.
- Remote: Gitea `ssh://git@192.168.32.130:2222/marco/bimadock2.git`, Push über den Deploy-Key `~/.ssh/bimadock_gitea_deploy` (in `core.sshCommand` hinterlegt).

## Fahrplan

1. Gerüst, Laden/Speichern, Import alter Daten, Ein-/Ausblenden, Starten, Kategorie-Dock ✔
2. Drag & Drop (hinzufügen, verschieben mit Lücke, in Kategorien), Kategorie anlegen, Mac-artiges Herausfließen des Kategorie-Docks ✔
3. Kontextmenü mit Bearbeiten/Löschen, Einstellungen (Verzögerung, Animation, Autostart), Aufräumen, Rückgängig ✔ (Farben noch offen)
4. Autostart, Update-Prüfung, Installer, UI-Tests, Release
