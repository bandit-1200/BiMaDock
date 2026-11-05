using System.Windows.Media;
using System.Windows.Media.Imaging;
using BiMaDock.Core;
using BiMaDock.Core.Icons;
using BiMaDock.Core.Model;
using BiMaDock.Native;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Liefert Symbole für Dock-Einträge. Ergebnisse werden zwischengespeichert; Aufrufe nur auf dem UI-Thread.
/// </summary>
public sealed class IconService(IconLibrary library, ILogger<IconService> logger)
{
    /// <summary>Symbolbibliothek (mitgelieferte und eigene Symbole).</summary>
    public IconLibrary Library { get; } = library;

    /// <summary>Symbole werden mit 48 DIP angezeigt; 128 px reichen für 250 % Skalierung.</summary>
    public const int PixelSize = 128;

    private readonly Dictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);
    private ImageSource? _placeholder;

    public ImageSource GetIcon(DockEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string key = $"{entry.IconPath}|{entry.Target}|{entry.Kind}";
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

    private bool _diskCacheChecked;

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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                logger.LogWarning(ex, "Zwischengespeichertes Symbol {File} unlesbar, wird neu erzeugt", cacheFile);
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

    private void SaveToCache(BitmapSource icon, string cacheFile)
    {
        try
        {
            PruneDiskCacheOnce();
            Directory.CreateDirectory(AppPaths.IconCacheDirectory);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(icon));
            string temp = cacheFile + ".tmp";
            using (var stream = File.Create(temp))
            {
                encoder.Save(stream);
            }

            File.Move(temp, cacheFile, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Symbol konnte nicht zwischengespeichert werden: {File}", cacheFile);
        }
    }

    private void PruneDiskCacheOnce()
    {
        if (_diskCacheChecked)
        {
            return;
        }

        _diskCacheChecked = true;
        if (!Directory.Exists(AppPaths.IconCacheDirectory))
        {
            return;
        }

        var files = Directory.GetFiles(AppPaths.IconCacheDirectory);
        if (files.Length <= MaxCachedFiles)
        {
            return;
        }

        foreach (var file in files)
        {
            File.Delete(file);
        }

        logger.LogInformation("Symbol-Zwischenspeicher geleert ({Count} Dateien)", files.Length);
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
