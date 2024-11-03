using System.Windows;

namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen; // Fenster in der Mitte des Bildschirms starten
        }

        private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Leere Methode
        }

        private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Leere Methode
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Leere Methode
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Fenster schließen
        }
    }
}
