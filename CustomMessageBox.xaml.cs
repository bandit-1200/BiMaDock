using System.Windows;

namespace BiMaDock
{
    public partial class CustomMessageBox : Window
    {
        public bool Result { get; private set; }

        public CustomMessageBox(string message)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
            Loaded += (_, _) => YesButton.Focus();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            CloseDialog(true);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            CloseDialog(false);
        }

        private void CloseDialog(bool dialogResult)
        {
            try
            {
                DialogResult = dialogResult;
            }
            catch (InvalidOperationException)
            {
                // Nicht modal angezeigt
                Close();
            }
        }
    }
}
