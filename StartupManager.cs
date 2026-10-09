using System.IO;
using System.Windows;

namespace BiMaDock
{
    public static class StartupManager
    {
        private const string AppName = "BiMaDock";
        private static readonly string AppPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "BiMaDock.exe");

        public static void AddToStartup(bool isChecked)
        {
            using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null)
                {
                    MessageBox.Show("Fehler beim Zugriff auf die Registry.");
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
