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
        using var key = Registry.CurrentUser.OpenSubKey(UninstallKey);
        if (key?.GetValue("UninstallString") is not string command)
        {
            return;
        }

        string exe = command.Trim().Trim('"');
        if (!File.Exists(exe))
        {
            return;
        }

        logger.LogInformation("Alte Version wird deinstalliert: {Exe}", exe);
        Process.Start(new ProcessStartInfo(exe, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART") { UseShellExecute = true });
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
