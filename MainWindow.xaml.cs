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
            dockManager = new DockManager(DockPanel); // Korrigierte Initialisierung
            dockManager.LoadDockItems();
            this.Closing += (s, e) => dockManager.SaveDockItems(); // Einstellungen beim Schließen speichern
        }
    }
}
