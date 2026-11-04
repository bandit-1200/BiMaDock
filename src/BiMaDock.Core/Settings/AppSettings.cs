using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiMaDock.Core.Settings;

/// <summary>Einstellungen des Docks. Werte außerhalb der Grenzen werden beim Laden zurechtgerückt.</summary>
public sealed record AppSettings
{
    public const int MinDelay = 0;
    public const int MaxDelay = 1500;
    public const int MinFlow = 200;
    public const int MaxFlow = 1500;

    /// <summary>Verzögerung, bis das Dock nach Berühren des Rands erscheint.</summary>
    public int ShowDelayMs { get; init; } = 250;

    /// <summary>Verzögerung, bis das Dock nach Verlassen verschwindet.</summary>
    public int HideDelayMs { get; init; } = 450;

    /// <summary>Kategorie-Dock fließt als Flüssigkeit aus dem Hauptdock. Aus = erscheint sofort.</summary>
    public bool FlowAnimationEnabled { get; init; } = true;

    /// <summary>Dauer des Herausfließens.</summary>
    public int FlowDurationMs { get; init; } = 520;

    public AppSettings Normalized() => this with
    {
        ShowDelayMs = Math.Clamp(ShowDelayMs, MinDelay, MaxDelay),
        HideDelayMs = Math.Clamp(HideDelayMs, MinDelay, MaxDelay),
        FlowDurationMs = Math.Clamp(FlowDurationMs, MinFlow, MaxFlow),
    };
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly ILogger _logger;

    public SettingsStore(string filePath, ILogger<SettingsStore>? logger = null)
    {
        FilePath = filePath;
        _logger = logger ?? NullLogger<SettingsStore>.Instance;
    }

    public string FilePath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        try
        {
            return (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings()).Normalized();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Einstellungen beschädigt, Standardwerte werden verwendet");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings.Normalized(), Options));
        File.Move(temp, FilePath, overwrite: true);
    }
}
