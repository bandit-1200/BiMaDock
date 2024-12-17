using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BiMaDock
{
    public class TestPath
    {
        public static async Task<string> GetShortcutTargetAsync(string filePath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"GetShortcutTarget.ps1\" -shortcutPath \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process? process = Process.Start(psi);

                if (process == null)
                {
                    Console.WriteLine("GetShortcutTargetAsync: Fehler: Prozess konnte nicht gestartet werden.");
                    return string.Empty; // Rückgabe eines leeren Strings anstelle von null
                }

                using (StreamReader reader = process.StandardOutput)
                {
                    string result = await reader.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    Console.WriteLine($"GetShortcutTargetAsync: Verknüpfung: {filePath}");
                    Console.WriteLine($"GetShortcutTargetAsync: Zielpfad: {result.Trim()}");
                    return result.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetShortcutTargetAsync: Fehler beim Abrufen des Ziels der Verknüpfung: {ex.Message}");
                return string.Empty; // Rückgabe eines leeren Strings anstelle von null im Fehlerfall
            }
        }
    }
}
