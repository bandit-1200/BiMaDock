using System.ComponentModel;
using System.Diagnostics;
using BiMaDock.Core.Model;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

public interface ILauncher
{
    /// <summary>Öffnet das Ziel eines Eintrags. Gibt bei Fehlern eine verständliche Meldung zurück, sonst null.</summary>
    /// <paramref name="files"/>: Dateien, die auf das Symbol gezogen wurden und damit geöffnet werden sollen.
    string? Launch(DockEntry entry, IReadOnlyList<string>? files = null);
}

/// <summary>
/// Öffnet Programme, Dateien, Ordner, Verknüpfungen und Weblinks über die Windows-Shell.
/// </summary>
public sealed class ShellLauncher(ILogger<ShellLauncher> logger) : ILauncher
{
    public string? Launch(DockEntry entry, IReadOnlyList<string>? files = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string target = entry.Target;
        if (string.IsNullOrWhiteSpace(target))
        {
            return $"„{entry.Name}“ hat kein Ziel.";
        }

        bool isWebLink = IconService.IsWebLink(target);
        bool isShellItem = ShellTargets.IsShellTarget(target);
        if (!isWebLink && !isShellItem && !File.Exists(target) && !Directory.Exists(target))
        {
            logger.LogWarning("Ziel nicht gefunden: {Target}", target);
            return $"„{entry.Name}“ wurde nicht gefunden:\n{target}";
        }

        var startInfo = new ProcessStartInfo(target) { UseShellExecute = true };
        if (!isWebLink && !isShellItem)
        {
            startInfo.Arguments = LaunchArguments.Build(entry.Arguments, files);
            if (!string.IsNullOrWhiteSpace(entry.WorkingDirectory))
            {
                startInfo.WorkingDirectory = Environment.ExpandEnvironmentVariables(entry.WorkingDirectory.Trim());
            }
            else if (File.Exists(target))
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(target) ?? string.Empty;
            }

            if (entry.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }
        }

        try
        {
            using var _ = Process.Start(startInfo);
            logger.LogInformation("Gestartet: {Target} {Arguments}{Admin}", target, startInfo.Arguments, entry.RunAsAdmin ? " (als Admin)" : string.Empty);
            return null;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: Nutzer hat die UAC-Abfrage abgebrochen.
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException or IOException
            or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or PlatformNotSupportedException)
        {
            logger.LogError(ex, "Start fehlgeschlagen: {Target}", target);
            return $"„{entry.Name}“ konnte nicht gestartet werden:\n{ex.Message}";
        }
    }
}
