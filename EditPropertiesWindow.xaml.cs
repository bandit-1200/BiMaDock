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
using System.Diagnostics;

namespace BiMaDock
{
    public partial class EditPropertiesWindow : Window
    {
        public DockItem? DockItem { get; set; }
        public EditPropertiesWindow()
        {
            InitializeComponent();
            // InitializeIcons(); // Stelle sicher, dass die Symbole initialisiert werden
            Loaded += EditPropertiesWindow_Loaded; // Füge den Event-Handler hinzu

        }


        private void EditPropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeIcons(); // Rufe InitializeIcons auf, wenn das Fenster geladen ist
        }



        private void CreateAppDataIconDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            if (!Directory.Exists(iconDirectoryPath))
            {
                Directory.CreateDirectory(iconDirectoryPath);
                Debug.WriteLine("CreateAppDataIconDirectory: Verzeichnis erstellt."); // Debugging Ausgabe
            }
            else
            {
                Debug.WriteLine("CreateAppDataIconDirectory: Verzeichnis existiert bereits."); // Debugging Ausgabe
            }
        }

        private async Task LoadIconsAsync()
        {
            Debug.WriteLine("LoadIconsAsync: gestartet."); // Debugging Ausgabe

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
                            Width = 48,
                            Height = 48,
                            Margin = new Thickness(5),
                            Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                        };

                        // Ereignis hinzufügen
                        image.MouseDown += Icon_Click;

                        SymbolPanel.Children.Add(image);
                        Debug.WriteLine($"LoadIconsAsync: Icon erfolgreich hinzugefügt: {iconPath}"); // Debugging Ausgabe bei Erfolg
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadIconsAsync: Fehler beim Hinzufügen des Icons: {iconPath}. Fehler: {ex.Message}"); // Debugging Ausgabe bei Fehler
                }
            }

            Debug.WriteLine("LoadIconsAsync: abgeschlossen."); // Debugging Ausgabe
            Debug.WriteLine($"LoadIconsAsync: SymbolPanel.Children.Count = {SymbolPanel.Children.Count}"); // Debug-Ausgabe zur Überprüfung der Kinder
        }

        public async void InitializeIcons()
        {
            Debug.WriteLine("InitializeIcons: gestartet."); // Debugging Ausgabe
            CreateAppDataIconDirectory();

            if (DockItem != null)
            {
                Debug.WriteLine($"ID: {DockItem.Id}");
                Debug.WriteLine($"Name: {DockItem.DisplayName}");
                Debug.WriteLine($"IconSource: {DockItem.IconSource}");
                Debug.WriteLine($"Kategorie: {DockItem.Category}");
                Debug.WriteLine($"Ist Kategorie: {DockItem.IsCategory}");

                // Originalbild laden und in der Box anzeigen
                // var originalImage = IconHelper.GetIcon(DockItem.FilePath);
                // Originalbild laden und in der Box anzeigen
                var originalImage = IconHelper.GetIcon(DockItem.IconSource, DockItem.FilePath);

                if (DockItem.IsCategory)
                {
                    originalImage = IconHelper.GetIcon(DockItem.FilePath, DockItem.IconSource);
                }

                OriginalImage.Source = originalImage;
                OriginalImage.Width = 48;
                OriginalImage.Height = 48;
                OriginalImage.Cursor = Cursors.Hand; // Zeiger ändern, um anklickbar zu zeigen

                // OriginalImage.Stretch = Stretch.None;
                // OriginalImage.HorizontalAlignment = HorizontalAlignment.Center;
                // OriginalImage.VerticalAlignment = VerticalAlignment.Center;
                // Klick-Event für OriginalImage hinzufügen
                OriginalImage.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedIconImage.Source = originalImage;
                    DockItem.IconSource = "";
                    IconSourceTextBox.Text = "";
                    ApplicationPathTextBox.Text = "";
                };
            }

            // Zuerst Benutzer-Icons laden, wenn vorhanden, andernfalls Standard-Icons verwenden
            if (!LoadIconsFromAppData())
            {
                Debug.WriteLine("Keine Benutzer-Icons gefunden. Kopiere Standard-Icons.");
                await CopyDefaultIcons();
                LoadIconsFromAppData(); // Versuche jetzt erneut, Icons zu laden
            }

            Debug.WriteLine("InitializeIcons: abgeschlossen."); // Debugging Ausgabe
        }




        private bool LoadIconsFromAppData()
        {
            Debug.WriteLine("LoadIconsFromAppData: gestartet."); // Debugging Ausgabe

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
                                    Width = 48,
                                    Height = 48,
                                    Margin = new Thickness(5),
                                    Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                                };

                                // Ereignis hinzufügen
                                image.MouseDown += Icon_Click;
                                SymbolPanel.Children.Add(image);
                                Debug.WriteLine($"LoadIconsFromAppData: Icon erfolgreich hinzugefügt: {iconPath}");
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"LoadIconsFromAppData: Fehler beim Hinzufügen des Icons: {iconPath}. Fehler: {ex.Message}");
                        }
                    }

                    Debug.WriteLine("LoadIconsFromAppData: abgeschlossen.");
                    return true;
                }
            }

            Debug.WriteLine("LoadIconsFromAppData: Keine Icons im AppData-Verzeichnis gefunden.");
            return false; // Rückgabe false, wenn keine Icons gefunden wurden
        }

        private void DeleteIcon_Click(object sender, RoutedEventArgs e)
        {
            // Bildpfad löschen
            // var iconSourceTextBox = this.FindName("IconSourceTextBox") as TextBox;
            // var selectedIconImage = this.FindName("SelectedIconImage") as Image;
            IconSourceTextBox.Text = string.Empty;
            Debug.WriteLine("Icon_Click: IconSourceTextBox aktualisiert - " + IconSourceTextBox.Text);

            // Bild im Vorschaufenster löschen
            SelectedIconImage.Source = null;
            SelectedIconBorder.Visibility = Visibility.Collapsed;
        }



        private async Task CopyDefaultIcons()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string iconDirectoryPath = Path.Combine(appDataPath, "BiMaDock", "Icons");

            // Sicherstellen, dass das Verzeichnis existiert
            Directory.CreateDirectory(iconDirectoryPath);

            // Der relative Pfad zum Entwicklungsverzeichnis
            string developmentIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Resources\Icons");

            // Pfad zu den Icons im Entwicklungsverzeichnis verwenden
            string resourceDirectoryPath = developmentIconPath;

            // Debugging-Ausgabe des Pfades
            Console.WriteLine($"Resource Verzeichnis: {resourceDirectoryPath}");
            Console.WriteLine($"Icon Zielverzeichnis: {iconDirectoryPath}");

            // Überprüfen, ob das Quellverzeichnis existiert
            if (!Directory.Exists(resourceDirectoryPath))
            {
                Console.WriteLine("Fehler: Quellverzeichnis existiert nicht.");
                return;
            }

            // Alle Standard-Icons aus dem Entwicklungsverzeichnis kopieren
            var defaultIcons = Directory.GetFiles(resourceDirectoryPath, "*.png");
            foreach (var iconPath in defaultIcons)
            {
                string fileName = Path.GetFileName(iconPath);
                string destinationPath = Path.Combine(iconDirectoryPath, fileName);

                // Debugging-Ausgabe der Pfade
                Console.WriteLine($"Kopiere Icon: {iconPath} nach {destinationPath}");

                // Wenn das Icon noch nicht existiert, kopiere es
                if (!File.Exists(destinationPath))
                {
                    using (FileStream sourceStream = File.Open(iconPath, FileMode.Open))
                    using (FileStream destinationStream = File.Create(destinationPath))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
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
            Debug.WriteLine("UploadIcon: gestartet."); // Debugging Ausgabe

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
                        Debug.WriteLine($"UploadIcon: {fileName} erfolgreich hochgeladen und hinzugefügt.");
                    }
                    else
                    {
                        Debug.WriteLine($"UploadIcon: {fileName} existiert bereits im Verzeichnis.");
                    }
                }
                else
                {
                    Debug.WriteLine($"UploadIcon: {sourceFilePath} hat ein nicht unterstütztes Format.");
                }
            }

            Debug.WriteLine("UploadIcon: abgeschlossen.");
        }

        private void DisplayIcons()
        {
            Debug.WriteLine("DisplayIcons: gestartet."); // Debugging Ausgabe

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
                    Width = 48,
                    Height = 48,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand // Zeiger ändern, um anklickbar zu zeigen
                };

                // Ereignis hinzufügen
                image.MouseDown += Icon_Click;
                SymbolPanel.Children.Add(image);
                Debug.WriteLine($"DisplayIcons: Icon erfolgreich hinzugefügt: {iconPath}"); // Debugging Ausgabe bei Erfolg
            }

            Debug.WriteLine("DisplayIcons: abgeschlossen."); // Debugging Ausgabe
            Debug.WriteLine($"DisplayIcons: SymbolPanel.Children.Count = {SymbolPanel.Children.Count}"); // Debug-Ausgabe zur Überprüfung der Kinder
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Speichern der Änderungen
            if (DockItem != null)
            {
                DockItem.DisplayName = NameTextBox.Text;
                //DockItem.Category = CategoryTextBox.Text;
                // DockItem.IsCategory = bool.Parse(IsCategoryTextBox.Text); // Je nach Datentyp anpassen
                DockItem.IconSource = IconSourceTextBox.Text;

                // Weitere Änderungen speichern
            }

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
            Debug.WriteLine("Icon_Click: Methode aufgerufen");

            if (sender is Image image && image.Source is BitmapImage bitmap)
            {
                Debug.WriteLine("Icon_Click: Bildquelle gefunden - " + bitmap.UriSource.AbsolutePath);

                var iconSourceTextBox = this.FindName("IconSourceTextBox") as TextBox;
                var selectedIconImage = this.FindName("SelectedIconImage") as Image;

                if (iconSourceTextBox != null)
                {
                    // IconSource im Kategorie-Element speichern
                    iconSourceTextBox.Text = bitmap.UriSource.AbsolutePath;
                    Debug.WriteLine("Icon_Click: IconSourceTextBox aktualisiert - " + iconSourceTextBox.Text);
                }

                if (selectedIconImage != null)
                {
                    // Anzeigen des ausgewählten Symbols
                    selectedIconImage.Source = bitmap;
                    Debug.WriteLine("Icon_Click: SelectedIconImage aktualisiert - " + selectedIconImage.Source);
                }
            }
            else
            {
                Debug.WriteLine("Icon_Click: Kein gültiges Bild gefunden");
            }

            Debug.WriteLine("Icon_Click: Methode beendet");
        }
    }
}
