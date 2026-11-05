namespace BiMaDock.Core.Launching;

/// <summary>
/// Setzt die Umgebung so zusammen, wie Windows sie für neu angemeldete Programme bildet: System, dann Benutzer,
/// dann flüchtige Werte. Benutzerwerte überschreiben Systemwerte, nur PATH wird aneinandergehängt.
/// </summary>
public static class EnvironmentMerger
{
    public static Dictionary<string, string> Merge(
        IReadOnlyDictionary<string, string> machine,
        IReadOnlyDictionary<string, string> user,
        IReadOnlyDictionary<string, string>? volatileValues = null)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(user);
        var result = new Dictionary<string, string>(machine, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in user)
        {
            result[name] = name.Equals("Path", StringComparison.OrdinalIgnoreCase)
                && result.TryGetValue(name, out var systemPath) && !string.IsNullOrEmpty(systemPath)
                ? $"{systemPath.TrimEnd(';')};{value}"
                : value;
        }

        foreach (var (name, value) in volatileValues ?? new Dictionary<string, string>())
        {
            result[name] = value;
        }

        return result;
    }
}
