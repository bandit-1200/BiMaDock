using System.Text.Json;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Storage;

/// <summary>
/// Übernimmt die <c>docksettings.json</c> der bisherigen BiMaDock-Version. Die alte Datei wird nur gelesen.
/// </summary>
public static class LegacyImporter
{
    private sealed class LegacyItem
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? FilePath { get; set; }
        public string? Category { get; set; }
        public bool IsCategory { get; set; }
        public int Position { get; set; }
        public string? IconSource { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static DockLayout Import(string json)
    {
        var items = JsonSerializer.Deserialize<List<LegacyItem>>(json, Options) ?? [];

        // Alte IDs behalten, damit die Kategorie-Zuordnungen erhalten bleiben.
        var idMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Id) && !idMap.ContainsKey(item.Id))
            {
                idMap[item.Id] = Guid.TryParse(item.Id, out var parsed) ? parsed : Guid.NewGuid();
            }
        }

        var layout = new DockLayout();
        foreach (var item in items.OrderBy(i => i.Position))
        {
            Guid id = !string.IsNullOrWhiteSpace(item.Id) && idMap.TryGetValue(item.Id, out var known) ? known : Guid.NewGuid();
            Guid? category = !string.IsNullOrWhiteSpace(item.Category) && idMap.TryGetValue(item.Category, out var parent) ? parent : null;

            layout.Entries.Add(new DockEntry
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(item.DisplayName) ? DeriveName(item.FilePath) : item.DisplayName.Trim(),
                Kind = item.IsCategory ? DockEntryKind.Category : DockEntryKind.Launch,
                Target = item.IsCategory ? string.Empty : (item.FilePath ?? string.Empty).Trim(),
                IconPath = NormalizeIconPath(item.IconSource),
                CategoryId = category,
            });
        }

        layout.Normalize();
        return layout;
    }

    private static string DeriveName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Unbenannt";
        }

        var name = Path.GetFileNameWithoutExtension(path.TrimEnd('\\', '/'));
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static string? NormalizeIconPath(string? iconSource)
    {
        if (string.IsNullOrWhiteSpace(iconSource))
        {
            return null;
        }

        // Die alte Version speicherte lokale Pfade teils mit Schrägstrichen.
        return Uri.TryCreate(iconSource, UriKind.Absolute, out var uri) && uri.IsFile
            ? uri.LocalPath
            : iconSource;
    }
}
