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

    /// <summary>Name unter jedem Symbol anzeigen.</summary>
    public bool ShowLabels { get; init; } = true;

    /// <summary>Animation, wenn die Maus über ein Symbol fährt.</summary>
    public HoverEffect HoverEffect { get; init; } = HoverEffect.Zoom;

    /// <summary>Symbol hüpft kurz, wenn ein Eintrag gestartet wird.</summary>
    public bool LaunchBounce { get; init; } = true;

    /// <summary>Stärke der Hover-Animation in Prozent (Größe, Winkel, Sprunghöhe).</summary>
    public int HoverIntensity { get; init; } = 100;

    /// <summary>Tempo der Hover-Animation in Prozent; höher ist schneller.</summary>
    public int HoverSpeed { get; init; } = 100;

    /// <summary>Sprunghöhe beim Starten in DIP.</summary>
    public int LaunchBounceHeight { get; init; } = 18;

    /// <summary>Lupe wie beim Mac: Symbol unter der Maus wächst, Nachbarn wachsen etwas mit.</summary>
    public bool MagnifyEnabled { get; init; } = true;

    /// <summary>Größe des Symbols direkt unter der Maus in Prozent.</summary>
    public int MagnifyScale { get; init; } = 145;

    /// <summary>Wie viele Nachbarn auf jeder Seite mitwachsen.</summary>
    public int MagnifyRange { get; init; } = 2;

    /// <summary>Art, wie das Dock am oberen Rand hereinkommt.</summary>
    public SlideStyle SlideStyle { get; init; } = SlideStyle.Spring;

    /// <summary>Dauer des Ein- und Ausblendens.</summary>
    public int SlideDurationMs { get; init; } = 260;

    /// <summary>Neue Einträge ploppen herein, entfernte schrumpfen weg.</summary>
    public bool ItemPopEnabled { get; init; } = true;

    /// <summary>Dauer von Hereinploppen und Wegschrumpfen.</summary>
    public int ItemPopDurationMs { get; init; } = 280;

    public const string DefaultDockColor = "#2A2A30";
    public const string DefaultBorderColor = "#FFFFFF";
    public const string DefaultAccentColor = "#4CC2FF";
    public const string DefaultFeedbackColor = "#D2691E";
    public const int MinOpacity = 40;

    /// <summary>Farbe von Haupt- und Kategorie-Dock (#RRGGBB).</summary>
    public string DockColor { get; init; } = DefaultDockColor;

    /// <summary>Deckkraft der Dock-Fläche in Prozent.</summary>
    public int DockOpacity { get; init; } = 92;

    /// <summary>Farbe des feinen Rands um die Docks (#RRGGBB, wird halbtransparent gezeichnet).</summary>
    public string BorderColor { get; init; } = DefaultBorderColor;

    /// <summary>Akzent für geöffnete Kategorie und Ablegen-Markierung (#RRGGBB).</summary>
    public string AccentColor { get; init; } = DefaultAccentColor;

    /// <summary>Hintergrund des Docks, über dem gerade etwas gezogen wird (#RRGGBB).</summary>
    public string FeedbackColor { get; init; } = DefaultFeedbackColor;

    public AppSettings Normalized() => this with
    {
        ShowDelayMs = Math.Clamp(ShowDelayMs, MinDelay, MaxDelay),
        HideDelayMs = Math.Clamp(HideDelayMs, MinDelay, MaxDelay),
        FlowDurationMs = Math.Clamp(FlowDurationMs, MinFlow, MaxFlow),
        DockOpacity = Math.Clamp(DockOpacity, MinOpacity, 100),
        DockColor = NormalizeColor(DockColor, DefaultDockColor),
        BorderColor = NormalizeColor(BorderColor, DefaultBorderColor),
        AccentColor = NormalizeColor(AccentColor, DefaultAccentColor),
        FeedbackColor = NormalizeColor(FeedbackColor, DefaultFeedbackColor),
        HoverEffect = Enum.IsDefined(HoverEffect) ? HoverEffect : HoverEffect.Zoom,
        HoverIntensity = Math.Clamp(HoverIntensity, 25, 200),
        HoverSpeed = Math.Clamp(HoverSpeed, 50, 200),
        LaunchBounceHeight = Math.Clamp(LaunchBounceHeight, 6, 30),
        MagnifyScale = Math.Clamp(MagnifyScale, 110, 200),
        MagnifyRange = Math.Clamp(MagnifyRange, 1, 4),
        SlideStyle = Enum.IsDefined(SlideStyle) ? SlideStyle : SlideStyle.Spring,
        SlideDurationMs = Math.Clamp(SlideDurationMs, 80, 600),
        ItemPopDurationMs = Math.Clamp(ItemPopDurationMs, 100, 600),
    };

    /// <summary>Gibt die Farbe als "#RRGGBB" in Großbuchstaben zurück; ungültige Werte ergeben <paramref name="fallback"/>.</summary>
    public static string NormalizeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string hex = value.Trim().TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        return hex.Length == 6 && hex.All(Uri.IsHexDigit) ? $"#{hex.ToUpperInvariant()}" : fallback;
    }
}

/// <summary>Animation beim Überfahren eines Symbols.</summary>
public enum HoverEffect
{
    None,
    Zoom,
    Hop,
    Swing,
    Wiggle,
    Pulse,
}

/// <summary>Bewegung beim Ein- und Ausblenden des Docks.</summary>
public enum SlideStyle
{
    /// <summary>Sofort, ohne Bewegung.</summary>
    None,

    /// <summary>Gleitet weich herein.</summary>
    Glide,

    /// <summary>Gleitet herein und federt kurz nach.</summary>
    Spring,
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };
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
