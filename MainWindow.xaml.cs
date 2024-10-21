using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MyDockApp
{
    public partial class MainWindow : Window
    {
        private DockManager dockManager;

        public MainWindow()
        {
            InitializeComponent();
            AllowDrop = true;
            dockManager = new DockManager(DockPanel, this); // Aktualisieren, um das Hauptfenster zu übergeben
            dockManager.LoadDockItems();
            this.Closing += (s, e) => dockManager.SaveDockItems(); // Einstellungen beim Schließen speichern

            // Kontextmenü anzeigen beim Rechtsklick auf das DockPanel
            DockPanel.MouseRightButtonDown += (s, e) =>
            {
                OpenMenuItem.Visibility = Visibility.Collapsed;
                DeleteMenuItem.Visibility = Visibility.Collapsed;
                EditMenuItem.Visibility = Visibility.Collapsed;
                DockContextMenu.IsOpen = true;
            };
        }

        // Event-Handler für das Beenden des Docks
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Platzhalter-Event-Handler für elementspezifische Aktionen
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button)
            {
                Console.WriteLine("Open_Click aufgerufen. Button Tag: " + (button.Tag ?? "null")); // Debug-Ausgabe des Tags
                if (button.Tag is string filePath)
                {
                    OpenFile(filePath);
                }
                else
                {
                    Console.WriteLine("Tag ist kein Dateipfad"); // Debug-Ausgabe bei Fehler
                }
            }
            else
            {
                Console.WriteLine("PlacementTarget ist kein Button"); // Debug-Ausgabe bei Fehler
            }
        }

        // Methode zum Öffnen der Datei
        public void OpenFile(string filePath)
        {
            try
            {
                Console.WriteLine("Dateipfad: " + filePath); // Ausgabe des Dateipfads
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Öffnen der Datei: " + ex.Message); // Debug-Ausgabe bei Fehler
            }
        }

        // Platzhalter für weitere Methoden
        // Event-Handler für das Löschen eines Dock-Elements
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is string filePath)
            {
                Console.WriteLine("Löschen des Elements: " + filePath); // Debug-Ausgabe
                dockManager.RemoveDockItem(button);
            }
        }


        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // Implementierung folgt
        }
    }
}
