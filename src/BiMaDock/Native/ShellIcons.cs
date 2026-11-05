using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BiMaDock.Native;

/// <summary>
/// Liest Shell-Symbole in hoher Auflösung samt Alphakanal (IShellItemImageFactory).
/// </summary>
internal static partial class ShellIcons
{
    private const int SIIGBF_BIGGERSIZEOK = 0x1;
    private const int SIIGBF_ICONONLY = 0x4;
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    /// <summary>
    /// Symbol einer Datei, eines Ordners oder einer Verknüpfung; null, wenn nicht ermittelbar.
    /// Alle Symbole kommen einheitlich groß und ohne Rahmen zurück: Hat ein Programm nur kleine Symbole, malt Windows
    /// sie bei großer Anfrage klein in einen hellen Kasten. Dann wird stattdessen die größte Größe ohne Kasten
    /// genommen, und durchsichtige Ränder werden abgeschnitten.
    /// </summary>
    public static BitmapSource? FromPath(string path, int size)
    {
        path = ShortcutIconFallback(path);
        var bitmap = Load(path, size);
        if (bitmap is not null && IsFramed(bitmap))
        {
            foreach (int smaller in new[] { 64, 48, 32 })
            {
                if (smaller < size && Load(path, smaller) is { } candidate && !IsFramed(candidate))
                {
                    bitmap = candidate;
                    break;
                }
            }
        }

        return bitmap is null ? null : TrimTransparentMargin(bitmap);
    }

    /// <summary>
    /// Verknüpfung, deren Symboldatei fehlt (z. B. gelöschter Installer-Cache): Symbol des Ziels nehmen,
    /// sonst zeigt Windows nur ein leeres Dokument.
    /// </summary>
    private static string ShortcutIconFallback(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            return path;
        }

        object? link = null;
        try
        {
            link = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"))!);
            if (link is not IShellLinkW shellLink || link is not IPersistFile file)
            {
                return path;
            }

            file.Load(path, 0);
            var buffer = new char[1024];
            shellLink.GetIconLocation(buffer, buffer.Length, out _);
            string icon = Environment.ExpandEnvironmentVariables(new string(buffer).TrimEnd('\0'));
            if (icon.Length == 0 || File.Exists(icon))
            {
                return path;
            }

            Array.Clear(buffer);
            shellLink.GetPath(buffer, buffer.Length, IntPtr.Zero, 0);
            string target = new string(buffer).TrimEnd('\0');
            return target.Length > 0 && File.Exists(target) ? target : path;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or IOException or UnauthorizedAccessException)
        {
            return path;
        }
        finally
        {
            if (link is not null)
            {
                Marshal.ReleaseComObject(link);
            }
        }
    }

    /// <summary>Kasten, den Windows um zu kleine Symbole malt: halbdurchsichtig weiße Ecken.</summary>
    private static bool IsFramed(BitmapSource bitmap)
    {
        int w = bitmap.PixelWidth;
        int h = bitmap.PixelHeight;
        if (w < 8 || h < 8)
        {
            return false;
        }

        return IsFramePixel(bitmap, 1, 1) && IsFramePixel(bitmap, w - 2, h - 2) && IsFramePixel(bitmap, w - 2, 1);
    }

    private static bool IsFramePixel(BitmapSource bitmap, int x, int y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new System.Windows.Int32Rect(x, y, 1, 1), pixel, 4, 0);
        byte a = pixel[3];

        // Grau bis Weiß (B = G = R), schwach deckend. Windows liefert den Kasten je nach Größe mit Alpha 12 bis 57.
        return a is >= 5 and <= 100 && pixel[0] >= a && Math.Abs(pixel[0] - pixel[1]) <= 4 && Math.Abs(pixel[1] - pixel[2]) <= 4;
    }

    /// <summary>Schneidet durchsichtige Ränder ab (quadratisch, mittig), damit alle Symbole gleich groß wirken.</summary>
    private static BitmapSource TrimTransparentMargin(BitmapSource bitmap)
    {
        int w = bitmap.PixelWidth;
        int h = bitmap.PixelHeight;
        var pixels = new byte[w * h * 4];
        bitmap.CopyPixels(pixels, w * 4, 0);

        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[(((y * w) + x) * 4) + 3] > 8)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
        {
            return bitmap;
        }

        int side = Math.Max(maxX - minX, maxY - minY) + 1;
        if (side >= Math.Min(w, h) - 2)
        {
            return bitmap; // kaum Rand, nichts zu tun
        }

        int left = Math.Clamp(((minX + maxX + 1) / 2) - (side / 2), 0, w - side);
        int top = Math.Clamp(((minY + maxY + 1) / 2) - (side / 2), 0, h - side);
        var cropped = new CroppedBitmap(bitmap, new System.Windows.Int32Rect(left, top, side, side));
        cropped.Freeze();
        return cropped;
    }

    private static BitmapSource? Load(string path, int size)
    {
        IntPtr hBitmap = IntPtr.Zero;
        object? factoryObject = null;
        try
        {
            int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out factoryObject);
            if (hr != 0 || factoryObject is not IShellItemImageFactory factory)
            {
                return null;
            }

            hr = factory.GetImage(new NativeSize(size, size), SIIGBF_BIGGERSIZEOK | SIIGBF_ICONONLY, out hBitmap);
            return hr == 0 && hBitmap != IntPtr.Zero ? FromAlphaBitmap(hBitmap) : null;
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }

            if (factoryObject is not null)
            {
                Marshal.ReleaseComObject(factoryObject);
            }
        }
    }

    /// <summary>Symbol, das Windows für eine Dateiendung anzeigt (z. B. ".html" für Weblinks).</summary>
    public static BitmapSource? FromExtension(string extension)
    {
        var info = new SHFILEINFO();
        IntPtr result = SHGetFileInfo(extension, FILE_ATTRIBUTE_NORMAL, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(),
            SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);
        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                info.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    private static BitmapSource? FromAlphaBitmap(IntPtr hBitmap)
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
            BitCount = 32,
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
        for (int i = 3; i < pixels.Length && !hasAlpha; i += 4)
        {
            hasAlpha = pixels[i] != 0;
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

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr phbm);
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszFile, int cch, IntPtr pfd, uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszName, int cch);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszDir, int cch);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszArgs, int cch);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszIconPath, int cch, out int piIcon);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeSize(int width, int height)
    {
        public readonly int Width = width;
        public readonly int Height = height;
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object? ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out NativeBitmap lpvObject);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BitmapInfoHeader lpbi, uint uUsage);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject);
}
