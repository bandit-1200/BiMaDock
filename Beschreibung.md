### Manuelles Erhöhen der Versionsnummer und Pushe zu GitHub

#### 1. Neueste Tag-Version herausfinden
Öffne dein Terminal oder die Kommandozeile und gehe in dein Projektverzeichnis. Führe den folgenden Befehl aus, um die neueste Tag-Version zu sehen:
```sh

# Aktuellste Version herausfinden (z.B. v1.0.3)
git tag --sort=-creatordate

# Tag manuell erhöhen und einen neuen Tag erstellen (z.B. v2.0.0)
git tag -a v2.0.0 -m "Release v2.0.0"

# Pushe den neuen Tag zu GitHub:
git push origin v2.0.0


# Alle lokalen Tags loeschen:
git tag -l | ForEach-Object { git tag -d $_ }

# Alle Remote-Tags loeschen:
git tag -l | ForEach-Object { git push origin --delete $_ }


Mit diesen Schritten kannst du die Versionsnummer manuell erhöhen und den Build-Prozess auslösen. 🚀😊
