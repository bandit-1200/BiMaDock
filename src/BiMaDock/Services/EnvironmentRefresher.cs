using System.Security;
using BiMaDock.Core.Launching;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BiMaDock.Services;

/// <summary>
/// Übernimmt geänderte Umgebungsvariablen (z. B. PATH nach einer Installation) in das Dock, damit gestartete
/// Programme den aktuellen Stand erben. Windows meldet Änderungen per WM_SETTINGCHANGE "Environment".
/// </summary>
public sealed class EnvironmentRefresher(ILogger<EnvironmentRefresher> logger)
{
    private const string MachineKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

    public void Reload()
    {
        try
        {
            var merged = EnvironmentMerger.Merge(
                Read(Registry.LocalMachine, MachineKey),
                Read(Registry.CurrentUser, "Environment"),
                Read(Registry.CurrentUser, "Volatile Environment"));

            int changed = 0;
            foreach (var (name, raw) in merged)
            {
                string value = Environment.ExpandEnvironmentVariables(raw);
                if (Environment.GetEnvironmentVariable(name) != value)
                {
                    Environment.SetEnvironmentVariable(name, value);
                    changed++;
                }
            }

            logger.LogInformation("Umgebungsvariablen neu geladen, {Count} geändert", changed);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            logger.LogError(ex, "Umgebungsvariablen konnten nicht neu geladen werden");
        }
    }

    private static Dictionary<string, string> Read(RegistryKey root, string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var key = root.OpenSubKey(path);
        if (key is null)
        {
            return values;
        }

        foreach (string name in key.GetValueNames())
        {
            // Nicht vorab auflösen: Verweise wie %USERPROFILE% erst nach dem Zusammensetzen auflösen
            if (key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is string value)
            {
                values[name] = value;
            }
        }

        return values;
    }
}
