using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BiMaDock.Services;

/// <summary>
/// Räumt Reste der alten Version auf: beendet das alte Dock und entfernt dessen Autostart.
/// Deinstalliert wird die alte Version vom Setup (installer/BiMaDock2.iss, PrepareToInstall).
/// Läuft erst, nachdem Einträge und Symbole übernommen sind (siehe DockViewModel.Load), und immer
/// als der angemeldete Benutzer, weil die alte Version pro Benutzer installiert wurde.
/// </summary>
public sealed class LegacyDockRemover(ILogger<LegacyDockRemover> logger)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string LegacyValueName = "BiMaDock";

    public void RemoveOldVersion()
    {
        try
        {
            StopOldProcesses();
            RemoveAutostart();
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
