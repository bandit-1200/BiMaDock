using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MyDockApp
{
    public partial class EditPropertiesWindow : Window
    {
        public BitmapSource? SelectedIcon { get; private set; }

        public EditPropertiesWindow()
        {
            InitializeComponent();

            // Event-Handler für Symbole hinzufügen
            foreach (var child in SymbolPanel.Children)
            {
                if (child is Image image)
                {
                    image.MouseLeftButtonDown += (s, e) =>
                    {
                        SelectedIcon = (BitmapSource)image.Source;
                        MessageBox.Show("Symbol ausgewählt!"); // Zum Testen
                    };
                }
            }
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
    }
}
