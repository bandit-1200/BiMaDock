using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace BiMaDock
{
    public class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/bandit-1200/BiMaDock/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            string currentVersion = GetCurrentVersion();
            Debug.WriteLine($"Installierte Version: {currentVersion}");
            Console.WriteLine($"Installierte Version: {currentVersion}");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "BiMaDock-Update-Checker");
                string response = await client.GetStringAsync(GitHubApiUrl);
                if (response != null)
                {
                    JObject releaseInfo = JObject.Parse(response);
                    if (releaseInfo != null)
                    {
                        string latestVersion = releaseInfo["tag_name"]?.ToString().Trim('v') ?? "Unknown";
                        string downloadUrl = releaseInfo["assets"]?[0]?["browser_download_url"]?.ToString() ?? "Unknown";

                        Debug.WriteLine($"Verfügbare Version: {latestVersion}");

                        if (IsNewVersionAvailable(currentVersion, latestVersion))
                        {
                            Debug.WriteLine($"Ein neues Update ist verfügbar: Version {latestVersion}");
                            Debug.WriteLine($"Download-Link: {downloadUrl}");

                            // Zeige den Update-Dialog nur, wenn ein Update verfügbar ist
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                UpdateDialog updateDialog = new UpdateDialog(latestVersion, downloadUrl);
                                updateDialog.ShowDialog();
                            });
                        }
                        else
                        {
                            Debug.WriteLine("Keine Updates verfügbar.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Fehler: Konnte Release-Informationen nicht abrufen.");
                    }
                }
                else
                {
                    Debug.WriteLine("Fehler: Konnte keine Antwort vom Server abrufen.");
                }
            }
        }

        private static string GetCurrentVersion()
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute?.InformationalVersion ?? "Unbekannte Version";
            string clearVersion = informationalVersion.Split('+')[0];

            return clearVersion;
        }

        private static bool IsNewVersionAvailable(string currentVersion, string latestVersion)
        {
            // Entferne die Buildnummer für den Vergleich
            Version current = new Version(TrimBuildNumber(currentVersion));
            Version latest = new Version(latestVersion);
            return latest > current;
        }

        private static string TrimBuildNumber(string version)
        {
            var parts = version.Split('.');
            return parts.Length > 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : version;
        }
    }
}
