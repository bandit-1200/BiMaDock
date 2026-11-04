# Versionsverlauf

## Unveröffentlicht

### Neuigkeiten

- Lupe wie beim Mac: Symbol unter der Maus wächst, Nachbarn wachsen abgestuft mit (Größe und Reichweite einstellbar)
- Dock einblenden: Sofort, Gleiten oder Gleiten mit Federn, Dauer einstellbar
- Neue Einträge ploppen herein, entfernte schrumpfen weg (auch bei Rückgängig)
- Hover-Animationen mit einstellbarer Stärke und Tempo, Sprunghöhe beim Starten einstellbar
- Einstellungen mit Tabs: Allgemein, Aussehen, Animationen, Symbole
- Symbolbibliothek mit 98 eigenen Symbolen in 8 Kategorien (Generator: tools/generate-icons.py), Suche, Kategorien, eigene Symbole hinzufügen per Dialog oder Ziehen
- Installer (Inno Setup, installer/build.ps1): ohne Adminrechte für den Benutzer oder für alle; Symbolbibliothek liegt im Programmordner, nichts wird in fremde Profile geschrieben (Fehler der alten Version behoben)
- Dunkle schmale Scrollbalken
- Namen unter den Symbolen (abschaltbar in den Einstellungen)
- Hover-Animationen wählbar: Keine, Vergrößern, Hüpfen, Schaukeln, Wackeln, Pulsieren
- Symbol hüpft beim Starten (abschaltbar)
- Neuentwicklung auf .NET 10 mit MVVM, Dependency Injection und Dateiprotokoll
- Hauptdock am oberen Bildschirmrand mit automatischem Ein- und Ausblenden
- Kategorie-Dock klappt animiert unter dem Kategorie-Symbol auf; die geöffnete Kategorie ist markiert
- Übernahme der Einträge aus der bisherigen Version beim ersten Start
- Scharfe Symbole in 128 px, Per-Monitor-DPI
- Beschädigte Dock-Datei wird gesichert statt die App zu blockieren
- Kategorie-Dock fließt wie Wasser aus dem Hauptdock: Ein Tropfen quillt unter dem Kategorie-Symbol hervor und fällt nach unten, beim Aufprall läuft er als flacher Strom mit Wellen nach beiden Seiten und füllt sich zur Dock-Fläche auf; ein geschwungener Steg bleibt mit dem Symbol verbunden. Beim Schließen fließt es zurück. Dauer einstellbar, abschaltbar
- Farbeinstellungen: Dock-Farbe, Deckkraft, Rand und Akzent mit Farbfeldern oder eigenem Hex-Wert; Änderungen sind sofort am Dock zu sehen
- Kontextmenü auf Einträgen: Öffnen, Im Explorer zeigen, Bearbeiten (Name, Ziel, eigenes Symbol mit Vorschau), Entfernen (Kategorien samt Inhalt nach Rückfrage)
- Rückgängig für Entfernen, Bearbeiten, Verschieben und Aufräumen (bis zu 20 Schritte)
- Aufräumen: findet fehlende Ziele (vorausgewählt), nicht erreichbare Netzwerk-/USB-Pfade und leere Kategorien (nicht vorausgewählt)
- Einstellungen: Verzögerung beim Ein- und Ausblenden, Herausfließen an/aus und Dauer, Autostart mit Windows
- Einheitliches dunkles Design für Menüs und Dialoge; Dialoge lassen sich an jeder freien Stelle verschieben, Esc schließt
- Drag & Drop: Dateien, Ordner, Verknüpfungen und Weblinks aus Explorer oder Browser ins Haupt- oder Kategorie-Dock ziehen
- Verschieben im Dock mit animierter Lücke und einem Abbild, das der Maus folgt; auch zwischen Haupt- und Kategorie-Dock
- Ablegen mitten auf einer Kategorie legt in die Kategorie; kurzes Verweilen öffnet sie
- Farbhinweis beim Ziehen: Das Ziel-Dock färbt sich weich in die Hinweisfarbe (Standard #D2691E wie in der alten Version), in den Einstellungen wählbar
- Kontextmenü „Neue Kategorie …“ mit eigenem Eingabedialog im Dock-Design
