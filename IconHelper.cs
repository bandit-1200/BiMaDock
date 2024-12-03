using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public static class IconHelper
{
    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_SMALLICON = 0x000000001;

    [StructLayout(LayoutKind.Sequential)]
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

    public static BitmapSource GetIcon(string filePath, string iconSource)
    {
        try
        {
            if (Path.GetExtension(iconSource).ToLower() == ".png")
            {
                return new BitmapImage(new Uri(iconSource));
            }

            SHFILEINFO shinfo = new SHFILEINFO();
            int result = SHGetFileInfo(iconSource, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

            if (result == 0 || shinfo.hIcon == IntPtr.Zero)
            {
                // Wenn das Icon nicht gefunden wird, versuche das zweite Argument
                return FallbackIcon(filePath);
            }

            using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
            {
                var bitmap = icon.ToBitmap();
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return bitmapSource;
            }
        }
        catch
        {
            // Bei einem Fehler versuche das zweite Argument
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
        int result = SHGetFileInfo(filePath, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

        if (result == 0 || shinfo.hIcon == IntPtr.Zero)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(new Bitmap(1, 1).GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        using (var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
        {
            var bitmap = icon.ToBitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return bitmapSource;
        }
    }


}
