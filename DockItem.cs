public class DockItem
{
    public string DisplayName { get; set; } = string.Empty; // Standardwert setzen
    public string FilePath { get; set; } = string.Empty; // Standardwert setzen
    public string Category { get; set; } = string.Empty; // Standardwert setzen
    public bool IsCategory { get; set; } = false; // Standardwert setzen
    public int Position { get; set; } = 0; // Standardwert setzen
}
