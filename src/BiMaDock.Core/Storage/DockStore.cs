using System.Text.Json;
using System.Text.Json.Serialization;
using BiMaDock.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiMaDock.Core.Storage;

/// <summary>
/// Lädt und speichert <see cref="DockLayout"/> als JSON. Speichern ist atomar (temporäre Datei, dann ersetzen).
/// </summary>
public sealed class DockStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string? _legacyFilePath;
    private readonly ILogger _logger;

    public DockStore(string filePath, string? legacyFilePath = null, ILogger<DockStore>? logger = null)
    {
        FilePath = filePath;
        _legacyFilePath = legacyFilePath;
        _logger = logger ?? NullLogger<DockStore>.Instance;
    }

    public string FilePath { get; }

    public DockLayout Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                var layout = JsonSerializer.Deserialize<DockLayout>(File.ReadAllText(FilePath), Options) ?? new DockLayout();
                layout.Entries ??= [];
                int fixedCount = layout.Normalize();
                if (fixedCount > 0)
                {
                    _logger.LogWarning("{Count} fehlerhafte Einträge beim Laden korrigiert", fixedCount);
                }

                return layout;
            }
            catch (JsonException ex)
            {
                string backup = BackupCorruptFile();
                _logger.LogError(ex, "Dock-Datei beschädigt, gesichert als {Backup}", backup);
                return new DockLayout();
            }
        }

        if (_legacyFilePath is not null && File.Exists(_legacyFilePath))
        {
            try
            {
                var imported = LegacyImporter.Import(File.ReadAllText(_legacyFilePath));
                Save(imported);
                _logger.LogInformation("{Count} Einträge aus {Legacy} übernommen", imported.Entries.Count, _legacyFilePath);
                return imported;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Alte Dock-Datei {Legacy} konnte nicht gelesen werden", _legacyFilePath);
            }
        }

        return new DockLayout();
    }

    public void Save(DockLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);

        string tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(layout, Options));
        File.Move(tempPath, FilePath, overwrite: true);
    }

    private string BackupCorruptFile()
    {
        string backup = $"{FilePath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
        File.Copy(FilePath, backup, overwrite: true);
        return backup;
    }
}
