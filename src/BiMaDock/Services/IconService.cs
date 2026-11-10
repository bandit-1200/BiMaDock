using System.Windows.Media;
using System.Windows.Media.Imaging;
using BiMaDock.Core;
using BiMaDock.Core.Icons;
using BiMaDock.Core.Model;
using BiMaDock.Native;
using BiMaDock.Views;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Liefert Symbole für Dock-Einträge. Ergebnisse werden zwischengespeichert; Aufrufe nur auf dem UI-Thread.
/// </summary>
public sealed class IconService(IconLibrary library, ILogger<IconService> logger)
{
    /// <summary>Symbolbibliothek (mitgelieferte und eigene Symbole).</summary>
    public IconLibrary Library { get; } = library;

    /// <summary>
    /// Symbolgröße in Pixeln, passend zur Bildschirmskalierung: Das Symbol wird bis 2fach vergrößert (Lupe) mit
    /// 44 DIP angezeigt, es braucht also etwa 88 DIP x Skalierung Pixel, sonst wird es beim Vergrößern weich.
    /// Gerundet auf 128, 192 oder 256, damit der Zwischenspeicher (Pixelgröße steckt im Dateinamen) nicht ausufert.
    /// </summary>
    public static readonly int PixelSize = PixelSizeFor(DisplayDpi.SystemScale);

    internal static int PixelSizeFor(double scale)
    {
        double needed = ItemAnimations.IconSize * 2 * scale;
        return needed <= 128 ? 128 : needed <= 192 ? 192 : 256;
    }

    private readonly Dictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);
    private ImageSource? _placeholder;

    public ImageSource GetIcon(DockEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string key = KeyOf(entry);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        ImageSource icon;
        try
        {
            icon = Load(entry) ?? Placeholder();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Symbol für {Name} konnte nicht geladen werden", entry.Name);
            icon = Placeholder();
        }

        _cache[key] = icon;
        return icon;
    }

    public void ClearCache() => _cache.Clear();

    private static string KeyOf(DockEntry entry) => $"{entry.IconPath}|{entry.Target}|{entry.Kind}";

    /// <summary>
    /// Kommt der Eintrag fürs Vorladen in Frage: ein Ziel mit Shell-Symbol, das noch nicht im Speicher liegt?
    /// Nur UI-Thread. Einträge mit eigenem Symbol, Weblinks und Kategorien laden schnell und bleiben außen vor.
    /// </summary>
    public bool NeedsPreload(DockEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Kind == DockEntryKind.Launch
            && !string.IsNullOrWhiteSpace(entry.Target)
            && string.IsNullOrWhiteSpace(entry.IconPath)
            && !IsWebLink(entry.Target)
            && !_cache.ContainsKey(KeyOf(entry));
    }

    /// <summary>
    /// Ist das Ziel erreichbar und fehlt sein Bild noch im Plattencache? Darf im Hintergrund laufen (kann bei
    /// getrennten Netzlaufwerken lange dauern, der Aufrufer setzt ein Zeitlimit).
    /// </summary>
    public bool IsMissingInDiskCache(DockEntry entry)
    {
        string? cacheFile = CacheFileFor(entry);
        return cacheFile is not null && !File.Exists(cacheFile);
    }

    private ImageSource? Load(DockEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.IconPath) && Library.Resolve(entry.IconPath) is { } path && LoadCustom(path) is { } custom)
        {
            return custom;
        }

        if (entry.IsCategory)
        {
            return ShellIcons.FromPath(Path.GetTempPath(), PixelSize);
        }

        if (IsWebLink(entry.Target))
        {
            return ShellIcons.FromExtension(".html");
        }

        return string.IsNullOrWhiteSpace(entry.Target) ? null : LoadShellIcon(entry);
    }

    /// <summary>Höchstzahl Bilder im Zwischenspeicher; darüber wird er beim Start geleert (alte Versionen sammeln sich sonst).</summary>
    private const int MaxCachedFiles = 1000;

    /// <summary>
    /// Programmsymbol über den Zwischenspeicher auf der Platte: Windows nach Symbolen zu fragen dauert, besonders bei
    /// vielen Einträgen oder Netzlaufwerken. Der Name enthält das Änderungsdatum des Ziels, ein Update erneuert ihn.
    /// </summary>
    private ImageSource? LoadShellIcon(DockEntry entry)
    {
        string? cacheFile = CacheFileFor(entry);
        if (cacheFile is not null && File.Exists(cacheFile))
        {
            try
            {
                return LoadPng(cacheFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException
                or FileFormatException or FormatException)
            {
                // Kaputtes oder abgeschnittenes Bild (z. B. Schreiben unterbrochen): löschen und neu erzeugen
                logger.LogWarning(ex, "Zwischengespeichertes Symbol {File} unlesbar, wird gelöscht und neu erzeugt", cacheFile);
                TryDelete(cacheFile);
            }
        }

        var icon = ShellIcons.FromPath(entry.Target, PixelSize);
        if (icon is not null && cacheFile is not null)
        {
            SaveToCache(icon, cacheFile);
        }

        return icon;
    }

    private string? CacheFileFor(DockEntry entry)
    {
        DateTime? stamp;
        try
        {
            string target = entry.Target.Trim();
            stamp = File.Exists(target) ? File.GetLastWriteTimeUtc(target)
                : Directory.Exists(target) ? Directory.GetLastWriteTimeUtc(target)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            stamp = null;
        }

        // Fehlende Ziele nicht zwischenspeichern, sonst bliebe das Ersatzsymbol hängen
        return stamp is null ? null : Path.Combine(AppPaths.IconCacheDirectory, IconCacheKey.FileName(entry, stamp, PixelSize));
    }

    /// <summary>Ab dieser Dauer wird ein Schreibvorgang als blockiert ins Protokoll geschrieben.</summary>
    private static readonly TimeSpan SlowSave = TimeSpan.FromSeconds(5);

    /// <summary>Laufende Schreibvorgänge (nur UI-Thread), damit dieselbe Datei nicht doppelt geschrieben wird.</summary>
    private readonly HashSet<string> _pendingSaves = new(StringComparer.OrdinalIgnoreCase);

    private Task? _diskCacheCleanup;

    /// <summary>
    /// Bild wird auf dem UI-Thread nur im Speicher kodiert, das Schreiben läuft im Hintergrund:
    /// Sicherheitssoftware kann Dateizugriffe minutenlang blockieren, das darf das Dock nie einfrieren.
    /// </summary>
    private void SaveToCache(BitmapSource icon, string cacheFile)
    {
        if (!_pendingSaves.Add(cacheFile))
        {
            return;
        }

        byte[] png;
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(icon));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            png = stream.ToArray();
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Symbol konnte nicht zwischengespeichert werden: {File}", cacheFile);
            _pendingSaves.Remove(cacheFile);
            return;
        }

        _diskCacheCleanup ??= Task.Run(CleanDiskCache);
        var cleanup = _diskCacheCleanup;
        var save = Task.Run(async () =>
        {
            await cleanup.ConfigureAwait(false);
            WriteCacheFile(png, cacheFile);
        });
        _ = WatchSaveAsync(save, cacheFile);
    }

    private async Task WatchSaveAsync(Task save, string cacheFile)
    {
        try
        {
            if (await Task.WhenAny(save, Task.Delay(SlowSave)) != save)
            {
                logger.LogWarning("Zwischenspeichern von {File} dauert länger als {Seconds} s, die Datei wird vermutlich von anderer Software blockiert",
                    cacheFile, SlowSave.TotalSeconds);
            }

            await save;
        }
        catch (Exception ex)
        {
            // Der Schreibvorgang läuft im Hintergrund, niemand sonst würde die Ausnahme je sehen
            logger.LogError(ex, "Zwischenspeichern von {File} im Hintergrund fehlgeschlagen", cacheFile);
        }
        finally
        {
            _pendingSaves.Remove(cacheFile); // läuft wieder auf dem UI-Thread
        }
    }

    /// <summary>
    /// Läuft im Hintergrund. Schreibt direkt unter dem endgültigen Namen, ohne Temp-Datei und Umbenennen:
    /// Viele Dateien schreiben und danach die Endung ändern hält Anti-Ransomware-Software für einen Angriff,
    /// blockiert das Umbenennen und beendet das Programm. Eine halb geschriebene Datei wird gelöscht; bliebe
    /// doch eine zurück, erkennt <see cref="LoadShellIcon"/> sie als unlesbar und erzeugt sie neu.
    /// </summary>
    private void WriteCacheFile(byte[] png, string cacheFile)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.IconCacheDirectory);
            using var stream = new FileStream(cacheFile, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(png);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Symbol konnte nicht zwischengespeichert werden: {File}", cacheFile);
            if (File.Exists(cacheFile))
            {
                TryDelete(cacheFile);
            }
        }
    }

    /// <summary>
    /// Einmal je Programmlauf, im Hintergrund vor dem ersten Schreiben: liegengebliebene Temp-Dateien entfernen,
    /// bei zu vielen Bildern den Zwischenspeicher leeren (alte Versionen sammeln sich sonst).
    /// </summary>
    private void CleanDiskCache()
    {
        try
        {
            if (!Directory.Exists(AppPaths.IconCacheDirectory))
            {
                return;
            }

            var stale = Directory.GetFiles(AppPaths.IconCacheDirectory, "*.tmp");
            foreach (var file in stale)
            {
                TryDelete(file);
            }

            if (stale.Length > 0)
            {
                logger.LogInformation("{Count} liegengebliebene Temp-Dateien im Symbol-Zwischenspeicher entfernt", stale.Length);
            }

            var files = Directory.GetFiles(AppPaths.IconCacheDirectory);
            if (files.Length <= MaxCachedFiles)
            {
                return;
            }

            foreach (var file in files)
            {
                TryDelete(file);
            }

            logger.LogInformation("Symbol-Zwischenspeicher geleert ({Count} Dateien)", files.Length);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Symbol-Zwischenspeicher konnte nicht aufgeräumt werden");
        }
    }

    private void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Datei {File} konnte nicht gelöscht werden", file);
        }
    }

    private static BitmapImage LoadPng(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // keine Dateisperre
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>Lädt ein Bild der Bibliothek (Vorschau im Auswahldialog), zwischengespeichert.</summary>
    public ImageSource GetLibraryIcon(LibraryIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        string key = "lib|" + icon.FullPath;
        if (!_cache.TryGetValue(key, out var image))
        {
            try
            {
                image = LoadCustom(icon.FullPath) ?? Placeholder();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Symbol {Path} konnte nicht geladen werden", icon.FullPath);
                image = Placeholder();
            }

            _cache[key] = image;
        }

        return image;
    }

    private static ImageSource? LoadCustom(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string extension = Path.GetExtension(path);
        if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
        {
            // z. B. .exe oder .dll als Symbolquelle
            return ShellIcons.FromPath(path, PixelSize);
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // keine Dateisperre
        image.DecodePixelWidth = PixelSize;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static bool IsWebLink(string target) =>
        Uri.TryCreate(target, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private ImageSource Placeholder()
    {
        if (_placeholder is null)
        {
            var geometry = Geometry.Parse("M4,4 H20 V20 H4 Z");
            var drawing = new GeometryDrawing(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), null, geometry);
            var image = new DrawingImage(drawing);
            image.Freeze();
            _placeholder = image;
        }

        return _placeholder;
    }
}
