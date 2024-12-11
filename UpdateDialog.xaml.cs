using System.Diagnostics;
using System.Windows;

namespace BiMaDock
{
    public partial class UpdateDialog : Window
    {
        private string downloadUrl;

        public UpdateDialog(string latestVersion, string downloadUrl)
        {
            InitializeComponent();
            UpdateMessage.Text = $"Ein neues Update (Version {latestVersion}) ist verfügbar. Möchten Sie es jetzt herunterladen und installieren?";
            this.downloadUrl = downloadUrl;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
