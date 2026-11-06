namespace BiMaDock.Core.Windowing;

/// <summary>
/// Oberflächen der Windows-Shell (Startmenü, Suche, Info-Center), die bildschirmfüllend aussehen können,
/// aber kein Vollbildprogramm sind. Das Dock soll davor nicht zurücktreten, sonst ist es beim Ziehen
/// aus dem Startmenü verdeckt. Erkannt wird am Prozess, nicht an der Fensterklasse, damit echte
/// Vollbild-XAML-Apps (gleiche Klasse, anderer Prozess) weiter als Vollbild gelten.
/// </summary>
public static class ShellSurfaces
{
    private static readonly HashSet<string> ShellProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "StartMenuExperienceHost",
        "ShellExperienceHost",
        "SearchHost",
        "SearchApp",
        "SearchUI",
        "ShellHost",
        "TextInputHost",
    };

    /// <summary>Fensterklassen der Shell-Oberflächen, die auch der Explorer selbst hosten kann (Startmenü, Taskleisten-Flyouts).</summary>
    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "XamlWindow",
        "Windows.UI.Core.CoreWindow",
    };

    /// <summary>True, wenn der Prozess (mit oder ohne ".exe") zur Shell-Oberfläche gehört. Unbekannter Prozess (null/leer) gilt nicht als Shell.</summary>
    public static bool IsShellProcess(string? processName) => ShellProcesses.Contains(Normalize(processName));

    /// <summary>
    /// True für Shell-Oberflächen: die Shell-Prozesse immer, der Explorer nur mit einer Shell-Fensterklasse
    /// (ein vollbildiges Explorer-Fenster mit F11 bleibt ein Vollbild).
    /// </summary>
    public static bool IsShellSurface(string? processName, string? className) =>
        IsShellProcess(processName)
        || (string.Equals(Normalize(processName), "explorer", StringComparison.OrdinalIgnoreCase)
            && className is not null && ShellClasses.Contains(className));

    private static string Normalize(string? processName)
    {
        string name = processName?.Trim() ?? string.Empty;
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }
}
