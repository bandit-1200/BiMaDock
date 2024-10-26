using System.Windows; // Für Window
using System.Windows.Controls; // Für Controls wie Button, TextBox etc.
using System.Windows.Input; // Für RoutedEventArgs

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
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Answer = CategoryNameTextBox.Text;
            this.DialogResult = true;
        }
    }
}
