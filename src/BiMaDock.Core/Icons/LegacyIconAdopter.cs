using BiMaDock.Core.Model;

namespace BiMaDock.Core.Icons;

/// <summary>
/// Sichert Symbole, auf die Einträge aus der alten Version noch per festem Pfad zeigen (z. B.
/// <c>%LOCALAPPDATA%\BiMaDock\Icons\...</c> oder der alte Programmordner). Die Bilder werden in die eigenen
/// Symbole des Benutzers kopiert und die Einträge auf die Kopie umgestellt. So bleiben die Symbole erhalten,
/// auch wenn das alte Dock deinstalliert oder sein Ordner gelöscht wird.
/// </summary>
public static class LegacyIconAdopter
{
    public const string Category = "Altes Dock";

    /// <summary>Gibt die Anzahl umgestellter Einträge zurück. Mehrfach aufrufbar, bereits gesicherte bleiben unverändert.</summary>
    public static int Adopt(DockLayout layout, IconLibrary library)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(library);

        var copies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int changed = 0;
        for (int i = 0; i < layout.Entries.Count; i++)
        {
            string? path = layout.Entries[i].IconPath;
            if (!IsLegacyFile(path))
            {
                continue;
            }

            string full = Path.GetFullPath(path!);
            if (!copies.TryGetValue(full, out string? reference))
            {
                reference = Existing(library, full) ?? library.Import(full, Category).Reference;
                copies[full] = reference;
            }

            layout.Entries[i] = layout.Entries[i] with { IconPath = reference };
            changed++;
        }

        return changed;
    }

    /// <summary>Absoluter, vorhandener Pfad in einem Ordner der alten Version ("BiMaDock", nicht "BiMaDock2").</summary>
    public static bool IsLegacyFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path))
        {
            return false;
        }

        return Path.GetFullPath(path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("BiMaDock", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Gleiche Datei schon in "Altes Dock"? Dann diese verwenden statt eine Kopie mit Nummer anzulegen.</summary>
    private static string? Existing(IconLibrary library, string source)
    {
        string target = Path.Combine(library.UserDirectory, Category, Path.GetFileName(source));
        if (!File.Exists(target) || new FileInfo(target).Length != new FileInfo(source).Length
            || !File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(source)))
        {
            return null;
        }

        return IconLibrary.UserPrefix + Category + "/" + Path.GetFileName(source);
    }
}
