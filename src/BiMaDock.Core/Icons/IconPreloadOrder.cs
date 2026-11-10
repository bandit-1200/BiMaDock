using BiMaDock.Core.Model;

namespace BiMaDock.Core.Icons;

/// <summary>
/// Reihenfolge, in der Symbole beim Start vorgeladen werden: erst das Hauptdock (sofort sichtbar), dann der Inhalt
/// der Kategorien in der Reihenfolge der Kategorien, zuletzt Reste (Einträge ohne gültige Kategorie).
/// Kategorien und Trennstriche haben kein eigenes Programmsymbol und kommen nicht vor.
/// </summary>
public static class IconPreloadOrder
{
    public static IReadOnlyList<DockEntry> Sort(DockLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var result = new List<DockEntry>();
        var seen = new HashSet<Guid>();

        void Add(IEnumerable<DockEntry> entries)
        {
            foreach (var entry in entries.Where(e => e.Kind == DockEntryKind.Launch))
            {
                if (seen.Add(entry.Id))
                {
                    result.Add(entry);
                }
            }
        }

        Add(layout.MainEntries);
        foreach (var category in layout.MainEntries.Where(e => e.IsCategory))
        {
            Add(layout.EntriesOf(category.Id));
        }

        Add(layout.Entries);
        return result;
    }
}
