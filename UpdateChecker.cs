using System;
using System.Diagnostics;
using System.IO;
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
        private static string ConfigFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock", "update_config.txt");

        public static async Task CheckForUpdatesAsync(bool ignoreDefer = false)
        {
            if (!ignoreDefer && IsUpdateDeferred())
            {
                Debug.WriteLine("Updateprüfung ist für 30 Tage ausgesetzt.");
                return;
            }

            string currentVersion = GetCurrentVersion();
            Debug.WriteLine($"Installierte Version: {currentVersion}");
            Console.WriteLine($"Installierte Version: {currentVersion}");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "BiMaDock-Update-Checker");
                try
                {
                    string response = await client.GetStringAsync(GitHubApiUrl);
                    JObject releaseInfo = JObject.Parse(response);
                    string latestVersion = releaseInfo["tag_name"]?.ToString().Trim('v') ?? "Unknown";
                    string downloadUrl = releaseInfo["assets"]?[0]?["browser_download_url"]?.ToString() ?? "Unknown";

                    Debug.WriteLine($"Verfügbare Version: {latestVersion}");

                    if (IsNewVersionAvailable(currentVersion, latestVersion))
                    {
                        Debug.WriteLine($"Ein neues Update ist verfügbar: Version {latestVersion}");
                        Debug.WriteLine($"Download-Link: {downloadUrl}");

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateDialog updateDialog = new UpdateDialog(latestVersion, downloadUrl);
                            updateDialog.ShowDialog();
                        });
                    }
                    else
                    {
                        Debug.WriteLine("Keine Updates verfügbar.");
                        if (ignoreDefer)
                        {
                            // Entferne "Build" vor der Anzeige in der MessageBox
                            string latestVersionTrimmed = latestVersion.Replace("-Build", ".");
                            MessageBox.Show($"Ihre Software ist auf dem neuesten Stand.\n\nInstallierte Version: {currentVersion}\nVerfügbare Version: {latestVersionTrimmed}");
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine($"Fehler: {e.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler: {ex.Message}");
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
            Version current = new Version(TrimBuildNumber(currentVersion));
            Version latest = new Version(TrimBuildNumber(latestVersion));
            return latest > current;
        }

        private static string TrimBuildNumber(string version)
        {
            // Entfernt das "-BuildN" Suffix
            var buildIndex = version.IndexOf("-Build");
            return buildIndex > -1 ? version.Substring(0, buildIndex) : version;
        }


        private static bool IsUpdateDeferred()
        {
            if (File.Exists(ConfigFilePath))
            {
                string deferredDateStr = File.ReadAllText(ConfigFilePath);
                if (DateTime.TryParse(deferredDateStr, out DateTime deferredDate))
                {
                    if (DateTime.Now < deferredDate)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DeferUpdate()
        {
            DateTime deferUntil = DateTime.Now.AddDays(30);
            string configFilePath = ConfigFilePath;
            string? directoryPath = Path.GetDirectoryName(configFilePath);

            if (directoryPath != null && !string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                File.WriteAllText(configFilePath, deferUntil.ToString());
            }
            else
            {
                Debug.WriteLine("Fehler: Der Verzeichnisname ist ungültig.");
            }
        }
    }
}
