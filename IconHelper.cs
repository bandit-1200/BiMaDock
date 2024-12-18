using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;


public class IconHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

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

    public static BitmapSource GetIcon(string filePath, string iconSource)
    {
        try
        {
            if (Uri.TryCreate(iconSource, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Wenn der Pfad eine Webseite ist, Standard-Webbrowser-Icon laden
                return GetDefaultBrowserIcon();
            }

            if (Path.GetExtension(iconSource).ToLower() == ".png")
            {
                return new BitmapImage(new Uri(iconSource));
            }

            SHFILEINFO shinfo = new SHFILEINFO();
            int result = SHGetFileInfo(iconSource, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

            if (result == 0 || shinfo.hIcon == IntPtr.Zero)
            {
                return FallbackIcon(filePath);
            }

            using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
            {
                var bitmap = icon.ToBitmap();
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                // Setze BitmapScalingMode auf HighQuality
                RenderOptions.SetBitmapScalingMode(bitmapSource, BitmapScalingMode.HighQuality);

                return bitmapSource;
            }
        }
        catch
        {
            return FallbackIcon(filePath);
        }
    }

    private static BitmapSource FallbackIcon(string filePath)
    {
        if (Path.GetExtension(filePath).ToLower() == ".png")
        {
            return new BitmapImage(new Uri(filePath));
        }

        SHFILEINFO shinfo = new SHFILEINFO();
        int result = SHGetFileInfo(filePath, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

        if (result == 0 || shinfo.hIcon == IntPtr.Zero)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(new Bitmap(1, 1).GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
        {
            var bitmap = icon.ToBitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // Setze BitmapScalingMode auf HighQuality
            RenderOptions.SetBitmapScalingMode(bitmapSource, BitmapScalingMode.HighQuality);

            return bitmapSource;
        }
    }

    private static BitmapSource GetDefaultBrowserIcon()
    {
        // Methode zur Ermittlung des Standard-Webbrowsers und Laden des Icons
        string browserPath = GetDefaultBrowserPath();
        if (string.IsNullOrEmpty(browserPath))
        {
            // Fallback-Icon, falls Standard-Browser nicht ermittelt werden kann
            return Imaging.CreateBitmapSourceFromHBitmap(new Bitmap(1, 1).GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        SHFILEINFO shinfo = new SHFILEINFO();
        int result = SHGetFileInfo(browserPath, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

        if (result == 0 || shinfo.hIcon == IntPtr.Zero)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(new Bitmap(1, 1).GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
        {
            var bitmap = icon.ToBitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // Setze BitmapScalingMode auf HighQuality
            RenderOptions.SetBitmapScalingMode(bitmapSource, BitmapScalingMode.HighQuality);

            return bitmapSource;
        }
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
