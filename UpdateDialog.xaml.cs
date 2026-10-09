using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BiMaDock
{
    public partial class UpdateDialog : Window
    {
        private readonly string latestVersion;
        private readonly string downloadUrl;

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
                DialogMessageBox.Show($"Fehler beim Öffnen des Download-Links: {ex.Message}");
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateChecker.DeferUpdate(latestVersion);
                Close();
            }
            catch (IOException ex)
            {
                DialogMessageBox.Show($"Das Update konnte nicht zurückgestellt werden:\n{ex.Message}", "BiMaDock-Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (UnauthorizedAccessException ex)
            {
                DialogMessageBox.Show($"Das Update konnte nicht zurückgestellt werden:\n{ex.Message}", "BiMaDock-Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                DialogMessageBox.Show($"Das Update konnte nicht zurückgestellt werden:\n{ex.Message}", "BiMaDock-Update", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
