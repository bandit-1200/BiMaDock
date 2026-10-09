using System.IO;
using System.Windows;

namespace BiMaDock
{
    public static class StartupManager
    {
        // UI-Tests setzen einen eigenen Registry-Wertnamen, damit der echte Autostart-Eintrag unberührt bleibt.
        private static readonly string AppName =
            Environment.GetEnvironmentVariable("BIMADOCK_STARTUP_VALUE_NAME") is { Length: > 0 } name ? name : "BiMaDock";
        private static readonly string AppPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "BiMaDock.exe");

        public static void AddToStartup(bool isChecked)
        {
            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null)
                {
                    DialogMessageBox.Show("Fehler beim Zugriff auf die Registry.");
                    return;
                }

                if (isChecked)
                {
                    key.SetValue(AppName, BuildStartupCommand(AppPath));
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }

        public static bool IsInStartup()
        {
            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
            {
                if (key != null)
                {
                    return key.GetValue(AppName) != null;
                }
                return false;
            }
        }

        internal static string BuildStartupCommand(string executablePath) => $"\"{executablePath}\"";
    }
}
