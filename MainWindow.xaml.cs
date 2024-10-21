using System;
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
            // Implementierung folgt
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            // Implementierung folgt
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // Implementierung folgt
        }
    }
}
