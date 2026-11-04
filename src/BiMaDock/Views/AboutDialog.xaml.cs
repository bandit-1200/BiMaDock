using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using BiMaDock.Services;

namespace BiMaDock.Views;

/// <summary>Zeigt Version, Entwickler und Lizenz.</summary>
public partial class AboutDialog : DialogWindow
{
    private AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = $"Version {UpdateService.CurrentVersion}";
    }

    public static void Show(Window? owner) => new AboutDialog().ShowFor(owner);

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
