# BiMaDock

BiMaDock ist eine Windows-Anwendung auf Basis von WPF, die eine personalisierbare Dock-Leiste für Programme, Dateien und häufig genutzte Aufgaben bereitstellt. Die App soll einen schnellen Zugriff auf wichtige Elemente am Bildschirmrand ermöglichen, ohne den Desktop zu überladen.

## Funktionen

- Drag & Drop zum Hinzufügen von Dateien, Ordnern und Programmen
- Automatisches Ein- und Ausblenden der Dock-Leiste
- Unterstützung für Kategorien und organisierte Gruppen
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

## Download

- [Neueste Version herunterladen](https://github.com/bandit-1200/BiMaDock/releases/latest)
- [Projekt-Website](https://bandit-1200.github.io/BiMaDock/)

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
- Objekte nach Typen oder Aufgaben gruppieren.
- So lassen sich häufig genutzte Einträge schnell finden und sauber strukturieren.

### Dock-Verhalten anpassen
- Ein- und Ausblendung konfigurieren
- Reaktionszeit und Erscheinungsbild individuell definieren
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
dotnet build
```

Oder direkt ausführen:

```bash
dotnet run --project .\BiMaDock.csproj
```

## Projektstatus

BiMaDock ist ein aktives Desktop-Projekt mit Fokus auf:

- praktische Dock-Funktionalität
- stabilen Start und Laufzeitverhalten
- nutzerfreundliche Konfigurierbarkeit
- saubere WPF-Integration unter Windows

## Lizenz

Das Projekt steht unter der MIT-Lizenz. Weitere Details finden sich in der LICENSE-Datei.
