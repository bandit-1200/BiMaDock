namespace BiMaDock.Core.Icons;

/// <summary>Ein Symbol der Bibliothek.</summary>
/// <param name="Reference">Wert, der im Dock-Eintrag gespeichert wird (z. B. <c>lib:Medien/Musik.png</c>).</param>
public sealed record LibraryIcon(string Name, string Category, string FullPath, string Reference, bool IsBuiltIn);

/// <summary>
/// Symbolbibliothek aus zwei Quellen: mitgelieferte Symbole im Programmordner (nur lesen, für alle Benutzer gleich)
/// und eigene Symbole im Datenordner des Benutzers. Einträge verweisen über eine Referenz statt über einen
/// absoluten Pfad, damit sie nach einer Neuinstallation an anderem Ort weiter passen.
/// </summary>
public sealed class IconLibrary(string builtInDirectory, string userDirectory)
{
    public const string BuiltInPrefix = "lib:";
    public const string UserPrefix = "user:";
    public const string UserCategory = "Eigene";

    private static readonly string[] Extensions = [".png", ".ico", ".jpg", ".jpeg"];

    public string BuiltInDirectory { get; } = builtInDirectory;

    public string UserDirectory { get; } = userDirectory;

    /// <summary>Alle Symbole, mitgelieferte zuerst, jeweils nach Kategorie und Name sortiert.</summary>
    public IReadOnlyList<LibraryIcon> Load() =>
        [.. Scan(BuiltInDirectory, BuiltInPrefix, isBuiltIn: true), .. Scan(UserDirectory, UserPrefix, isBuiltIn: false)];

    /// <summary>Löst eine Referenz in einen Dateipfad auf. Normale Pfade werden unverändert zurückgegeben.</summary>
    public string? Resolve(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        if (iconPath.StartsWith(BuiltInPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Combine(BuiltInDirectory, iconPath[BuiltInPrefix.Length..]);
        }

        if (iconPath.StartsWith(UserPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Combine(UserDirectory, iconPath[UserPrefix.Length..]);
        }

        return iconPath;
    }

    /// <summary>Kopiert eine Bilddatei in die eigenen Symbole. Gleichnamige Dateien bekommen eine Nummer.</summary>
    public LibraryIcon Import(string sourceFile, string category = UserCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        string extension = Path.GetExtension(sourceFile).ToLowerInvariant();
        if (!Extensions.Contains(extension))
        {
            throw new NotSupportedException($"Dateityp {extension} wird nicht unterstützt (png, ico, jpg).");
        }

        string folder = Path.Combine(UserDirectory, SafeName(category));
        Directory.CreateDirectory(folder);
        string name = SafeName(Path.GetFileNameWithoutExtension(sourceFile));
        string target = Path.Combine(folder, name + extension);
        for (int i = 2; File.Exists(target); i++)
        {
            target = Path.Combine(folder, $"{name} {i}{extension}");
        }

        File.Copy(sourceFile, target);
        return ToIcon(UserDirectory, UserPrefix, target, isBuiltIn: false);
    }

    /// <summary>Löscht ein eigenes Symbol. Mitgelieferte Symbole lassen sich nicht löschen.</summary>
    public void Delete(LibraryIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        if (icon.IsBuiltIn)
        {
            throw new InvalidOperationException("Mitgelieferte Symbole können nicht gelöscht werden.");
        }

        File.Delete(icon.FullPath);
        string? folder = Path.GetDirectoryName(icon.FullPath);
        if (folder is not null && !PathEquals(folder, UserDirectory) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
        {
            Directory.Delete(folder);
        }
    }

    private static IEnumerable<LibraryIcon> Scan(string root, string prefix, bool isBuiltIn)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => ToIcon(root, prefix, f, isBuiltIn))
            .OrderBy(i => i.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase);
    }

    private static LibraryIcon ToIcon(string root, string prefix, string file, bool isBuiltIn)
    {
        string relative = Path.GetRelativePath(root, file);
        string? folder = Path.GetDirectoryName(relative);
        string category = string.IsNullOrEmpty(folder) ? (isBuiltIn ? "Allgemein" : UserCategory) : folder.Replace('\\', '/');
        return new LibraryIcon(Path.GetFileNameWithoutExtension(file), category, file, prefix + relative.Replace('\\', '/'), isBuiltIn);
    }

    /// <summary>Verbindet Wurzel und relativen Teil; Ausbrüche aus der Wurzel (..) ergeben null.</summary>
    private static string? Combine(string root, string relative)
    {
        string fullRoot = Path.GetFullPath(root);
        string full = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? full
            : null;
    }

    private static string SafeName(string value)
    {
        string cleaned = string.Concat(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
        return cleaned.Length == 0 ? "Symbol" : cleaned;
    }

    private static bool PathEquals(string a, string b) =>
        string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
}
