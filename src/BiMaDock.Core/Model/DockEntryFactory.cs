namespace BiMaDock.Core.Model;

/// <summary>
/// Erzeugt Dock-Einträge aus abgelegten Dateien, Ordnern und Weblinks.
/// </summary>
public static class DockEntryFactory
{
    public static DockEntry? FromDropValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            string host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
            return new DockEntry { Name = host, Target = uri.AbsoluteUri };
        }

        if (!Path.IsPathFullyQualified(value))
        {
            return null;
        }

        return new DockEntry { Name = NameForPath(value), Target = value };
    }

    public static DockEntry NewCategory(string name) => new()
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Kategorie" : name.Trim(),
        Kind = DockEntryKind.Category,
    };

    private static string NameForPath(string path)
    {
        string trimmed = path.TrimEnd('\\', '/');
        if (trimmed.Length <= 2 || Directory.Exists(path))
        {
            // Laufwerk ("C:") oder Ordner: vollständigen letzten Namen zeigen
            string name = Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? trimmed : name;
        }

        string withoutExtension = Path.GetFileNameWithoutExtension(trimmed);
        return string.IsNullOrWhiteSpace(withoutExtension) ? Path.GetFileName(trimmed) : withoutExtension;
    }
}
