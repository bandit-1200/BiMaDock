namespace BiMaDock.Core;

/// <summary>
/// Ablageorte der Benutzerdaten. Mit <c>BIMADOCK_DATA_DIR</c> lässt sich der Datenordner umstellen (Tests).
/// </summary>
public static class AppPaths
{
    public const string DataDirectoryVariable = "BIMADOCK_DATA_DIR";

    public static bool IsDataDirectoryOverridden =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DataDirectoryVariable));

    public static string DataDirectory => IsDataDirectoryOverridden
        ? Environment.GetEnvironmentVariable(DataDirectoryVariable)!
        : Path.Combine(LocalAppData, "BiMaDock2");

    public static string DockFile => Path.Combine(DataDirectory, "dock.json");

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public static string LogDirectory => Path.Combine(DataDirectory, "logs");

    /// <summary>Daten der bisherigen Version; werden beim ersten Start einmalig übernommen (nur gelesen).</summary>
    public static string? LegacyDockFile => IsDataDirectoryOverridden
        ? null
        : Path.Combine(LocalAppData, "BiMaDock", "docksettings.json");

    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
