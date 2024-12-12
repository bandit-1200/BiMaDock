using System.Diagnostics;
using System.Windows;

namespace BiMaDock
{
    public partial class UpdateDialog : Window
    {
        private string latestVersion;
        private string downloadUrl;

        public UpdateDialog(string latestVersion, string downloadUrl)
        {
            InitializeComponent();
            this.latestVersion = latestVersion;
            this.downloadUrl = downloadUrl;
            UpdateMessage.Text = $"Eine neue Version ({latestVersion}) ist verfügbar. Möchtest du jetzt aktualisieren?";
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = downloadUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Download-Links: {ex.Message}");
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Setzt das Update für 30 Tage aus
            UpdateChecker.DeferUpdate();
            Close();
        }
    }
}
