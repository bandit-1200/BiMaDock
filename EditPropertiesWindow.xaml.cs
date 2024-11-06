using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input; // Dieser Namespace enthält die Cursors-Klasse
using System.Windows.Media.Imaging;

namespace BiMaDock
{
    public partial class EditPropertiesWindow : Window
    {
        public EditPropertiesWindow()
        {
            InitializeComponent();
            InitializeIcons(); // Stelle sicher, dass die Symbole initialisiert werden
        }

        private void CreateAppDataIconDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            if (!Directory.Exists(iconDirectoryPath))
            {
                Directory.CreateDirectory(iconDirectoryPath);
                Console.WriteLine("CreateAppDataIconDirectory: Verzeichnis erstellt."); // Debugging Ausgabe
            }
            else
            {
                Console.WriteLine("CreateAppDataIconDirectory: Verzeichnis existiert bereits."); // Debugging Ausgabe
            }
        }

        private async Task LoadIconsAsync()
        {
            Console.WriteLine("LoadIconsAsync: gestartet."); // Debugging Ausgabe

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            var icons = Directory.GetFiles(iconDirectoryPath, "*.png");
            foreach (var iconPath in icons)
            {
                try
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(iconPath)),
                            Width = 64,
                            Height = 64,
                            Margin = new Thickness(5),
                            Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                        };

                        // Ereignis hinzufügen
                        image.MouseDown += Icon_Click;

                        SymbolPanel.Children.Add(image);
                        Console.WriteLine($"LoadIconsAsync: Icon erfolgreich hinzugefügt: {iconPath}"); // Debugging Ausgabe bei Erfolg
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadIconsAsync: Fehler beim Hinzufügen des Icons: {iconPath}. Fehler: {ex.Message}"); // Debugging Ausgabe bei Fehler
                }
            }

            Console.WriteLine("LoadIconsAsync: abgeschlossen."); // Debugging Ausgabe
            Console.WriteLine($"LoadIconsAsync: SymbolPanel.Children.Count = {SymbolPanel.Children.Count}"); // Debug-Ausgabe zur Überprüfung der Kinder
        }

        public void InitializeIcons()
        {
            Console.WriteLine("InitializeIcons: gestartet."); // Debugging Ausgabe
            CreateAppDataIconDirectory();

            // Zuerst Benutzer-Icons laden, wenn vorhanden, andernfalls Standard-Icons verwenden
            if (!LoadIconsFromAppData())
            {
                Console.WriteLine("Keine Benutzer-Icons gefunden. Kopiere Standard-Icons.");
                CopyDefaultIcons();
                LoadIconsFromAppData(); // Versuche jetzt erneut, Icons zu laden
            }

            Console.WriteLine("InitializeIcons: abgeschlossen."); // Debugging Ausgabe
        }

        private bool LoadIconsFromAppData()
        {
            Console.WriteLine("LoadIconsFromAppData: gestartet."); // Debugging Ausgabe

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            // Überprüfe, ob es Icons im AppData-Verzeichnis gibt
            if (Directory.Exists(iconDirectoryPath))
            {
                var icons = Directory.GetFiles(iconDirectoryPath, "*.png"); // Nur .png-Dateien laden
                if (icons.Length > 0)
                {
                    foreach (var iconPath in icons)
                    {
                        try
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                var image = new Image
                                {
                                    Source = new BitmapImage(new Uri(iconPath)),
                                    Width = 64,
                                    Height = 64,
                                    Margin = new Thickness(5),
                                    Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                                };

                                // Ereignis hinzufügen
                                image.MouseDown += Icon_Click;
                                SymbolPanel.Children.Add(image);
                                Console.WriteLine($"LoadIconsFromAppData: Icon erfolgreich hinzugefügt: {iconPath}");
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"LoadIconsFromAppData: Fehler beim Hinzufügen des Icons: {iconPath}. Fehler: {ex.Message}");
                        }
                    }

                    Console.WriteLine("LoadIconsFromAppData: abgeschlossen.");
                    return true;
                }
            }

            Console.WriteLine("LoadIconsFromAppData: Keine Icons im AppData-Verzeichnis gefunden.");
            return false; // Rückgabe false, wenn keine Icons gefunden wurden
        }


        private void CopyDefaultIcons()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            // Sicherstellen, dass das Verzeichnis existiert
            Directory.CreateDirectory(iconDirectoryPath);

            // Der Pfad zum Installationsverzeichnis
            string? installationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Überprüfen, ob installationPath null ist
            if (string.IsNullOrEmpty(installationPath))
            {
                Console.WriteLine("CopyDefaultIcons: Installationspfad konnte nicht ermittelt werden.");
                return; // Wenn der Pfad nicht ermittelt werden kann, abbrechen
            }

            // Pfad zu den Icons im Installationsverzeichnis
            string resourceDirectoryPath = Path.Combine(installationPath, "Icons");

            Console.WriteLine($"CopyDefaultIcons: Resource Verzeichnis: {resourceDirectoryPath}");

            // Alle Standard-Icons aus dem Installationsverzeichnis kopieren
            var defaultIcons = Directory.GetFiles(resourceDirectoryPath, "*.png");
            foreach (var iconPath in defaultIcons)
            {
                string fileName = Path.GetFileName(iconPath);
                string destinationPath = Path.Combine(iconDirectoryPath, fileName);

                // Wenn das Icon noch nicht existiert, kopiere es
                if (!File.Exists(destinationPath))
                {
                    File.Copy(iconPath, destinationPath);
                    Console.WriteLine($"CopyDefaultIcons: {fileName} hinzugefügt.");
                }
                else
                {
                    Console.WriteLine($"CopyDefaultIcons: {fileName} existiert bereits im Verzeichnis.");
                }
            }
        }

        private void UploadIcon_Click(object sender, RoutedEventArgs e)
        {
            UploadIcon();
            DisplayIcons();
        }

        private void UploadIcon()
        {
            Console.WriteLine("UploadIcon: gestartet."); // Debugging Ausgabe

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            // Sicherstellen, dass das Verzeichnis existiert
            Directory.CreateDirectory(iconDirectoryPath);

            // FileDialog zum Hochladen von Bildern öffnen
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.ico",
                Title = "Wähle ein Icon zum Hochladen"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string sourceFilePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(sourceFilePath).ToLower();

                if (fileExtension == ".png" || fileExtension == ".ico")
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    string destinationPath = Path.Combine(iconDirectoryPath, fileName);

                    if (!File.Exists(destinationPath))
                    {
                        File.Copy(sourceFilePath, destinationPath);
                        Console.WriteLine($"UploadIcon: {fileName} erfolgreich hochgeladen und hinzugefügt.");
                    }
                    else
                    {
                        Console.WriteLine($"UploadIcon: {fileName} existiert bereits im Verzeichnis.");
                    }
                }
                else
                {
                    Console.WriteLine($"UploadIcon: {sourceFilePath} hat ein nicht unterstütztes Format.");
                }
            }

            Console.WriteLine("UploadIcon: abgeschlossen.");
        }

        private void DisplayIcons()
        {
            Console.WriteLine("DisplayIcons: gestartet."); // Debugging Ausgabe

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            // Sicherstellen, dass das Verzeichnis existiert
            Directory.CreateDirectory(iconDirectoryPath);

            // Leeren der Symbolbox
            SymbolPanel.Children.Clear();

            var icons = Directory.GetFiles(iconDirectoryPath, "*.png");
            foreach (var iconPath in icons)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(iconPath)),
                    Width = 64,
                    Height = 64,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                };

                // Ereignis hinzufügen
                image.MouseDown += Icon_Click;
                SymbolPanel.Children.Add(image);
                Console.WriteLine($"DisplayIcons: Icon erfolgreich hinzugefügt: {iconPath}"); // Debugging Ausgabe bei Erfolg
            }

            Console.WriteLine("DisplayIcons: abgeschlossen."); // Debugging Ausgabe
            Console.WriteLine($"DisplayIcons: SymbolPanel.Children.Count = {SymbolPanel.Children.Count}"); // Debug-Ausgabe zur Überprüfung der Kinder
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Speichern der Änderungen
            this.DialogResult = true; // Schließen des Fensters mit Erfolg
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Abbrechen der Änderungen
            this.DialogResult = false; // Schließen des Fensters ohne Erfolg
        }

        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                string originalColor = btn.Background.ToString();
                btn.Tag = originalColor;  // Speichern der ursprünglichen Farbe im Tag-Attribut
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A5A5A")); // Dezente Hover-Farbe
                btn.Foreground = new SolidColorBrush(Colors.Black); // Schriftfarbe ändern
            }
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string originalColor)
            {
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(originalColor)); // Ursprüngliche Farbe wiederherstellen
                btn.Foreground = new SolidColorBrush(Colors.White); // Schriftfarbe zurücksetzen
            }
        }

        private void Icon_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Icon_Click: Methode aufgerufen");

            if (sender is Image image && image.Source is BitmapImage bitmap)
            {
                Console.WriteLine("Icon_Click: Bildquelle gefunden - " + bitmap.UriSource.AbsolutePath);

                var iconSourceTextBox = this.FindName("IconSourceTextBox") as TextBox;
                var selectedIconImage = this.FindName("SelectedIconImage") as Image;

                if (iconSourceTextBox != null)
                {
                    // IconSource im Kategorie-Element speichern
                    iconSourceTextBox.Text = bitmap.UriSource.AbsolutePath;
                    Console.WriteLine("Icon_Click: IconSourceTextBox aktualisiert - " + iconSourceTextBox.Text);
                }

                if (selectedIconImage != null)
                {
                    // Anzeigen des ausgewählten Symbols
                    selectedIconImage.Source = bitmap;
                    Console.WriteLine("Icon_Click: SelectedIconImage aktualisiert - " + selectedIconImage.Source);
                }
            }
            else
            {
                Console.WriteLine("Icon_Click: Kein gültiges Bild gefunden");
            }

            Console.WriteLine("Icon_Click: Methode beendet");
        }
    }
}
