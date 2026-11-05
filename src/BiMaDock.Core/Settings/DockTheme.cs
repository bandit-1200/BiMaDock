using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiMaDock.Core.Settings;

/// <summary>
/// Ein Theme ist ein gespeichertes Aussehen: Farben und Effekte. Verhalten (Verzögerungen, Updates) gehört nicht
/// dazu und bleibt beim Anwenden, wie der Benutzer es eingestellt hat.
/// </summary>
public sealed record DockTheme
{
    public required string Name { get; init; }

    /// <summary>Kurze Beschreibung für die Auswahl.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Mitgeliefert (nicht löschbar) oder vom Benutzer gespeichert.</summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; init; }

    public AppSettings Look { get; init; } = new();

    /// <summary>Übernimmt Farben und Effekte des Themes, behält aber das Verhalten aus <paramref name="current"/>.</summary>
    public AppSettings ApplyTo(AppSettings current)
    {
        ArgumentNullException.ThrowIfNull(current);
        return (Look with
        {
            ShowDelayMs = current.ShowDelayMs,
            HideDelayMs = current.HideDelayMs,
            UpdateMode = current.UpdateMode,
        }).Normalized();
    }

    /// <summary>Hält das aktuelle Aussehen als Theme fest.</summary>
    public static DockTheme FromSettings(string name, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(settings);
        var defaults = new AppSettings();

        // Verhaltenswerte auf Standard, damit die Datei nur das Aussehen beschreibt
        return new DockTheme
        {
            Name = name.Trim(),
            Description = "Eigenes Theme",
            Look = settings.Normalized() with
            {
                ShowDelayMs = defaults.ShowDelayMs,
                HideDelayMs = defaults.HideDelayMs,
                UpdateMode = defaults.UpdateMode,
            },
        };
    }

    /// <summary>Gleiches Aussehen wie <paramref name="settings"/> (für die Markierung des aktiven Themes).</summary>
    public bool Matches(AppSettings settings) => ApplyTo(settings) == settings.Normalized();
}

/// <summary>Die mitgelieferten Themes.</summary>
public static class BuiltInThemes
{
    private static readonly AppSettings Base = new();

    public static IReadOnlyList<DockTheme> All { get; } =
    [
        Theme("Standard", "Dunkles Glas mit blauem Akzent, alle Effekte an", Base),
        Theme("Mitternacht", "Tiefes Blau, kühles Türkis, sanftes Gleiten", Base with
        {
            DockColor = "#141B2D", DockOpacity = 94, BorderColor = "#4CC2FF", AccentColor = "#38E1D6",
            HoverColor = "#4CC2FF", HoverOpacity = 18, FeedbackColor = "#2A7FB8", SlideStyle = SlideStyle.Glide,
        }),
        Theme("Graphit", "Schlicht und ruhig, kaum Bewegung", Base with
        {
            DockColor = "#1A1A1D", DockOpacity = 96, BorderColor = "#000000", AccentColor = "#FFFFFF",
            HoverColor = "#FFFFFF", HoverOpacity = 10, FeedbackColor = "#546E7A", HoverEffect = HoverEffect.None,
            MagnifyScale = 125, LaunchBounce = false, SlideStyle = SlideStyle.Glide, SlideDurationMs = 200,
        }),
        Theme("Milchglas", "Hell und luftig, wie frisch geputzt", Base with
        {
            DockColor = "#E8E8EC", DockOpacity = 78, BorderColor = "#FFFFFF", AccentColor = "#2A7FB8",
            HoverColor = "#FFFFFF", HoverOpacity = 35, FeedbackColor = "#2A7FB8",
        }),
        Theme("Wald", "Dunkles Grün mit frischem Akzent", Base with
        {
            DockColor = "#1E3A2B", DockOpacity = 92, BorderColor = "#6CCB5F", AccentColor = "#6CCB5F",
            HoverColor = "#6CCB5F", HoverOpacity = 16, FeedbackColor = "#2E7D32", HoverEffect = HoverEffect.Swing,
        }),
        Theme("Sonnenuntergang", "Warmes Rotbraun, orange Akzente, federnd", Base with
        {
            DockColor = "#4A2B22", DockOpacity = 92, BorderColor = "#FF8C00", AccentColor = "#FF8C00",
            HoverColor = "#FFD54F", HoverOpacity = 16, FeedbackColor = "#D2691E", SlideSpring = 55,
        }),
        Theme("Neon", "Fast schwarz, pinke Kanten, alles hüpft", Base with
        {
            DockColor = "#0D0D12", DockOpacity = 95, BorderColor = "#FF5C8A", AccentColor = "#FF5C8A",
            HoverColor = "#B388FF", HoverOpacity = 22, FeedbackColor = "#8E24AA", HoverEffect = HoverEffect.Hop,
            MagnifyScale = 165, LaunchBounceCount = 4, ItemPopOvershoot = 80,
        }),
        Theme("Minimal", "Ohne Animationen, maximal schnell", Base with
        {
            DockColor = "#2A2A30", DockOpacity = 100, HoverOpacity = 12, HoverEffect = HoverEffect.None,
            MagnifyEnabled = false, LaunchBounce = false, ItemPopEnabled = false, FlowAnimationEnabled = false,
            SlideStyle = SlideStyle.None, ShowLabels = false,
        }),
    ];

    private static DockTheme Theme(string name, string description, AppSettings look) =>
        new() { Name = name, Description = description, IsBuiltIn = true, Look = look.Normalized() };
}

/// <summary>Eigene Themes als JSON-Dateien in einem Ordner. Dateien lassen sich so auch weitergeben.</summary>
public sealed class ThemeStore(string directory, ILogger<ThemeStore>? logger = null)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ILogger _logger = logger ?? NullLogger<ThemeStore>.Instance;

    public string Directory { get; } = directory;

    /// <summary>Mitgelieferte und eigene Themes, eigene alphabetisch dahinter.</summary>
    public List<DockTheme> LoadAll() =>
        [.. BuiltInThemes.All, .. LoadUserThemes().OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)];

    public List<DockTheme> LoadUserThemes()
    {
        var result = new List<DockTheme>();
        if (!System.IO.Directory.Exists(Directory))
        {
            return result;
        }

        foreach (string file in System.IO.Directory.GetFiles(Directory, "*.json"))
        {
            try
            {
                var theme = JsonSerializer.Deserialize<DockTheme>(File.ReadAllText(file), Options);
                if (theme is not null && !string.IsNullOrWhiteSpace(theme.Name))
                {
                    result.Add(theme with { IsBuiltIn = false, Look = (theme.Look ?? new AppSettings()).Normalized() });
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Theme {File} übersprungen", file);
            }
        }

        return result;
    }

    /// <summary>Speichert ein eigenes Theme; ein gleichnamiges wird ersetzt.</summary>
    public void Save(DockTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        System.IO.Directory.CreateDirectory(Directory);
        string path = PathFor(theme.Name);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(theme with { IsBuiltIn = false }, Options));
        File.Move(temp, path, overwrite: true);
    }

    public void Delete(string name)
    {
        string path = PathFor(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>Name gehört einem mitgelieferten Theme.</summary>
    public static bool IsBuiltInName(string name) =>
        BuiltInThemes.All.Any(t => t.Name.Equals(name?.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private string PathFor(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string safe = new([.. name.Trim().Select(c => invalid.Contains(c) ? '_' : c)]);
        return Path.Combine(Directory, (safe.Length == 0 ? "Theme" : safe) + ".json");
    }
}
