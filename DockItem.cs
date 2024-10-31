public class DockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Neue ID, standardmäßig eine eindeutige GUID
    public string DisplayName { get; set; } = string.Empty; // Standardwert setzen
    public string FilePath { get; set; } = string.Empty; // Standardwert setzen
    public string Category { get; set; } = string.Empty; // Standardwert setzen
    public bool IsCategory { get; set; } = false; // Standardwert setzen
    public int Position { get; set; } = 0; // Standardwert setzen
}
