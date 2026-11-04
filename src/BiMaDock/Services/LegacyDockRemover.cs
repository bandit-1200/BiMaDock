using BiMaDock.Core;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BiMaDock.Services;

/// <summary>
/// Ersetzt die alte Version: beendet das alte Dock, entfernt dessen Autostart und deinstalliert es still.
/// Läuft erst, nachdem Einträge und Symbole übernommen sind (siehe DockViewModel.Load), und immer
/// als der angemeldete Benutzer, weil die alte Version pro Benutzer installiert wurde.
/// </summary>
public sealed class LegacyDockRemover(ILogger<LegacyDockRemover> logger)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BiMaDock_is1";
    private const string LegacyValueName = "BiMaDock";

    public void RemoveOldVersion()
    {
        try
        {
            StopOldProcesses();
            RemoveAutostart();
            Uninstall();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(ex, "Alte Version konnte nicht vollständig entfernt werden");
        }
    }

    private void StopOldProcesses()
    {
        string own = Path.GetFullPath(Environment.ProcessPath ?? string.Empty);
        foreach (var process in Process.GetProcessesByName("BiMaDock"))
        {
            using (process)
            {
                string? path = TryPath(process);
                if (path is null || string.Equals(Path.GetFullPath(path), own, StringComparison.OrdinalIgnoreCase)
                    || File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "BiMaDock.Core.dll")))
                {
                    continue; // das sind wir selbst oder eine andere Installation der neuen Version
                }

                logger.LogInformation("Altes Dock wird beendet: {Path}", path);
                process.Kill();
                process.WaitForExit(3000);
            }
        }
    }

    private void RemoveAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(LegacyValueName) is not null)
        {
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            logger.LogInformation("Autostart des alten Docks entfernt");
        }
    }

    private void Uninstall()
    {
        // Pro Benutzer installiert: still und ohne Rückfrage deinstallieren.
        if (FindUninstaller(Registry.CurrentUser, RegistryView.Default) is { } user)
        {
            logger.LogInformation("Alte Version (Benutzer) wird deinstalliert: {Exe}", user);
            Process.Start(new ProcessStartInfo(user, SilentArgs) { UseShellExecute = true });
            return;
        }

        // Für alle Benutzer installiert (Program Files): braucht Adminrechte, daher einmal fragen.
        string? machine = FindUninstaller(Registry.LocalMachine, RegistryView.Registry32)
            ?? FindUninstaller(Registry.LocalMachine, RegistryView.Registry64);
        if (machine is null || File.Exists(DeclinedMarker))
        {
            return;
        }

        var answer = System.Windows.MessageBox.Show(
            "Die alte Version von BiMaDock ist noch für alle Benutzer installiert.\n\n"
            + "Deine Einträge und Symbole sind schon übernommen. Soll die alte Version jetzt entfernt werden? "
            + "Windows fragt dafür nach Administratorrechten.",
            "BiMaDock", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (answer != System.Windows.MessageBoxResult.Yes)
        {
            File.WriteAllText(DeclinedMarker, "Deinstallation der alten Version abgelehnt. Datei löschen, um erneut zu fragen.");
            logger.LogInformation("Deinstallation der alten Version (alle Benutzer) abgelehnt");
            return;
        }

        try
        {
            logger.LogInformation("Alte Version (alle Benutzer) wird deinstalliert: {Exe}", machine);
            Process.Start(new ProcessStartInfo(machine, SilentArgs) { UseShellExecute = true, Verb = "runas" });
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            logger.LogInformation("Adminrechte für die Deinstallation verweigert");
        }
    }

    private const string SilentArgs = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART";

    private static string DeclinedMarker => Path.Combine(AppPaths.DataDirectory, "alte-version-behalten.txt");

    private static string? FindUninstaller(RegistryKey hive, RegistryView view)
    {
        using var root = RegistryKey.OpenBaseKey(hive == Registry.CurrentUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine, view);
        using var key = root.OpenSubKey(UninstallKey);
        if (key?.GetValue("UninstallString") is not string command)
        {
            return null;
        }

        string exe = command.Trim().Trim('"');
        return File.Exists(exe) ? exe : null;
    }

    private static string? TryPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }
}
