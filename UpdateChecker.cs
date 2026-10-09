using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace BiMaDock
{
    public class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/bandit-1200/BiMaDock/releases/latest";
        private const string InstallerAssetName = "BiMaDockSetup.exe";
        private static string ConfigFilePath => AppPaths.GetUpdateConfigFilePath();

        public static async Task CheckForUpdatesAsync(bool ignoreDefer = false)
        {
            string currentVersion = GetCurrentVersion();

            try
            {
                if (!ReleaseVersion.TryParse(currentVersion, out _))
                {
                    throw new InvalidDataException($"The installed application version is invalid: '{currentVersion}'.");
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.Add("User-Agent", "BiMaDock-Update-Checker");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                using HttpResponseMessage response = await client.GetAsync(GitHubApiUrl);
                response.EnsureSuccessStatusCode();
                JObject releaseInfo = JObject.Parse(await response.Content.ReadAsStringAsync());

                string latestVersion = releaseInfo["tag_name"]?.Value<string>() ?? string.Empty;
                if (!ReleaseVersion.TryParse(latestVersion, out _))
                {
                    throw new InvalidDataException($"GitHub returned an invalid release tag: '{latestVersion}'.");
                }

                string? downloadUrl = releaseInfo["assets"]?
                    .Children<JObject>()
                    .FirstOrDefault(asset => string.Equals(
                        asset["name"]?.Value<string>(),
                        InstallerAssetName,
                        StringComparison.OrdinalIgnoreCase))?["browser_download_url"]?.Value<string>();

                if (string.IsNullOrWhiteSpace(downloadUrl) ||
                    !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? downloadUri) ||
                    downloadUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidDataException($"The latest release does not contain a valid {InstallerAssetName} download.");
                }

                Debug.WriteLine($"Installed release: {currentVersion}; latest release: {latestVersion}");

                if (!ignoreDefer && IsUpdateDeferred(latestVersion))
                {
                    Debug.WriteLine($"Update {latestVersion} was deferred.");
                    return;
                }

                if (ReleaseVersion.IsNewer(currentVersion, latestVersion))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateDialog updateDialog = new UpdateDialog(latestVersion, downloadUri.AbsoluteUri);
                        updateDialog.ShowDialog();
                    });
                }
                else if (ignoreDefer)
                {
                    DialogMessageBox.Show(
                        $"Ihre Software ist auf dem neuesten Stand.\n\nInstallierte Version: {currentVersion}\nVerfügbare Version: {latestVersion}",
                        "BiMaDock-Update",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (HttpRequestException ex)
            {
                ReportCheckFailure(ex, ignoreDefer);
            }
            catch (TaskCanceledException ex)
            {
                ReportCheckFailure(ex, ignoreDefer);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                ReportCheckFailure(ex, ignoreDefer);
            }
            catch (InvalidDataException ex)
            {
                ReportCheckFailure(ex, ignoreDefer);
            }
        }

        internal static string GetCurrentVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? releaseTag = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "ReleaseTag")?.Value;

            if (!string.IsNullOrWhiteSpace(releaseTag))
            {
                return releaseTag;
            }

            string? informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            return informationalVersion?.Split('+')[0] ?? "Unknown";
        }

        private static bool IsUpdateDeferred(string releaseTag)
        {
            if (!File.Exists(ConfigFilePath))
            {
                return false;
            }

            string[] settings;
            try
            {
                settings = File.ReadAllLines(ConfigFilePath);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Could not read update deferral: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Could not read update deferral: {ex.Message}");
                return false;
            }

            return IsDeferredForRelease(settings, releaseTag, DateTimeOffset.UtcNow);
        }

        internal static bool IsDeferredForRelease(string[] settings, string releaseTag, DateTimeOffset now)
        {
            if (settings.Length != 2 ||
                !string.Equals(settings[0], releaseTag, StringComparison.OrdinalIgnoreCase) ||
                !DateTimeOffset.TryParse(
                    settings[1],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset deferredUntil))
            {
                return false;
            }

            return now < deferredUntil;
        }

        public static void DeferUpdate(string releaseTag)
        {
            if (!ReleaseVersion.TryParse(releaseTag, out _))
            {
                throw new ArgumentException("A valid release tag is required.", nameof(releaseTag));
            }

            string? directoryPath = Path.GetDirectoryName(ConfigFilePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("The update settings directory is invalid.");
            }

            Directory.CreateDirectory(directoryPath);
            File.WriteAllLines(ConfigFilePath, new[]
            {
                releaseTag,
                DateTimeOffset.UtcNow.AddDays(30).ToString("O", CultureInfo.InvariantCulture)
            });
        }

        private static void ReportCheckFailure(Exception exception, bool showToUser)
        {
            Debug.WriteLine($"Update check failed: {exception}");
            if (showToUser)
            {
                DialogMessageBox.Show(
                    $"Die Updateprüfung ist fehlgeschlagen.\n\n{exception.Message}",
                    "BiMaDock-Update",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
