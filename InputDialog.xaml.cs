using System.Windows; // Für Window
using System.Windows.Input; // Für RoutedEventArgs

namespace BiMaDock
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; } = string.Empty; // Sicherstellen, dass Answer nicht null ist

        public InputDialog(string title, string question)
        {
            InitializeComponent();
            this.Title = title;
            this.QuestionTextBlock.Text = question;
            Loaded += (s, e) => CategoryNameTextBox.Focus(); // Fokus auf das Textfeld setzen

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
    }
}
