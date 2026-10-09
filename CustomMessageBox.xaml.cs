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
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.Close();
        }
    }
}
