using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


public class IconHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public const uint SHGFI_ICON = 0x100;
    public const uint SHGFI_LARGEICON = 0x0;    // 'Large icon

    // Icons werden mit 32 DIP angezeigt; 96 px reichen für bis zu 300 % Anzeigeskalierung.
    private const int IconPixelSize = 96;
    private const int DecodePixelWidth = IconPixelSize;

    // Cache für geladene Icons (nur UI-Thread). Schlüssel: Pfade + Änderungszeitpunkte der beteiligten Dateien.
    private static readonly Dictionary<(string FilePath, string IconSource, DateTime FileStamp, DateTime IconStamp), BitmapSource> Cache = new();

    private static BitmapSource? placeholderIcon;
    private static BitmapSource? defaultBrowserIcon;

    /// <summary>
    /// Leert den Icon-Cache (z. B. nachdem ein Dock-Element bearbeitet wurde).
    /// </summary>
    public static void ClearCache()
    {
        Cache.Clear();
        defaultBrowserIcon = null;
    }

    public static BitmapSource GetIcon(string filePath, string iconSource)
    {
        try
        {
            filePath ??= string.Empty;
            iconSource ??= string.Empty;

            var key = (filePath, iconSource, GetFileStamp(filePath), GetFileStamp(iconSource));
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var icon = LoadIcon(filePath, iconSource);
            Cache[key] = icon;
            return icon;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IconHelper.GetIcon: Fehler beim Laden ({filePath}, {iconSource}): {ex.Message}");
            return CreatePlaceholderIcon();
        }
    }

    private static DateTime GetFileStamp(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return File.GetLastWriteTimeUtc(path);
            }
        }
        catch
        {
            // Zeitstempel nicht ermittelbar – ohne Zeitstempel cachen
        }

        return DateTime.MinValue;
    }

    private static BitmapSource LoadIcon(string filePath, string iconSource)
    {
        try
        {
            if (Uri.TryCreate(iconSource, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Wenn der Pfad eine Webseite ist, Standard-Webbrowser-Icon laden
                return GetDefaultBrowserIcon();
            }

            var icon = TryLoadFromPath(iconSource);
            if (icon != null)
            {
                return icon;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IconHelper: Fehler beim Laden von IconSource '{iconSource}': {ex.Message}");
        }

        return FallbackIcon(filePath);
    }

    private static BitmapSource FallbackIcon(string filePath)
    {
        try
        {
            return TryLoadFromPath(filePath) ?? CreatePlaceholderIcon();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IconHelper: Fehler beim Laden von '{filePath}': {ex.Message}");
            return CreatePlaceholderIcon();
        }
    }

    /// <summary>
    /// Lädt ein Bild (.png) oder das Shell-Icon eines Pfads. Gibt null zurück, wenn nichts geladen werden kann.
    /// </summary>
    private static BitmapSource? TryLoadFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            return File.Exists(path) ? LoadImageFile(path) : null;
        }

        return LoadShellIcon(path);
    }

    private static BitmapSource LoadImageFile(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // keine Dateisperre
        image.DecodePixelWidth = DecodePixelWidth;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapSource? LoadShellIcon(string path)
    {
        // Bevorzugt die moderne Shell-Schnittstelle: liefert Icons in hoher Auflösung (inkl. Verknüpfungen und Ordner).
        var highResolution = TryLoadShellImage(path, IconPixelSize);
        if (highResolution != null)
        {
            return highResolution;
        }

        // Rückfall: klassisches 32x32-Icon
        SHFILEINFO shinfo = new SHFILEINFO();
        int result = SHGetFileInfo(path, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

        if (result == 0 || shinfo.hIcon == IntPtr.Zero)
        {
            if (shinfo.hIcon != IntPtr.Zero)
            {
                DestroyIcon(shinfo.hIcon);
            }
            return null;
        }

        return CreateBitmapSourceFromIcon(shinfo.hIcon);
    }

    private static BitmapSource GetDefaultBrowserIcon()
    {
        if (defaultBrowserIcon != null)
        {
            return defaultBrowserIcon;
        }

        // Methode zur Ermittlung des Standard-Webbrowsers und Laden des Icons
        string browserPath = GetDefaultBrowserPath();
        BitmapSource? icon = null;
        if (!string.IsNullOrEmpty(browserPath))
        {
            try
            {
                icon = LoadShellIcon(browserPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IconHelper: Browser-Icon konnte nicht geladen werden: {ex.Message}");
            }
        }

        // Fallback-Icon, falls Standard-Browser nicht ermittelt werden kann
        defaultBrowserIcon = icon ?? CreatePlaceholderIcon();
        return defaultBrowserIcon;
    }

    private static BitmapSource CreateBitmapSourceFromIcon(IntPtr hIcon)
    {
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            RenderOptions.SetBitmapScalingMode(bitmapSource, BitmapScalingMode.HighQuality);
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static BitmapSource? TryLoadShellImage(string path, int size)
    {
        IShellItemImageFactory? factory = null;
        IntPtr hBitmap = IntPtr.Zero;
        try
        {
            SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out factory);
            int hr = factory.GetImage(new NativeSize { Width = size, Height = size }, SIIGBF_BIGGERSIZEOK | SIIGBF_ICONONLY, out hBitmap);
            if (hr != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            return CreateBitmapSourceFromAlphaBitmap(hBitmap);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IconHelper: Hochauflösendes Icon für '{path}' nicht verfügbar: {ex.Message}");
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }
            if (factory != null)
            {
                Marshal.ReleaseComObject(factory);
            }
        }
    }

    /// <summary>
    /// Liest ein 32-Bit-HBITMAP samt Alphakanal aus. Imaging.CreateBitmapSourceFromHBitmap würde die Transparenz verlieren.
    /// </summary>
    private static BitmapSource? CreateBitmapSourceFromAlphaBitmap(IntPtr hBitmap)
    {
        if (GetObject(hBitmap, Marshal.SizeOf<NativeBitmap>(), out NativeBitmap info) == 0 || info.Width <= 0 || info.Height == 0)
        {
            return null;
        }

        int width = info.Width;
        int height = Math.Abs(info.Height);
        var header = new BitmapInfoHeader
        {
            Size = Marshal.SizeOf<BitmapInfoHeader>(),
            Width = width,
            Height = -height, // negativ = Zeilen von oben nach unten
            Planes = 1,
            BitCount = 32
        };
        var pixels = new byte[width * height * 4];

        IntPtr hdc = CreateCompatibleDC(IntPtr.Zero);
        try
        {
            if (GetDIBits(hdc, hBitmap, 0, (uint)height, pixels, ref header, 0) == 0)
            {
                return null;
            }
        }
        finally
        {
            DeleteDC(hdc);
        }

        // Bitmaps ohne Alphakanal (alle Werte 0) wären sonst unsichtbar.
        bool hasAlpha = false;
        for (int i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] != 0)
            {
                hasAlpha = true;
                break;
            }
        }
        if (!hasAlpha)
        {
            for (int i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private const int SIIGBF_BIGGERSIZEOK = 0x1;
    private const int SIIGBF_ICONONLY = 0x4;

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmap
    {
        public int Type;
        public int Width;
        public int Height;
        public int WidthBytes;
        public ushort Planes;
        public ushort BitsPixel;
        public IntPtr Bits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out NativeBitmap lpvObject);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BitmapInfoHeader lpbi, uint uUsage);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static BitmapSource CreatePlaceholderIcon()
    {
        if (placeholderIcon == null)
        {
            // 1x1 transparentes Bild
            var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
            bitmap.Freeze();
            placeholderIcon = bitmap;
        }

        return placeholderIcon;
    }

    private static string GetDefaultBrowserPath()
    {
        // Logik zur Ermittlung des Pfads des Standard-Webbrowsers
        // Dies kann je nach Windows-Version und Registrierungseinstellungen variieren
        // Beispielhafte Implementierung:
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"))
            {
                if (key != null)
                {
                    var progId = key.GetValue("ProgId") as string;
                    if (!string.IsNullOrEmpty(progId))
                    {
                        using (var browserKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"))
                        {
                            if (browserKey != null)
                            {
                                var command = browserKey.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(command))
                                {
                                    // Extrahiere den Pfad zur Exe-Datei
                                    var parts = command.Split('"');
                                    if (parts.Length > 1)
                                    {
                                        return parts[1];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fehlerbehandlung
        }

        return string.Empty;
    }
}
