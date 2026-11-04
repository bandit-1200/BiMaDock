using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BiMaDock.Services;

/// <summary>
/// Autostart über <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>. Der Pfad wird in Anführungszeichen geschrieben.
/// </summary>
public sealed class StartupService(ILogger<StartupService> logger)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultValueName = "BiMaDock2";

    /// <summary>Für Tests umstellbar, damit der echte Autostart-Eintrag unberührt bleibt.</summary>
    private static string ValueName =>
        Environment.GetEnvironmentVariable("BIMADOCK_STARTUP_VALUE_NAME") is { Length: > 0 } name ? name : DefaultValueName;

    public static string Command => $"\"{Environment.ProcessPath}\"";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value && value.Equals(Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool TrySetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                key.SetValue(ValueName, Command);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            logger.LogInformation("Autostart {State}", enabled ? "aktiviert" : "deaktiviert");
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            logger.LogError(ex, "Autostart konnte nicht geändert werden");
            return false;
        }
    }
}
