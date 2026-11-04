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
