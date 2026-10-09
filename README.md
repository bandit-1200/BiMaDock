# BiMaDock

BiMaDock ist eine Windows-Anwendung auf Basis von WPF, die eine personalisierbare Dock-Leiste für Programme, Dateien und häufig genutzte Aufgaben bereitstellt. Die App soll einen schnellen Zugriff auf wichtige Elemente am Bildschirmrand ermöglichen, ohne den Desktop zu überladen.

## Funktionen

- Drag & Drop zum Hinzufügen von Dateien, Ordnern, Programmen und Weblinks
- Verschieben im Dock wie beim macOS-Dock: Nachbarn gleiten zur Seite, ein Abbild folgt der Maus
- Automatisches Ein- und Ausblenden der Dock-Leiste
- Kategorien, die fließend aus ihrem Symbol unter dem Hauptdock aufklappen (abschaltbar, Dauer einstellbar)
- „Aufräumen“: findet Einträge deinstallierter Programme und gelöschter Dateien und entfernt sie nach Auswahl – mit Rückgängig-Funktion
- Scharfe Icons, auch bei hoher Bildschirmskalierung
- Konfigurierbare Darstellung mit Design- und Layout-Einstellungen
- Kontextmenüs für schnelle Aktionen wie Bearbeiten, Löschen und Öffnen
- Flexible Anpassung der Reaktionszeit und Dock-Verhalten
- Optimierte WPF-Implementierung für ein sauberes und reaktionsschnelles Verhalten

## Warum BiMaDock?

BiMaDock verbindet die Flexibilität eines schnellen Startbereichs mit der Übersichtlichkeit eines modernen Desktop-Layouts. Sie eignet sich besonders für Nutzer, die häufig auf bestimmte Programme, Ordner oder Dateien zugreifen und dabei eine kompakte, gut organisierte Oberfläche bevorzugen.

## Voraussetzungen

- Windows 10 oder höher
- .NET 8 Desktop Runtime
- WPF-Unterstützung durch das Betriebssystem
- Keine Administratorrechte nötig: Die Installation erfolgt pro Windows-Benutzer

## Download

- [Neueste Version herunterladen](https://github.com/bandit-1200/BiMaDock/releases/latest)
- [Projekt-Website](https://bandit-1200.github.io/BiMaDock/)

BiMaDock prüft beim Start auf neue veröffentlichte Releases. Über **Über BiMaDock → Auf Update prüfen** lässt sich die Prüfung jederzeit manuell starten. Zurückstellen gilt nur für das angezeigte Release; eine spätere Version wird weiterhin angeboten.

## Schnellstart

1. Die neueste Version herunterladen und installieren.
2. BiMaDock starten.
3. Objekte per Drag & Drop in das Dock legen.
4. Über das Einstellungsmenü Design, Verhalten und Dock-Optionen anpassen.

## Verwendung

### Programme oder Dateien hinzufügen

- Dateien, Ordner oder Verknüpfungen per Drag & Drop in das Dock ziehen.
- Ein Klick auf ein Element startet es direkt.

### Kategorien nutzen

- Über das Kontextmenü (Rechtsklick) → **Kategorie erstellen** eine Kategorie anlegen.
- Elemente per Drag & Drop auf die Kategorie ziehen; ein Klick auf die Kategorie klappt ihre Elemente unter dem Hauptdock auf.

### Dock aufräumen

- Kontextmenü → **Aufräumen …** prüft, ob Programme, Dateien, Ordner und Verknüpfungsziele noch vorhanden sind.
- Fehlende Einträge sind vorausgewählt; nicht erreichbare Netzwerk- oder USB-Pfade und leere Kategorien werden ohne Haken angeboten.
- Vor dem Entfernen wird eine Sicherung angelegt; **Aufräumen rückgängig machen** stellt die Einträge wieder her.

### Dock-Verhalten anpassen

- Ein- und Ausblendung konfigurieren
- Reaktionszeit, Farben, Hover-Animationen und das Aufklappen der Kategorien einstellen
- Layout- und Designoptionen über die Einstellungen anpassen

## Entwicklung

BiMaDock wird in C# mit WPF entwickelt und nutzt eine modulare Struktur für:

- Dock-Management und Anordnung
- Drag & Drop-Logik
- Einstellungs- und Persistenzverwaltung
- UI- und Theme-Anpassungen
- Start- und Laufzeitlogik

### Projekt lokal bauen

```bash
dotnet build BiMaDock.sln -c Release
```

Oder direkt ausführen:

```bash
dotnet run --project .\BiMaDock.csproj
```

### Tests

```powershell
.\test.ps1               # Unit-Tests (xUnit)
.\ui-test.ps1 -c Release # UI-Tests (FlaUI) – steuern Maus und Tastatur
```

Die UI-Tests starten BiMaDock mit einem eigenen, temporären Datenordner; echte Einstellungen bleiben unberührt.

### Versionen und Änderungen

Versionen folgen dem Schema `JJ.MM.N` (z. B. `26.11.3`) und werden nur für ein Release gesetzt. Alle Änderungen stehen in [CHANGELOG.md](CHANGELOG.md), Details zum Release-Ablauf in [Beschreibung.md](Beschreibung.md).

## Projektstatus

BiMaDock ist ein aktives Desktop-Projekt mit Fokus auf:

- praktische Dock-Funktionalität
- stabilen Start und Laufzeitverhalten
- nutzerfreundliche Konfigurierbarkeit
- saubere WPF-Integration unter Windows

## Lizenz

Das Projekt steht unter der MIT-Lizenz. Weitere Details finden sich in der LICENSE-Datei.
