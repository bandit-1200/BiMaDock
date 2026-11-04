using System.Text.Json.Serialization;

namespace BiMaDock.Core.Model;

/// <summary>
/// Gesamter Inhalt des Docks, so wie er gespeichert wird.
/// </summary>
public sealed class DockLayout
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<DockEntry> Entries { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<DockEntry> MainEntries => Entries.Where(e => e.CategoryId is null);

    public IEnumerable<DockEntry> EntriesOf(Guid categoryId) => Entries.Where(e => e.CategoryId == categoryId);

    public DockEntry? Find(Guid id) => Entries.Find(e => e.Id == id);

    /// <summary>Einträge eines Docks: <paramref name="dockId"/> null = Hauptdock, sonst die Kategorie.</summary>
    public List<DockEntry> EntriesIn(Guid? dockId) => Entries.Where(e => e.CategoryId == dockId).ToList();

    /// <summary>
    /// Fügt einen Eintrag an Position <paramref name="index"/> eines Docks ein (über das Ende hinaus = anhängen).
    /// Kategorien landen immer im Hauptdock; ein unbekanntes Ziel ebenfalls. Gibt den eingefügten Eintrag zurück.
    /// </summary>
    public DockEntry Insert(DockEntry entry, Guid? dockId, int index)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.IsCategory || (dockId is { } id && Find(id) is not { IsCategory: true }))
        {
            dockId = null;
        }

        entry = entry with { CategoryId = dockId };
        var siblings = EntriesIn(dockId);
        index = Math.Clamp(index, 0, siblings.Count);

        int position = index < siblings.Count
            ? Entries.IndexOf(siblings[index])
            : siblings.Count > 0 ? Entries.IndexOf(siblings[^1]) + 1 : Entries.Count;
        Entries.Insert(position, entry);
        return entry;
    }

    /// <summary>
    /// Verschiebt einen Eintrag in ein Dock an Position <paramref name="index"/>, gezählt ohne den Eintrag selbst.
    /// Gibt false zurück, wenn der Eintrag fehlt oder in sich selbst verschoben würde.
    /// </summary>
    public bool Move(Guid id, Guid? dockId, int index)
    {
        var entry = Find(id);
        if (entry is null || dockId == id)
        {
            return false;
        }

        Entries.Remove(entry);
        Insert(entry, dockId, index);
        return true;
    }

    /// <summary>
    /// Hängt Einträge, deren Kategorie fehlt oder die selbst Kategorien sind, ins Hauptdock
    /// (keine verschachtelten Kategorien) und vergibt doppelte IDs neu.
    /// Gibt die Anzahl der korrigierten Einträge zurück.
    /// </summary>
    public int Normalize()
    {
        int fixedCount = 0;

        var seen = new HashSet<Guid>();
        for (int i = 0; i < Entries.Count; i++)
        {
            if (!seen.Add(Entries[i].Id))
            {
                Entries[i] = Entries[i] with { Id = Guid.NewGuid() };
                seen.Add(Entries[i].Id);
                fixedCount++;
            }
        }

        var categoryIds = Entries.Where(e => e.IsCategory).Select(e => e.Id).ToHashSet();
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.CategoryId is { } parent && (entry.IsCategory || !categoryIds.Contains(parent)))
            {
                Entries[i] = entry with { CategoryId = null };
                fixedCount++;
            }
        }

        return fixedCount;
    }
}
