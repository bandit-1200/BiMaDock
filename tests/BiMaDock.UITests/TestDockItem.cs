namespace BiMaDock.UITests;

/// <summary>Spiegelt das JSON-Format von docksettings.json.</summary>
public sealed class TestDockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsCategory { get; set; }
    public int Position { get; set; }
    public string IconSource { get; set; } = string.Empty;

    public static TestDockItem File(string name, string path, int position, string category = "") =>
        new() { DisplayName = name, FilePath = path, Position = position, Category = category };

    public static TestDockItem CategoryItem(string name, int position) =>
        new() { DisplayName = name, IsCategory = true, Position = position };
}
