public class DockItem
{
    public string FilePath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Oder `string?` wenn Kategorie optional ist
}
