using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace BiMaDock
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            ShowVersion();
        }

        private void ShowVersion()
        {
            VersionTextBox.Text = $"Version: {UpdateChecker.GetCurrentVersion()}";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateChecker.CheckForUpdatesAsync(ignoreDefer: true);
        }





        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
