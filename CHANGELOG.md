# Versionsverlauf

## 26.11.10

- Das erste Öffnen einer Kategorie ist nicht mehr langsam: Einige Sekunden nach dem Start lädt das Dock fehlende Symbole getaktet im Voraus (ein Symbol alle 75 ms, bei Leerlauf des UI-Threads). Reihenfolge: Hauptdock, dann der Inhalt der Kategorien, Reste zuletzt. Übersprungen werden Symbole, die schon im Speicher oder im Plattencache liegen, und Ziele, die nicht binnen 2 s antworten (z. B. getrennte Netzlaufwerke, vorher im Hintergrund geprüft). Öffnest du vorher eine Kategorie, lädt sie normal, das Vorladen überspringt sie dann. Symbole, die länger als 250 ms brauchen, stehen als Warnung im Protokoll, am Ende eine Zusammenfassung (Anzahl, Ladezeit). Das Takten verteilt auch die Schreibzugriffe und schont damit die Anti-Ransomware-Erkennung.
- Einfrieren und spurloses Beenden beim Öffnen von Kategorien und Ordnern behoben. Ursache: Fehlte ein Symbol im Zwischenspeicher, schrieb das Dock es auf dem UI-Thread als `.tmp`-Datei und benannte sie danach in `.png` um. Viele Dateien schreiben und dann die Endung ändern hält Anti-Ransomware-Software (hier ManageEngine Endpoint Security) für einen Angriff: Sie blockierte das Umbenennen, dadurch stand das Dock still, und kurz darauf beendete sie das Programm ohne Meldung.
- Symbole werden jetzt direkt unter dem endgültigen Namen gespeichert, ohne Umbenennen, und das im Hintergrund statt auf dem UI-Thread. Eine halb geschriebene Datei wird gelöscht oder beim nächsten Laden neu erzeugt.
- Dauert das Zwischenspeichern eines Symbols länger als 5 s, steht eine Warnung im Protokoll.
- Liegengebliebene Temp-Dateien früherer Versionen im Symbol-Zwischenspeicher werden beim ersten Schreiben eines Programmlaufs entfernt.
- Ein kaputtes oder abgeschnittenes Bild im Zwischenspeicher lässt das Dock nicht mehr abstürzen: Es wird gelöscht und neu erzeugt. Unerwartete Fehler beim Schreiben im Hintergrund kommen ins Protokoll.
- Symbole und Beschriftungen sind schärfer. Ursachen: Die Einträge lagen in Ruhe auf halben Pixeln (Layout-Rundung war für die Lupe abgeschaltet), jetzt wird in Ruhe auf ganze Pixel gerundet und nur während die Lupe arbeitet nicht. Die Beschriftung wurde mit gebrochenen Pixelmaßen (3 DIP Rand = 3,75 px bei 125 %) gezeichnet und danach neu abgetastet; sie rechnet jetzt in ganzen Pixeln, im Anzeige-Modus mit Graustufen-Text und liegt 1:1 auf dem Pixelraster. Die Symbolgröße im Zwischenspeicher richtet sich nach der Bildschirmskalierung (128, 192 oder 256 px statt immer 128), damit die Lupe auch bei 150 % und mehr nicht weich wird; die Pixelgröße steckt im Dateinamen, alte Bilder werden nur bei geänderter Größe neu erzeugt.

## 26.11.9

- Absturz beim Öffnen von Ordnern aus dem Dock behoben (unter Windows 11 seit 26.11.4). Ursache: Die Vordergrund-Benachrichtigung aus 26.11.4 lief direkt im Windows-Rückruf, mitten in der Aktion des Docks, und ein Fehler darin riss das ganze Programm mit. Sie läuft jetzt zeitversetzt auf dem UI-Thread, mehrere Meldungen werden zusammengefasst, Fehler kommen ins Protokoll statt zum Absturz.
- Starten von Zielen fängt weitere Fehlerarten ab (Zugriff verweigert, Ein-/Ausgabefehler) und meldet sie, statt abzustürzen.

## 26.11.8

- Programme aus dem Windows-Startmenü (angeheftet oder unter "Alle", auch Store-Apps) lassen sich jetzt wirklich ins Dock ziehen. Drei Ursachen waren zu beheben: Das Startmenü erlaubt beim Ziehen nur "Verknüpfung" (Link), das Dock bot "Kopieren" an und Windows verwarf das Ablegen stillschweigend. Das Startmenü meldet außerdem keine Mausbewegung, solange der Zeiger ruhig steht, der Sicherheits-Timer hat das Dock deshalb nach 300 ms wieder eingefahren, obwohl der Zeiger noch darüber stand. Und bei Store-Apps steht unter "Dateien" nur eine AppID statt eines Pfads, jetzt zählt dort die Shell-Liste.
- Ziehen aus dem Explorer-Fenster `shell:AppsFolder` ("Anwendungen") funktioniert ebenso.
- Das Protokoll hält beim Ziehen von außen Wirkung, erlaubte Wirkungen und Position fest (nur bei Änderung), dazu "Drop von außen" und "DragLeave".

## 26.11.7

- Das Dock tritt nicht mehr hinter dem Windows-Startmenü, der Suche und dem Info-Center zurück. Sie wurden fälschlich als Vollbildfenster erkannt, dadurch war das Dock beim Ziehen aus dem Startmenü verdeckt und bekam nichts davon mit. Erkannt wird am Prozess (StartMenuExperienceHost, ShellExperienceHost, SearchHost, SearchApp, ShellHost, TextInputHost, Explorer mit Shell-Fensterklasse), echte Vollbild-Apps zählen weiter als Vollbild.

## 26.11.6

- Programme aus dem Windows-Startmenü lassen sich ins Dock ziehen (auch angeheftete Programme und Store-Apps). Programme mit echtem Pfad oder Verknüpfung werden normale Einträge, Store-Apps bekommen das Ziel `shell:AppsFolder\<AppID>`. Name und Symbol kommen von Windows. Solche Einträge starten über die Shell, gelten nie als "fehlend". Dateien darauf ziehen und "Im Explorer zeigen" gibt es bei ihnen nicht.
- Beim Ablegen wird der Eintrag im Protokoll vermerkt ("Abgelegt: Name -> Ziel").

## 26.11.5

- Ziehen von außen (Explorer, Browser, Startmenü) ist robuster: Die Lücke und die Farbmarkierung im Dock verschwinden zuverlässig, auch wenn der Zeiger das Dock schnell verlässt oder der Ziehvorgang abgebrochen wird (Sicherheits-Timer nach 300 ms ohne Bewegung).
- Fehler beim Lesen der gezogenen Daten (z. B. beim Ziehen aus dem Windows-Startmenü) lassen das Dock nicht mehr abstürzen. Sie werden protokolliert, ebenso die angebotenen Formate bei unbekannten Inhalten.
- Gezogene Daten werden pro Ziehvorgang nur noch einmal gelesen statt bei jeder Mausbewegung.

## 26.11.4

- Themes: acht mitgelieferte Looks (Standard, Mitternacht, Graphit, Milchglas, Wald, Sonnenuntergang, Neon, Minimal) im neuen Reiter "Themes" der Einstellungen. Ein Klick übernimmt Farben und Effekte, Verzögerungen und Updates bleiben. Eigene Themes speichern, löschen und als Datei aus dem Themes-Ordner weitergeben.
- Website und README neu geschrieben, mit allen neuen Funktionen.
- Das Dock reagiert sofort, wenn ein Fenster ins Vollbild wechselt (z. B. RDP), statt bis zu einer halben Sekunde später. Windows meldet den Wechsel jetzt selbst, die regelmäßige Abfrage läuft nur noch alle 5 Sekunden zur Absicherung.
- Einträge, deren Ziel nicht mehr existiert, werden blass angezeigt. Geprüft wird im Hintergrund beim Start und beim Einblenden, nicht erreichbare Netzlaufwerke zählen nicht als fehlend.
- Gestartete Programme bekommen geänderte Umgebungsvariablen (z. B. PATH nach einer Installation) sofort, ohne das Dock neu zu starten.
- Schnellerer Start: Programmsymbole werden im Datenordner unter IconCache zwischengespeichert. Ändert sich das Programm, wird das Symbol neu geholt.
- Trenner: Rechtsklick auf ein Symbol, "Trenner rechts einfügen" setzt einen schmalen senkrechten Strich zum Gruppieren, im Haupt- und im Kategorie-Dock. Trenner lassen sich wie Symbole verschieben und wachsen mit der Lupe in der Höhe mit. Einen Trenner aus dem Dock ziehen und loslassen entfernt ihn (Rückgängig stellt ihn wieder her). Aufräumen ignoriert sie.
- Aufräumen findet doppelte Einträge: gleiches Ziel im selben Dock, der erste bleibt, die weiteren sind zum Entfernen vorausgewählt. Gleiche Einträge in verschiedenen Kategorien gelten als Absicht.
- Symbole einheitlich groß und ohne hellen Kasten: Programme mit nur kleinen Symbolen (z. B. PuTTY) zeigte Windows klein in einem Rahmen. Durchsichtige Ränder werden abgeschnitten. Verknüpfungen, deren Symboldatei fehlt (z. B. MobaXterm), zeigen das Symbol des Programms statt eines leeren Blatts.
- Einträge haben jetzt Startparameter, einen Arbeitsordner und "Als Administrator starten" (unter Bearbeiten). Zusätzlich startet "Als Administrator starten" im Rechtsklickmenü ein Programm einmalig mit Adminrechten.
- Dateien aus dem Explorer auf ein Programm oder eine Verknüpfung im Dock ziehen öffnet sie damit. Neben dem Symbol abgelegt werden sie wie bisher als neuer Eintrag hinzugefügt.

## 26.11.3

- Das Dock bleibt jetzt wirklich hinter einer RDP-Sitzung oder einem anderen Vollbildfenster. In 26.11.2 hat es sich beim Zurücktreten wieder davor gelegt.

## 26.11.2

- Liegt ein Vollbildfenster im Vordergrund (z. B. eine RDP-Sitzung, Spiel oder Video), tritt das Dock dahinter zurück und kommt erst wieder nach vorne, wenn das Vollbild verlassen wird.

## 26.11.1

- Nach einem Update startet das Dock wieder von selbst. Das Setup hat den Schalter /UPDATE ohne Wert nicht erkannt.

## 26.11.0

- Neues Versionsschema Jahr.Monat.Nummer, wie bei Version 1. Damit findet auch die alte Version 1 (26.10.x) das Update und kann auf BiMaDock 2 wechseln. Das Setup liegt dafür zusätzlich als BiMaDockSetup.exe im Release.

## 2.0.2

- Das Setup entfernt die alte Version 1 jetzt selbst, bei einer Installation für alle Benutzer mit einer Windows-Abfrage nach Adminrechten. Das Dock fragt nicht mehr nach. Einträge und Symbole bleiben erhalten und werden beim ersten Start übernommen.

## 2.0.1

- Alte Version wird auch entfernt, wenn sie für alle Benutzer installiert ist (Program Files): einmal fragen, dann Adminrechte. Bei "Nein" wird nicht erneut gefragt (Datei alte-version-behalten.txt im Datenordner löschen, um wieder zu fragen)
- App-Symbol für Programm, Installer und Deinstallation
- Installer werden über GitHub Actions gebaut und veröffentlicht

## 2.0.0

- Volle Leisten: Passen nicht alle Symbole auf den Bildschirm, erscheinen Pfeile; Klick, Halten, Mausrad und Ziehen scrollen
- Lupe läuft im Bildtakt, ohne Zittern und ohne Zusammenfallen zwischen zwei Symbolen; bei vielen Symbolen deutlich schneller (Schatten ohne Weichzeichner, Beschriftungen als fertige Bilder)
- Verbindungssteg zum Kategorie-Dock bleibt an beiden Docks angeheftet und federt leicht nach
- Farbe und Deckkraft der Fläche unter der Maus einstellbar, Live-Vorschau der Farben in den Einstellungen
- Neu: „Über BiMaDock …“ im Kontextmenü mit Version, Entwickler und Lizenz (MIT, LICENSE-Datei wieder im Repo)

Komplett neu entwickelte Version, ersetzt BiMaDock 26.10.x.

### Umstieg von der alten Version

- Einträge und Kategorien werden beim ersten Start übernommen
- Symbole aus dem Ordner der alten Version werden in die eigenen Symbole kopiert (Kategorie "Altes Dock")
- Danach wird das alte Dock beendet, sein Autostart entfernt und es still deinstalliert

### Neuigkeiten

- Updates: automatische Suche nach dem Start und alle 6 Stunden, wahlweise nur fragen oder still installieren; manuell über Kontextmenü oder Einstellungen
- Jede Animation hat eigene Werte: jeder Hover-Effekt merkt sich Stärke und Tempo, Nachfedern beim Einblenden, Reaktionszeit der Lupe, Anzahl und Dauer der Sprünge beim Starten, Überschwingen beim Ploppen
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
