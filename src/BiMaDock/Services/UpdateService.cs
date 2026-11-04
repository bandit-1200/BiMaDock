using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using BiMaDock.Core.Settings;
using BiMaDock.Core.Updates;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Sucht nach neuen Versionen auf GitHub: automatisch kurz nach dem Start und danach alle paar Stunden,
/// oder von Hand. Je nach Einstellung wird nur gefragt oder still installiert.
/// </summary>
public sealed class UpdateService : IDisposable
{
    private static readonly TimeSpan FirstCheck = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly UpdateChecker _checker;
    private readonly ILogger<UpdateService> _logger;
    private readonly DispatcherTimer _timer = new();
    private Func<AppSettings> _settings = () => new AppSettings();
    private bool _busy;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _checker = new UpdateChecker(new HttpClient { Timeout = TimeSpan.FromMinutes(5) });
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = Interval;
            await CheckAsync(owner: null, manual: false);
        };
    }

    public static Version CurrentVersion =>
        UpdateChecker.Normalize(typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>Automatische Prüfung starten; <paramref name="settings"/> liefert jeweils die aktuellen Einstellungen.</summary>
    public void Start(Func<AppSettings> settings)
    {
        _settings = settings;
        _timer.Interval = FirstCheck;
        _timer.Start();
    }

    /// <summary>Sucht nach einer neuen Version. Bei <paramref name="manual"/> gibt es immer eine Rückmeldung.</summary>
    public async Task CheckAsync(Window? owner, bool manual)
    {
        var mode = _settings().UpdateMode;
        if (_busy || (!manual && mode == UpdateMode.Off))
        {
            return;
        }

        _busy = true;
        try
        {
            var update = await _checker.FindNewerAsync(CurrentVersion);
            if (update is null)
            {
                _logger.LogInformation("Update-Prüfung: {Version} ist aktuell", CurrentVersion);
                if (manual)
                {
                    Show(owner, $"BiMaDock {CurrentVersion} ist aktuell.", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return;
            }

            _logger.LogInformation("Update gefunden: {Version}", update.Version);
            bool silent = !manual && mode == UpdateMode.Automatic;
            if (!silent)
            {
                string notes = string.IsNullOrWhiteSpace(update.Notes) ? string.Empty : $"\n\n{Shorten(update.Notes)}";
                var answer = Show(owner,
                    $"BiMaDock {update.Version} ist da (installiert ist {CurrentVersion}).{notes}\n\nJetzt herunterladen und installieren? Das Dock startet danach neu.",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            string setup = await _checker.DownloadAsync(update);
            _logger.LogInformation("Installiere {Setup}", setup);

            // /UPDATE sorgt im Installer dafür, dass das Dock danach wieder startet (auch im stillen Modus).
            Process.Start(new ProcessStartInfo(setup, $"{(silent ? "/VERYSILENT" : "/SILENT")} /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /UPDATE")
            {
                UseShellExecute = true,
            });
            Application.Current.Shutdown();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or System.Text.Json.JsonException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(ex, "Update-Prüfung fehlgeschlagen");
            if (manual)
            {
                Show(owner, $"Die Suche nach Updates hat nicht geklappt:\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose() => _timer.Stop();

    private static string Shorten(string text) => text.Length <= 600 ? text.Trim() : text[..600].Trim() + " …";

    private static MessageBoxResult Show(Window? owner, string text, MessageBoxButton buttons, MessageBoxImage image) =>
        owner is { IsVisible: true }
            ? MessageBox.Show(owner, text, "BiMaDock Update", buttons, image)
            : MessageBox.Show(text, "BiMaDock Update", buttons, image);
}
