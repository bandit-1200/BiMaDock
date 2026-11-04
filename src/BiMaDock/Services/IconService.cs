using System.Windows.Media;
using System.Windows.Media.Imaging;
using BiMaDock.Core.Model;
using BiMaDock.Native;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Liefert Symbole für Dock-Einträge. Ergebnisse werden zwischengespeichert; Aufrufe nur auf dem UI-Thread.
/// </summary>
public sealed class IconService(ILogger<IconService> logger)
{
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

    private static ImageSource? Load(DockEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.IconPath) && LoadCustom(entry.IconPath) is { } custom)
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

        return string.IsNullOrWhiteSpace(entry.Target) ? null : ShellIcons.FromPath(entry.Target, PixelSize);
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
