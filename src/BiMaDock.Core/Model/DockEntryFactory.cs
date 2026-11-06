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

        if (value.StartsWith(ShellTargets.AppsFolderPrefix, StringComparison.OrdinalIgnoreCase) && value.Length > ShellTargets.AppsFolderPrefix.Length)
        {
            return new DockEntry { Name = ShellTargets.FallbackName(value), Target = value };
        }

        if (!Path.IsPathFullyQualified(value))
        {
            return null;
        }

        return new DockEntry { Name = NameForPath(value), Target = value };
    }

    /// <summary>Eintrag aus dem Startmenü: <paramref name="displayName"/> ist der Name, den Windows anzeigt.</summary>
    public static DockEntry? FromShellItem(string? target, string? displayName)
    {
        if (FromDropValue(target) is not { } entry)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(displayName) ? entry : new DockEntry { Name = displayName.Trim(), Target = entry.Target };
    }

    public static DockEntry NewCategory(string name) => new()
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Kategorie" : name.Trim(),
        Kind = DockEntryKind.Category,
    };

    public static DockEntry NewSeparator() => new() { Name = "Trenner", Kind = DockEntryKind.Separator };

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
