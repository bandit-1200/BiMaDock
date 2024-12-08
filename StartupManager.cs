using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BiMaDock
{
    public static class StartupManager
    {
        private static string appName = "BiMaDock";
        private static string? appPath = Process.GetCurrentProcess().MainModule?.FileName;

        public static void AddToStartup(bool isChecked)
        {
            if (appPath == null)
            {
                MessageBox.Show("Fehler beim Ermitteln des Anwendungs-Pfads.");
                return;
            }

            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null)
                {
                    MessageBox.Show("Fehler beim Zugriff auf die Registry.");
                    return;
                }

                if (isChecked)
                {
                    key.SetValue(appName, appPath);
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }

        public static bool IsInStartup()
        {
            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
            {
                if (key != null)
                {
                    return key.GetValue(appName) != null;
                }
                return false;
            }
        }

        public static void RemoveFromStartup()
        {
            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null)
                {
                    MessageBox.Show("Fehler beim Zugriff auf die Registry.");
                    return;
                }

                key.DeleteValue(appName, false);
            }
        }
    }
}
