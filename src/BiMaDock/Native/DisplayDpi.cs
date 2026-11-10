using System.Runtime.InteropServices;

namespace BiMaDock.Native;

/// <summary>Skalierung des Hauptbildschirms (100 % = 1,0), unabhängig von einem Fenster.</summary>
internal static partial class DisplayDpi
{
    /// <summary>Skalierung beim Start (System-DPI); 1,0, falls Windows keinen Wert liefert.</summary>
    public static double SystemScale { get; } = Query();

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForSystem();

    private static double Query()
    {
        try
        {
            uint dpi = GetDpiForSystem();
            return dpi >= 96 ? dpi / 96.0 : 1.0;
        }
        catch (EntryPointNotFoundException)
        {
            return 1.0; // vor Windows 10 1607
        }
    }
}
