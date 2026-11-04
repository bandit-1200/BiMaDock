namespace BiMaDock.Core.Model;

public enum DockEntryKind
{
    /// <summary>Programm, Datei, Ordner, Verknüpfung oder Weblink.</summary>
    Launch,

    /// <summary>Kategorie, die ihre Einträge im Kategorie-Dock zeigt.</summary>
    Category,
}

/// <summary>
/// Ein Eintrag im Haupt- oder Kategorie-Dock. Die Reihenfolge ergibt sich aus der Position in der Liste.
/// </summary>
public sealed record DockEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public DockEntryKind Kind { get; init; } = DockEntryKind.Launch;

    /// <summary>Pfad oder URL, die beim Klick geöffnet wird. Leer bei Kategorien.</summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>Optionales eigenes Symbol (PNG/ICO). Ohne Angabe wird das Shell-Symbol des Ziels verwendet.</summary>
    public string? IconPath { get; init; }

    /// <summary>Kategorie, zu der der Eintrag gehört. Null = Hauptdock.</summary>
    public Guid? CategoryId { get; init; }

    public bool IsCategory => Kind == DockEntryKind.Category;
}
