namespace BiMaDock.Core.Model;

/// <summary>Startparameter für Einträge, auch wenn Dateien auf ein Symbol gezogen werden.</summary>
public static class LaunchArguments
{
    private static readonly string[] FileAcceptingExtensions = [".exe", ".lnk", ".bat", ".cmd", ".com"];

    /// <summary>Ziel kann Dateien öffnen, die man auf sein Symbol zieht (Programm oder Verknüpfung).</summary>
    public static bool AcceptsFiles(DockEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Kind == DockEntryKind.Launch
            && FileAcceptingExtensions.Contains(Path.GetExtension(entry.Target.Trim()), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Eigene Parameter des Eintrags plus die übergebenen Dateien, jede in Anführungszeichen.</summary>
    public static string Build(string? arguments, IEnumerable<string>? files = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            parts.Add(arguments.Trim());
        }

        foreach (var file in files ?? [])
        {
            if (!string.IsNullOrWhiteSpace(file))
            {
                parts.Add($"\"{file.Trim().Trim('"')}\"");
            }
        }

        return string.Join(' ', parts);
    }
}
