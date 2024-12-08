using System.IO;
using System.Windows;

namespace BiMaDock
{
    public static class StartupManager
    {
        private static string appName = "BiMaDock";
        private static string appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        private static string appPath = Path.Combine(appDirectory, "BiMaDock.exe");

        public static void AddToStartup(bool isChecked)
        {
            // Debugging-Ausgabe zur Überprüfung des Pfads
            // MessageBox.Show($"DEBUG: appPath = {appPath}");

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
