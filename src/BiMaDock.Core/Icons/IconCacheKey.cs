using System.Security.Cryptography;
using System.Text;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Icons;

/// <summary>
/// Dateiname für ein zwischengespeichertes Symbol. Ändert sich das Ziel (z. B. Programm-Update), ändert sich
/// das Datum und damit der Name: alte Bilder werden einfach nicht mehr benutzt.
/// </summary>
public static class IconCacheKey
{
    /// <summary>Erhöhen, wenn sich die Art ändert, wie Symbole gelesen werden; dann gilt der alte Zwischenspeicher nicht mehr.</summary>
    public const int FormatVersion = 1;

    public static string FileName(DockEntry entry, DateTime? targetWriteTimeUtc, int pixelSize)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string text = string.Join('|', FormatVersion, pixelSize, entry.Kind, entry.Target.Trim().ToUpperInvariant(),
            targetWriteTimeUtc?.Ticks ?? 0);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32] + ".png";
    }
}
