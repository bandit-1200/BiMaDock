using System.Windows; // Für Window
using System.Windows.Controls; // Für Controls wie Button, TextBox etc.
using System.Windows.Input; // Für RoutedEventArgs
using System.Windows.Media;

namespace MyDockApp
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; } = string.Empty; // Sicherstellen, dass Answer nicht null ist

        public InputDialog()
        {
            InitializeComponent();
        }

        public InputDialog(string title, string question)
        {
            InitializeComponent();
            this.Title = title;
            this.QuestionTextBlock.Text = question;

            // Key-Event hinzufügen
            this.KeyDown += InputDialog_KeyDown;
        }

        private void InputDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(this, new RoutedEventArgs());
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputCategoryName = CategoryNameTextBox.Text;
            var existingItems = SettingsManager.LoadSettings();

            // Überprüfen, ob die Kategorie bereits existiert
            foreach (var item in existingItems)
            {
                if (item.DisplayName == inputCategoryName && item.IsCategory)
                {
                    MessageBox.Show($"Kategorie {inputCategoryName} existiert bereits. Bitte wählen Sie einen anderen Namen.", "Kategorie existiert", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Abbruch der Erstellung
                }
            }

            this.Answer = inputCategoryName;
            this.DialogResult = true;
            this.Close(); // Füge diese Zeile hinzu, um das Dialogfenster zu schließen
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close(); // Füge diese Zeile hinzu, um das Dialogfenster zu schließen
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
    }
}
